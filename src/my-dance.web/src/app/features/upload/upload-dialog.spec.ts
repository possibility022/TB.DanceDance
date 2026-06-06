import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of, Subject, throwError } from 'rxjs';

import { UploadDialog } from './upload-dialog';
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
    imports: [UploadDialog],
    providers: [
      { provide: AccessService, useValue: access },
      { provide: UploadService, useValue: uploads },
      { provide: BlobUploadService, useValue: blob },
    ],
  });

  const fixture: ComponentFixture<UploadDialog> = TestBed.createComponent(UploadDialog);
  fixture.detectChanges();
  return { fixture, access, uploads, blob, component: fixture.componentInstance };
}

describe('UploadDialog', () => {
  describe('target loading', () => {
    it('builds targets from assigned groups, events, and the private library', () => {
      const { component } = createFixture({});
      const targets = component.targets();
      expect(targets.map((t) => t.key)).toEqual(['private', 'g:g1', 'e:e1']);
      expect(targets[1]).toMatchObject({ label: 'Group: Salsa', type: SharingWithType.Group, sharedWith: 'g1' });
      expect(targets[2]).toMatchObject({ label: 'Event: Gala', type: SharingWithType.Event, sharedWith: 'e1' });
      expect(component.targetsLoading()).toBe(false);
    });

    it('stops the targets spinner even when access loading fails', () => {
      const { component } = createFixture({
        getMyAccess: vi.fn(() => throwError(() => new Error('x'))),
      });
      expect(component.targetsLoading()).toBe(false);
      expect(component.targets()).toEqual([]);
    });
  });

  describe('file selection', () => {
    it('tracks selected files and sets canSubmit', () => {
      const { component } = createFixture({});
      expect(component.canSubmit()).toBe(false);

      component.onFilesSelected({ target: { files: [file('a.mp4')] } } as unknown as Event);

      expect(component.files()).toHaveLength(1);
      expect(component.canSubmit()).toBe(true);
      expect(component.singleFile()).toBe(true);
    });
  });

  describe('upload flow', () => {
    it('uploads a file to the private library and reaches the done stage', () => {
      const { component, uploads, blob } = createFixture({});
      const f = file('lesson.mp4');
      component.files.set([f]);
      component.form.patchValue({ recordedDate: '2026-05-01' });

      component.submit();

      expect(uploads.produceUploadUrl).toHaveBeenCalledWith(
        expect.objectContaining({
          nameOfVideo: 'lesson.mp4',
          sharingWithType: SharingWithType.Private,
        }),
      );
      expect(blob.upload).toHaveBeenCalledWith('sas-url', f);
      expect(component.stage()).toBe('done');
    });

    it('targets an event when that target key is selected', () => {
      const { component, uploads } = createFixture({});
      component.form.patchValue({ targetKey: 'e:e1', recordedDate: '2026-05-01' });
      component.files.set([file('lesson.mp4')]);

      component.submit();

      expect(uploads.produceUploadUrl).toHaveBeenCalledWith(
        expect.objectContaining({ sharingWithType: SharingWithType.Event, sharedWith: 'e1' }),
      );
    });

    it('marks the item as error and completes the batch when the upload fails', () => {
      const { component } = createFixture({
        produceUploadUrl: vi.fn(() => throwError(() => new Error('fail'))),
      });
      component.files.set([file('lesson.mp4')]);

      component.submit();

      expect(component.uploadItems()[0]).toMatchObject({ status: 'error' });
      expect(component.stage()).toBe('done');
    });
  });

  describe('dialog lifecycle', () => {
    it('opening the dialog resets form state and applies the preselected target key', () => {
      const { fixture, component } = createFixture({});

      component.files.set([file('old.mp4')]);
      component.form.patchValue({ name: 'stale', targetKey: 'g:g1' });

      fixture.componentRef.setInput('preselectedTargetKey', 'e:e1');
      fixture.componentRef.setInput('open', true);
      fixture.detectChanges();

      expect(component.files()).toEqual([]);
      expect(component.form.getRawValue()).toEqual({ name: '', recordedDate: '', targetKey: 'e:e1' });
      expect(component.stage()).toBe('form');
    });

    it('opening the dialog without a preselected key defaults to the private library', () => {
      const { fixture, component } = createFixture({});

      fixture.componentRef.setInput('open', true);
      fixture.detectChanges();

      expect(component.form.getRawValue().targetKey).toBe('private');
    });

    it('ignores a preselected key that is not in the targets list', () => {
      const { fixture, component } = createFixture({});

      fixture.componentRef.setInput('preselectedTargetKey', 'e:unknown');
      fixture.componentRef.setInput('open', true);
      fixture.detectChanges();

      expect(component.form.getRawValue().targetKey).toBe('private');
    });

    it('close() emits the closed event and resets state', () => {
      const { component } = createFixture({});
      component.files.set([file('video.mp4')]);
      component.submit();
      expect(component.stage()).toBe('done');

      let closedEmitted = false;
      component.closed.subscribe(() => {
        closedEmitted = true;
      });

      component.close();

      expect(closedEmitted).toBe(true);
      expect(component.stage()).toBe('form');
      expect(component.files()).toEqual([]);
    });

    it('close() does nothing while upload is in progress', () => {
      const uploadSubject = new Subject<number>();
      const { component } = createFixture({ upload: vi.fn(() => uploadSubject) });
      component.files.set([file('video.mp4')]);
      component.submit();
      expect(component.stage()).toBe('uploading');

      let closedEmitted = false;
      component.closed.subscribe(() => {
        closedEmitted = true;
      });

      component.close();

      expect(closedEmitted).toBe(false);
      expect(component.stage()).toBe('uploading');
    });

    it('retryUpload() resets to the form and re-applies the preselected key', () => {
      const { fixture, component } = createFixture({
        produceUploadUrl: vi.fn(() => throwError(() => new Error('fail'))),
      });

      fixture.componentRef.setInput('preselectedTargetKey', 'e:e1');
      fixture.componentRef.setInput('open', true);
      fixture.detectChanges();

      component.files.set([file('video.mp4')]);
      component.submit();
      expect(component.stage()).toBe('done');

      component.retryUpload();

      expect(component.stage()).toBe('form');
      expect(component.files()).toEqual([]);
      expect(component.form.getRawValue().targetKey).toBe('e:e1');
    });
  });
});
