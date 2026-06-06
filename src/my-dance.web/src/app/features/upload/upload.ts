import { ChangeDetectionStrategy, Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { EMPTY, catchError, concatMap, defer, from, switchMap, tap } from 'rxjs';

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

interface FileRow {
  readonly id: string;
  readonly fileName: string;
  readonly recordedDate: string;
}

type Stage = 'form' | 'uploading' | 'done' | 'error';
type UploadItemStatus = 'pending' | 'uploading' | 'done' | 'error';

interface UploadItem {
  readonly id: string;
  readonly fileName: string;
  readonly progress: number;
  readonly status: UploadItemStatus;
}

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
  readonly fileRows = signal<readonly FileRow[]>([]);
  readonly uploadItems = signal<readonly UploadItem[]>([]);
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
    const files = input.files ? Array.from(input.files) : [];
    const recordedDate = this.form.getRawValue().recordedDate;
    this.files.set(files);
    this.fileRows.set(files.map((file, index) => this.toFileRow(file, index, recordedDate)));
    this.uploadItems.set([]);
  }

  applyRecordedDateToAll(): void {
    const recordedDate = this.form.getRawValue().recordedDate;
    this.fileRows.update((rows) => rows.map((row) => ({ ...row, recordedDate })));
  }

  onRecordedDateSelected(index: number, event: Event): void {
    const recordedDate = (event.target as HTMLInputElement).value;
    this.fileRows.update((rows) =>
      rows.map((row, rowIndex) => (rowIndex === index ? { ...row, recordedDate } : row)),
    );
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
    this.uploadItems.set(
      files.map((file, index) => ({
        id: `${index}:${file.name}:${file.lastModified}:${file.size}`,
        fileName: file.name,
        progress: 0,
        status: 'pending',
      })),
    );

    from(files)
      .pipe(
        concatMap((file, index) =>
          defer(() => {
            this.currentIndex.set(index + 1);
            this.progress.set(0);
            this.updateUploadItem(index, { progress: 0, status: 'uploading' });
            return this.uploads.produceUploadUrl(this.buildRequest(file, target, explicitName, index)).pipe(
              switchMap((response) => this.blob.upload(response.sas ?? '', file)),
              tap({
                next: (percent) => {
                  this.progress.set(percent);
                  this.updateUploadItem(index, { progress: percent });
                },
                error: () => this.updateUploadItem(index, { status: 'error' }),
                complete: () => this.updateUploadItem(index, { progress: 100, status: 'done' }),
              }),
              catchError(() => EMPTY),
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
    this.fileRows.set([]);
    this.uploadItems.set([]);
    this.form.reset({ name: '', recordedDate: '', targetKey: 'private' });
  }

  private updateUploadItem(index: number, patch: Partial<UploadItem>): void {
    this.uploadItems.update((items) =>
      items.map((item, itemIndex) => (itemIndex === index ? { ...item, ...patch } : item)),
    );
  }

  private toFileRow(file: File, index: number, recordedDate: string): FileRow {
    return {
      id: `${index}:${file.name}:${file.lastModified}:${file.size}`,
      fileName: file.name,
      recordedDate,
    };
  }

  private buildRequest(
    file: File,
    target: UploadTarget,
    explicitName: string,
    index: number,
  ): ProduceUploadUrlRequest {
    const fileRow = this.fileRows()[index];
    const recordedDate = fileRow ? fileRow.recordedDate : this.form.getRawValue().recordedDate;
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
