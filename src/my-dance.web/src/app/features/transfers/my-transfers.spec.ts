import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpErrorResponse } from '@angular/common/http';
import { of, throwError } from 'rxjs';

import { MyTransfers } from './my-transfers';
import { TransfersService } from '../../core/api/transfers.service';
import { TransferSummaryResponse } from '../../core/api/api-models';

function createFixture(overrides: {
  getMyTransfers?: ReturnType<typeof vi.fn>;
  revokeTransfer?: ReturnType<typeof vi.fn>;
  approveTransfer?: ReturnType<typeof vi.fn>;
  cancelTransfer?: ReturnType<typeof vi.fn>;
}) {
  const transfers = {
    getMyTransfers: overrides.getMyTransfers ?? vi.fn(() => of({ transfers: [] })),
    revokeTransfer: overrides.revokeTransfer ?? vi.fn(() => of(void 0)),
    approveTransfer: overrides.approveTransfer ?? vi.fn(() => of({ accepted: true })),
    cancelTransfer: overrides.cancelTransfer ?? vi.fn(() => of(void 0)),
  };

  TestBed.configureTestingModule({
    imports: [MyTransfers],
    providers: [{ provide: TransfersService, useValue: transfers }],
  });

  const fixture: ComponentFixture<MyTransfers> = TestBed.createComponent(MyTransfers);
  fixture.detectChanges();
  return { fixture, transfers, component: fixture.componentInstance };
}

const TRANSFERS: TransferSummaryResponse[] = [
  { linkId: 't1', status: 'Pending', totalSizeBytes: 100, shareUrl: 'https://x/transfer/t1' },
  { linkId: 't2', status: 'Accepted', totalSizeBytes: 200 },
  { linkId: 't3', status: 'Revoked', totalSizeBytes: 300 },
  { linkId: 't4', status: 'Approved', totalSizeBytes: 400 },
  { linkId: 't5', status: 'Cancelled', totalSizeBytes: 500 },
];

describe('MyTransfers', () => {
  it('loads the outgoing transfers', () => {
    const { component } = createFixture({ getMyTransfers: vi.fn(() => of({ transfers: TRANSFERS })) });
    expect(component.loading()).toBe(false);
    expect(component.items()).toHaveLength(5);
  });

  it('enters the failed state on error', () => {
    const { component } = createFixture({ getMyTransfers: vi.fn(() => throwError(() => new Error('x'))) });
    expect(component.failed()).toBe(true);
  });

  it('isPending reflects the status across the lifecycle', () => {
    const { component } = createFixture({});
    expect(component.isPending({ status: 'Pending' })).toBe(true);
    expect(component.isPending({ status: 'Accepted' })).toBe(false);
    expect(component.isPending({ status: 'Revoked' })).toBe(false);
    expect(component.isPending({ status: 'Expired' })).toBe(false);
    expect(component.isPending({ status: 'Approved' })).toBe(false);
    expect(component.isPending({ status: 'Cancelled' })).toBe(false);
  });

  it('isAccepted returns true only for Accepted status', () => {
    const { component } = createFixture({});
    expect(component.isAccepted({ status: 'Accepted' })).toBe(true);
    expect(component.isAccepted({ status: 'Pending' })).toBe(false);
    expect(component.isAccepted({ status: 'Approved' })).toBe(false);
  });

  it('revoke marks the pending transfer revoked', () => {
    const { component, transfers } = createFixture({
      getMyTransfers: vi.fn(() => of({ transfers: TRANSFERS })),
    });

    component.revoke(TRANSFERS[0]);

    expect(transfers.revokeTransfer).toHaveBeenCalledWith('t1');
    expect(component.items().find((t) => t.linkId === 't1')?.status).toBe('Revoked');
  });

  it('approve calls the approve endpoint and marks the transfer Approved', () => {
    const { component, transfers } = createFixture({
      getMyTransfers: vi.fn(() => of({ transfers: TRANSFERS })),
    });

    component.approve(TRANSFERS[1]); // t2 is Accepted

    expect(transfers.approveTransfer).toHaveBeenCalledWith('t2');
    expect(component.items().find((t) => t.linkId === 't2')?.status).toBe('Approved');
  });

  it('approve 409 sets inline quota error for that transfer', () => {
    const { component } = createFixture({
      getMyTransfers: vi.fn(() => of({ transfers: TRANSFERS })),
      approveTransfer: vi.fn(() =>
        throwError(
          () =>
            new HttpErrorResponse({
              status: 409,
              error: { accepted: false, requiredBytes: 200, availableBytes: 50 },
            }),
        ),
      ),
    });

    component.approve(TRANSFERS[1]);

    const err = component.approveQuotaError();
    expect(err?.linkId).toBe('t2');
    expect(err?.required).toBe(200);
    expect(err?.available).toBe(50);
    // Status must NOT have changed
    expect(component.items().find((t) => t.linkId === 't2')?.status).toBe('Accepted');
  });

  it('cancel marks the accepted transfer Cancelled and clears quota error', () => {
    const { component, transfers } = createFixture({
      getMyTransfers: vi.fn(() => of({ transfers: TRANSFERS })),
    });

    component.cancel(TRANSFERS[1]); // t2 is Accepted

    expect(transfers.cancelTransfer).toHaveBeenCalledWith('t2');
    expect(component.items().find((t) => t.linkId === 't2')?.status).toBe('Cancelled');
    expect(component.approveQuotaError()).toBeNull();
  });

  it('terminal statuses are neither pending nor accepted', () => {
    const { component } = createFixture({});
    for (const status of ['Revoked', 'Declined', 'Cancelled', 'Approved', 'Expired']) {
      expect(component.isPending({ status })).toBe(false);
      expect(component.isAccepted({ status })).toBe(false);
    }
  });

  it('shareUrl uses the server url when absolute, else falls back to the origin', () => {
    const { component } = createFixture({});
    expect(component.shareUrl({ linkId: 't1', shareUrl: 'https://x/transfer/t1' })).toBe('https://x/transfer/t1');
    expect(component.shareUrl({ linkId: 't1', shareUrl: '/transfer/t1' })).toBe(
      `${window.location.origin}/transfer/t1`,
    );
  });

  it('revoke ignores a transfer with no link id', () => {
    const { component, transfers } = createFixture({});
    component.revoke({});
    expect(transfers.revokeTransfer).not.toHaveBeenCalled();
  });
});
