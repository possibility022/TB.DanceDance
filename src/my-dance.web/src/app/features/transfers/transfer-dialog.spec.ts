import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';

import { TransferDialog } from './transfer-dialog';
import { TransfersService } from '../../core/api/transfers.service';

function createFixture(overrides: {
  createTransfer?: ReturnType<typeof vi.fn>;
  getMyTransfers?: ReturnType<typeof vi.fn>;
  revokeTransfer?: ReturnType<typeof vi.fn>;
}) {
  const transfers = {
    createTransfer:
      overrides.createTransfer ??
      vi.fn(() => of({ linkId: 't1', shareUrl: 'https://x/transfer/t1' })),
    getMyTransfers: overrides.getMyTransfers ?? vi.fn(() => of({ transfers: [] })),
    revokeTransfer: overrides.revokeTransfer ?? vi.fn(() => of(void 0)),
  };

  TestBed.configureTestingModule({
    imports: [TransferDialog],
    providers: [{ provide: TransfersService, useValue: transfers }],
  });

  const fixture: ComponentFixture<TransferDialog> = TestBed.createComponent(TransferDialog);
  return { fixture, transfers, component: fixture.componentInstance };
}

function open(fixture: ComponentFixture<TransferDialog>, videoId = 'v1'): void {
  fixture.componentRef.setInput('videoId', videoId);
  fixture.componentRef.setInput('videoName', 'Tango basics');
  fixture.componentRef.setInput('open', true);
  fixture.detectChanges();
}

describe('TransferDialog', () => {
  it('defaults the expiry to 7 days', () => {
    const { component } = createFixture({});
    expect(component.transferForm.getRawValue()).toEqual({ expirationDays: 7 });
  });

  describe('transfer()', () => {
    it('posts to createTransfer with the videoId and form body', () => {
      const { fixture, component, transfers } = createFixture({});
      open(fixture);

      component.transfer();

      expect(transfers.createTransfer).toHaveBeenCalledWith('v1', { expirationDays: 7 });
      expect(component.transferResult()?.linkId).toBe('t1');
      expect(component.transferring()).toBe(false);
    });

    it('does nothing when the form is invalid', () => {
      const { fixture, component, transfers } = createFixture({});
      open(fixture);
      component.transferForm.controls.expirationDays.setValue(0);

      component.transfer();

      expect(transfers.createTransfer).not.toHaveBeenCalled();
    });

    it('does nothing when there is no videoId', () => {
      const { fixture, component, transfers } = createFixture({});
      open(fixture, '');

      component.transfer();

      expect(transfers.createTransfer).not.toHaveBeenCalled();
    });

    it('sets transferFailed on error and clears transferring', () => {
      const { fixture, component } = createFixture({
        createTransfer: vi.fn(() => throwError(() => new Error('x'))),
      });
      open(fixture);

      component.transfer();

      expect(component.transferFailed()).toBe(true);
      expect(component.transferring()).toBe(false);
    });
  });

  describe('shareUrl()', () => {
    it('returns the absolute URL from the server', () => {
      const { fixture, component } = createFixture({});
      open(fixture);

      component.transfer();

      expect(component.shareUrl()).toBe('https://x/transfer/t1');
    });

    it('falls back to the origin for a relative server URL', () => {
      const { fixture, component } = createFixture({
        createTransfer: vi.fn(() => of({ linkId: 't1', shareUrl: '/transfer/t1' })),
      });
      open(fixture);

      component.transfer();

      expect(component.shareUrl()).toBe(`${window.location.origin}/transfer/t1`);
    });
  });

  describe('existing pending transfer', () => {
    it('surfaces a pending transfer for this video when opened', () => {
      const pending = { linkId: 'p1', status: 'Pending', items: [{ videoId: 'v1' }] };
      const { fixture, component } = createFixture({
        getMyTransfers: vi.fn(() => of({ transfers: [pending] })),
      });

      open(fixture);

      expect(component.existingTransfer()?.linkId).toBe('p1');
    });

    it('ignores pending transfers for other videos', () => {
      const other = { linkId: 'p2', status: 'Pending', items: [{ videoId: 'other' }] };
      const { fixture, component } = createFixture({
        getMyTransfers: vi.fn(() => of({ transfers: [other] })),
      });

      open(fixture);

      expect(component.existingTransfer()).toBeNull();
    });

    it('revokes the existing transfer and clears it', () => {
      const pending = { linkId: 'p1', status: 'Pending', items: [{ videoId: 'v1' }] };
      const { fixture, component, transfers } = createFixture({
        getMyTransfers: vi.fn(() => of({ transfers: [pending] })),
      });
      open(fixture);

      component.revoke();

      expect(transfers.revokeTransfer).toHaveBeenCalledWith('p1');
      expect(component.existingTransfer()).toBeNull();
      expect(component.revoking()).toBe(false);
    });
  });

  describe('close()', () => {
    it('resets all state and emits closed', () => {
      const { fixture, component } = createFixture({});
      open(fixture);
      component.transfer();
      component.transferFailed.set(true);

      let emitted = false;
      component.closed.subscribe(() => (emitted = true));
      component.close();

      expect(component.transferResult()).toBeNull();
      expect(component.transferFailed()).toBe(false);
      expect(component.existingTransfer()).toBeNull();
      expect(component.transferForm.getRawValue()).toEqual({ expirationDays: 7 });
      expect(emitted).toBe(true);
    });
  });
});
