import { ChangeDetectionStrategy, Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { HttpEventType } from '@angular/common/http';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { switchMap } from 'rxjs';

import { AccessService } from '../../core/api/access.service';
import { BlobUploadService } from '../../core/api/blob-upload.service';
import { UploadService } from '../../core/api/upload.service';
import { SharingWithType } from '../../core/api/api-models';

interface UploadTarget {
  readonly key: string;
  readonly label: string;
  readonly type: SharingWithType;
  readonly sharedWith: string;
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
  readonly progress = signal(0);
  readonly file = signal<File | null>(null);

  readonly form = this.fb.nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(200)]],
    recordedDate: ['', [Validators.required]],
    targetKey: ['private', [Validators.required]],
  });

  readonly canSubmit = computed(() => this.file() !== null);

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
            { key: 'private', label: 'Private library', type: SharingWithType.Private, sharedWith: '' },
            ...groups,
            ...events,
          ]);
          this.targetsLoading.set(false);
        },
        error: () => this.targetsLoading.set(false),
      });
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.file.set(input.files?.[0] ?? null);
  }

  submit(): void {
    const file = this.file();
    if (this.form.invalid || !file || this.stage() === 'uploading') {
      return;
    }

    const { name, recordedDate, targetKey } = this.form.getRawValue();
    const target = this.targets().find((option) => option.key === targetKey);
    if (!target) {
      return;
    }

    this.stage.set('uploading');
    this.progress.set(0);

    this.uploads
      .produceUploadUrl({
        nameOfVideo: name.trim(),
        fileName: file.name,
        recordedTimeUtc: new Date(recordedDate),
        sharedWith: target.sharedWith,
        sharingWithType: target.type,
      })
      .pipe(
        switchMap((response) => this.blob.upload(response.sas ?? '', file)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (httpEvent) => {
          if (httpEvent.type === HttpEventType.UploadProgress && httpEvent.total) {
            this.progress.set(Math.round((100 * httpEvent.loaded) / httpEvent.total));
          } else if (httpEvent.type === HttpEventType.Response) {
            this.stage.set('done');
          }
        },
        error: () => this.stage.set('error'),
      });
  }

  reset(): void {
    this.stage.set('form');
    this.progress.set(0);
    this.file.set(null);
    this.form.reset({ name: '', recordedDate: '', targetKey: 'private' });
  }
}
