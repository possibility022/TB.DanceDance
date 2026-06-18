import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';

import { MyTransfers } from './my-transfers';
import { TransfersService } from '../../core/api/transfers.service';
import { TransferSummaryResponse } from '../../core/api/api-models';

function createFixture(overrides: {
  getMyTransfers?: ReturnType<typeof vi.fn>;
  revokeTransfer?: ReturnType<typeof vi.fn>;
  rollbackTransfer?: ReturnType<typeof vi.fn>;
}) {
  const transfers = {
    getMyTransfers: overrides.getMyTransfers ?? vi.fn(() => of({ transfers: [] })),
    revokeTransfer: overrides.revokeTransfer ?? vi.fn(() => of(void 0)),
    rollbackTransfer: overrides.rollbackTransfer ?? vi.fn(() => of(void 0)),
  };

  TestBed.configureTestingModule({
    imports: [MyTransfers],
    providers: [{ provide: TransfersService, useValue: transfers }],
  });

  const fixture: ComponentFixture<MyTransfers> = TestBed.createComponent(MyTransfers);
  fixture.detectChanges();
  return { fixture, transfers, component: fixture.componentInstance };
}

const FUTURE = new Date(Date.now() + 10 * 24 * 60 * 60 * 1000);
const PAST = new Date(Date.now() - 1 * 24 * 60 * 60 * 1000);

const TRANSFERS: TransferSummaryResponse[] = [
  { linkId: 't1', status: 'Pending', totalSizeBytes: 100, shareUrl: 'https://x/transfer/t1' },
  { linkId: 't2', status: 'Accepted', totalSizeBytes: 200, rollbackDeadline: FUTURE },
  { linkId: 't3', status: 'Revoked', totalSizeBytes: 300 },
  { linkId: 't4', status: 'RolledBack', totalSizeBytes: 400 },
  { linkId: 't5', status: 'Accepted', totalSizeBytes: 500, rollbackDeadline: PAST },
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
    expect(component.isPending({ status: 'RolledBack' })).toBe(false);
  });

  it('isAccepted returns true only for Accepted status', () => {
    const { component } = createFixture({});
    expect(component.isAccepted({ status: 'Accepted' })).toBe(true);
    expect(component.isAccepted({ status: 'Pending' })).toBe(false);
    expect(component.isAccepted({ status: 'RolledBack' })).toBe(false);
  });

  it('canRollback is true only while Accepted and the deadline is in the future', () => {
    const { component } = createFixture({});
    expect(component.canRollback({ status: 'Accepted', rollbackDeadline: FUTURE })).toBe(true);
    expect(component.canRollback({ status: 'Accepted', rollbackDeadline: PAST })).toBe(false);
    expect(component.canRollback({ status: 'Accepted' })).toBe(false);
    expect(component.canRollback({ status: 'Pending', rollbackDeadline: FUTURE })).toBe(false);
  });

  it('revoke marks the pending transfer revoked', () => {
    const { component, transfers } = createFixture({
      getMyTransfers: vi.fn(() => of({ transfers: TRANSFERS })),
    });

    component.revoke(TRANSFERS[0]);

    expect(transfers.revokeTransfer).toHaveBeenCalledWith('t1');
    expect(component.items().find((t) => t.linkId === 't1')?.status).toBe('Revoked');
  });

  it('rollback calls the rollback endpoint and marks the transfer RolledBack', () => {
    const { component, transfers } = createFixture({
      getMyTransfers: vi.fn(() => of({ transfers: TRANSFERS })),
    });

    component.rollback(TRANSFERS[1]); // t2 is Accepted, within window

    expect(transfers.rollbackTransfer).toHaveBeenCalledWith('t2');
    expect(component.items().find((t) => t.linkId === 't2')?.status).toBe('RolledBack');
  });

  it('rollback ignores a transfer with no link id', () => {
    const { component, transfers } = createFixture({});
    component.rollback({});
    expect(transfers.rollbackTransfer).not.toHaveBeenCalled();
  });

  it('terminal statuses are neither pending nor accepted', () => {
    const { component } = createFixture({});
    for (const status of ['Revoked', 'Declined', 'RolledBack', 'Expired']) {
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
