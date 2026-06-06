import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';

import { Upload } from './upload';
import { UploadService } from '../../core/api/upload.service';
import { BlobUploadService } from '../../core/api/blob-upload.service';
import { AccessService } from '../../core/api/access.service';
import { SharingWithType } from '../../core/api/api-models';

function file(name: string): File {
  return new File([new Uint8Array(1)], name, { type: 'video/mp4' });
}

function createFixture(overrides: {
  getMyAccess?: ReturnType<typeof vi.fn>;
  produceUploadUrl?: ReturnType<typeof vi.fn>;
  upload?: ReturnType<typeof vi.fn>;
}) {
  const access = {
    getMyAccess:
      overrides.getMyAccess ??
      vi.fn(() =>
        of({
          assigned: {
            groups: [{ id: 'g1', name: 'Salsa' }],
            events: [{ id: 'e1', name: 'Gala', date: new Date(2026, 0, 1) }],
          },
        }),
      ),
  };
  const uploads = {
    produceUploadUrl: overrides.produceUploadUrl ?? vi.fn(() => of({ sas: 'sas-url', videoId: 'v' })),
  };
  const blob = { upload: overrides.upload ?? vi.fn(() => of(100)) };

  TestBed.configureTestingModule({
    imports: [Upload],
    providers: [
      { provide: AccessService, useValue: access },
      { provide: UploadService, useValue: uploads },
      { provide: BlobUploadService, useValue: blob },
    ],
  });

  const fixture: ComponentFixture<Upload> = TestBed.createComponent(Upload);
  fixture.detectChanges();
  return { fixture, access, uploads, blob, component: fixture.componentInstance };
}

describe('Upload', () => {
  it('builds upload targets from assigned groups and events plus the private library', () => {
    const { component } = createFixture({});
    const targets = component.targets();
    expect(targets.map((t) => t.key)).toEqual(['private', 'g:g1', 'e:e1']);
    expect(targets[1]).toMatchObject({ label: 'Group: Salsa', type: SharingWithType.Group, sharedWith: 'g1' });
    expect(targets[2]).toMatchObject({ label: 'Event: Gala', type: SharingWithType.Event, sharedWith: 'e1' });
    expect(component.targetsLoading()).toBe(false);
  });

  it('stops the targets spinner even when access loading fails', () => {
    const { component } = createFixture({ getMyAccess: vi.fn(() => throwError(() => new Error('x'))) });
    expect(component.targetsLoading()).toBe(false);
    expect(component.targets()).toEqual([]);
  });

  it('tracks selected files and derived flags', () => {
    const { component } = createFixture({});
    expect(component.canSubmit()).toBe(false);

    component.onFilesSelected({ target: { files: [file('a.mp4')] } } as unknown as Event);

    expect(component.files()).toHaveLength(1);
    expect(component.canSubmit()).toBe(true);
    expect(component.singleFile()).toBe(true);
  });

  it('does nothing on submit without files', () => {
    const { component, uploads } = createFixture({});
    component.submit();
    expect(uploads.produceUploadUrl).not.toHaveBeenCalled();
  });

  it('uploads a single file to the private library and finishes', () => {
    const { component, uploads, blob } = createFixture({});
    const f = file('lesson.mp4');
    component.files.set([f]);
    component.form.patchValue({ recordedDate: '2026-05-01' });

    component.submit();

    expect(uploads.produceUploadUrl).toHaveBeenCalledTimes(1);
    expect(uploads.produceUploadUrl).toHaveBeenCalledWith({
      nameOfVideo: 'lesson.mp4',
      fileName: 'lesson.mp4',
      recordedTimeUtc: new Date('2026-05-01'),
      sharingWithType: SharingWithType.Private,
      sharedWith: undefined,
    });
    expect(blob.upload).toHaveBeenCalledWith('sas-url', f);
    expect(component.stage()).toBe('done');
    expect(component.progress()).toBe(100);
    expect(component.total()).toBe(1);
    expect(component.currentIndex()).toBe(1);
  });

  it('applies an explicit name for a single file', () => {
    const { component, uploads } = createFixture({});
    component.files.set([file('lesson.mp4')]);
    component.form.patchValue({ name: '  Cha-cha basics  ', recordedDate: '2026-05-01' });

    component.submit();

    expect(uploads.produceUploadUrl).toHaveBeenCalledWith(
      expect.objectContaining({ nameOfVideo: 'Cha-cha basics', fileName: 'lesson.mp4' }),
    );
  });

  it('names each recording after its file when uploading a batch', () => {
    const { component, uploads } = createFixture({});
    component.files.set([file('one.mp4'), file('two.mp4')]);
    component.form.patchValue({ name: 'ignored for batches', recordedDate: '2026-05-01' });

    component.submit();

    expect(uploads.produceUploadUrl).toHaveBeenCalledTimes(2);
    expect(uploads.produceUploadUrl).toHaveBeenNthCalledWith(1, expect.objectContaining({ nameOfVideo: 'one.mp4' }));
    expect(uploads.produceUploadUrl).toHaveBeenNthCalledWith(2, expect.objectContaining({ nameOfVideo: 'two.mp4' }));
    expect(component.stage()).toBe('done');
  });

  it('targets the selected group', () => {
    const { component, uploads } = createFixture({});
    component.files.set([file('lesson.mp4')]);
    component.form.patchValue({ targetKey: 'g:g1', recordedDate: '2026-05-01' });

    component.submit();

    expect(uploads.produceUploadUrl).toHaveBeenCalledWith(
      expect.objectContaining({ sharingWithType: SharingWithType.Group, sharedWith: 'g1' }),
    );
  });

  it('enters the error stage when an upload fails', () => {
    const { component } = createFixture({
      produceUploadUrl: vi.fn(() => throwError(() => new Error('nope'))),
    });
    component.files.set([file('lesson.mp4')]);

    component.submit();

    expect(component.stage()).toBe('error');
  });

  it('reset returns to the form with cleared state', () => {
    const { component } = createFixture({});
    component.files.set([file('a.mp4')]);
    component.submit();
    expect(component.stage()).toBe('done');

    component.reset();

    expect(component.stage()).toBe('form');
    expect(component.files()).toEqual([]);
    expect(component.progress()).toBe(0);
    expect(component.total()).toBe(0);
    expect(component.form.getRawValue()).toEqual({ name: '', recordedDate: '', targetKey: 'private' });
  });
});
