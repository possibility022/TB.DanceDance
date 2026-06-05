import { ChangeDetectionStrategy, Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { concatMap, defer, from, switchMap, tap } from 'rxjs';

import { AccessService } from '../../core/api/access.service';
import { BlobUploadService } from '../../core/api/blob-upload.service';
import { UploadService } from '../../core/api/upload.service';
import { ProduceUploadUrlRequest, SharingWithType } from '../../core/api/api-models';

interface UploadTarget {
  readonly key: string;
  readonly label: string;
  readonly type: SharingWithType;
  /** Group/event id; undefined for the private library. */
  readonly sharedWith?: string;
}

type Stage = 'form' | 'uploading' | 'done' | 'error';

@Component({
  selector: 'app-upload',
  imports: [ReactiveFormsModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './upload.html',
})
export class Upload {
  private readonly uploads = inject(UploadService);
  private readonly blob = inject(BlobUploadService);
  private readonly access = inject(AccessService);
  private readonly fb = inject(FormBuilder);
  private readonly destroyRef = inject(DestroyRef);

  readonly today = new Date().toISOString().slice(0, 10);

  readonly targetsLoading = signal(true);
  readonly targets = signal<readonly UploadTarget[]>([]);

  readonly stage = signal<Stage>('form');
  readonly files = signal<readonly File[]>([]);
  readonly total = signal(0);
  readonly currentIndex = signal(0);
  readonly progress = signal(0);

  readonly form = this.fb.nonNullable.group({
    name: ['', [Validators.maxLength(200)]],
    recordedDate: [''],
    targetKey: ['private', [Validators.required]],
  });

  readonly singleFile = computed(() => this.files().length === 1);
  readonly canSubmit = computed(() => this.files().length > 0);

  constructor() {
    this.loadTargets();
  }

  loadTargets(): void {
    this.targetsLoading.set(true);
    this.access
      .getMyAccess()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (response) => {
          const groups = (response.assigned?.groups ?? []).map<UploadTarget>((group) => ({
            key: `g:${group.id}`,
            label: `Group: ${group.name}`,
            type: SharingWithType.Group,
            sharedWith: group.id ?? '',
          }));
          const events = (response.assigned?.events ?? []).map<UploadTarget>((event) => ({
            key: `e:${event.id}`,
            label: `Event: ${event.name}`,
            type: SharingWithType.Event,
            sharedWith: event.id ?? '',
          }));
          this.targets.set([
            { key: 'private', label: 'Private library', type: SharingWithType.Private },
            ...groups,
            ...events,
          ]);
          this.targetsLoading.set(false);
        },
        error: () => this.targetsLoading.set(false),
      });
  }

  onFilesSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.files.set(input.files ? Array.from(input.files) : []);
  }

  submit(): void {
    const files = this.files();
    if (files.length === 0 || this.form.invalid || this.stage() === 'uploading') {
      return;
    }

    const { name, targetKey } = this.form.getRawValue();
    const target = this.targets().find((option) => option.key === targetKey);
    if (!target) {
      return;
    }

    // When a single file is selected, the optional name applies; for a batch,
    // each recording is named after its file.
    const explicitName = files.length === 1 ? name.trim() : '';

    this.stage.set('uploading');
    this.total.set(files.length);
    this.currentIndex.set(0);
    this.progress.set(0);

    from(files)
      .pipe(
        concatMap((file, index) =>
          defer(() => {
            this.currentIndex.set(index + 1);
            this.progress.set(0);
            return this.uploads.produceUploadUrl(this.buildRequest(file, target, explicitName)).pipe(
              switchMap((response) => this.blob.upload(response.sas ?? '', file)),
              tap((percent) => this.progress.set(percent)),
            );
          }),
        ),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        error: () => this.stage.set('error'),
        complete: () => this.stage.set('done'),
      });
  }

  reset(): void {
    this.stage.set('form');
    this.progress.set(0);
    this.currentIndex.set(0);
    this.total.set(0);
    this.files.set([]);
    this.form.reset({ name: '', recordedDate: '', targetKey: 'private' });
  }

  private buildRequest(file: File, target: UploadTarget, explicitName: string): ProduceUploadUrlRequest {
    const recordedDate = this.form.getRawValue().recordedDate;
    return {
      nameOfVideo: explicitName || file.name,
      fileName: file.name,
      recordedTimeUtc: recordedDate ? new Date(recordedDate) : new Date(file.lastModified),
      sharingWithType: target.type,
      // Omitted for the private library (no group/event target).
      sharedWith: target.sharedWith as string,
    };
  }
}
