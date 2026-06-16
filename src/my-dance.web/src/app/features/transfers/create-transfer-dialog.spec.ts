import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';

import { CreateTransferDialog } from './create-transfer-dialog';
import { TransfersService } from '../../core/api/transfers.service';
import { VideoInformation } from '../../core/api/api-models';

function createFixture(overrides: { createTransfer?: ReturnType<typeof vi.fn> } = {}) {
  const transfers = {
    createTransfer: overrides.createTransfer ?? vi.fn(() => of({ linkId: 't1', shareUrl: 'https://x/transfer/t1' })),
  };

  TestBed.configureTestingModule({
    imports: [CreateTransferDialog],
    providers: [{ provide: TransfersService, useValue: transfers }],
  });

  const fixture: ComponentFixture<CreateTransferDialog> = TestBed.createComponent(CreateTransferDialog);
  return { fixture, transfers, component: fixture.componentInstance };
}

const VIDEOS: VideoInformation[] = [
  { videoId: 'v1', name: 'One', sizeBytes: 1000 },
  { videoId: 'v2', name: 'Two', sizeBytes: 2000 },
];

function open(fixture: ComponentFixture<CreateTransferDialog>, videos: VideoInformation[] = VIDEOS): void {
  fixture.componentRef.setInput('videos', videos);
  fixture.componentRef.setInput('open', true);
  fixture.detectChanges();
}

describe('CreateTransferDialog', () => {
  it('computes the total size of the selected videos', () => {
    const { fixture, component } = createFixture();
    open(fixture);
    expect(component.totalSizeBytes()).toBe(3000);
  });

  it('creates a transfer from the selected video ids and exposes the link', () => {
    const { fixture, component, transfers } = createFixture();
    open(fixture);

    component.create();

    expect(transfers.createTransfer).toHaveBeenCalledWith({ videoIds: ['v1', 'v2'], expirationDays: 7 });
    expect(component.result()?.shareUrl).toBe('https://x/transfer/t1');
    expect(component.creating()).toBe(false);
  });

  it('emits created after a successful transfer', () => {
    const { fixture, component } = createFixture();
    open(fixture);
    let emitted = false;
    component.created.subscribe(() => (emitted = true));

    component.create();

    expect(emitted).toBe(true);
  });

  it('does nothing when there are no videos', () => {
    const { fixture, component, transfers } = createFixture();
    open(fixture, []);
    component.create();
    expect(transfers.createTransfer).not.toHaveBeenCalled();
  });

  it('does nothing when the form is invalid', () => {
    const { fixture, component, transfers } = createFixture();
    open(fixture);
    component.form.controls.expirationDays.setValue(0); // below min
    component.create();
    expect(transfers.createTransfer).not.toHaveBeenCalled();
  });

  it('flags failure and clears the creating flag on error', () => {
    const { fixture, component } = createFixture({
      createTransfer: vi.fn(() => throwError(() => new Error('x'))),
    });
    open(fixture);
    component.create();
    expect(component.failed()).toBe(true);
    expect(component.creating()).toBe(false);
  });

  it('close() resets state and emits closed', () => {
    const { fixture, component } = createFixture();
    open(fixture);
    component.result.set({ linkId: 't1', shareUrl: 'https://x/transfer/t1' });
    let emitted = false;
    component.closed.subscribe(() => (emitted = true));

    component.close();

    expect(component.result()).toBeNull();
    expect(emitted).toBe(true);
  });
});
