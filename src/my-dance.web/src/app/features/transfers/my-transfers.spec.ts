import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';

import { MyTransfers } from './my-transfers';
import { TransfersService } from '../../core/api/transfers.service';
import { TransferSummaryResponse } from '../../core/api/api-models';

function createFixture(overrides: {
  getMyTransfers?: ReturnType<typeof vi.fn>;
  revokeTransfer?: ReturnType<typeof vi.fn>;
}) {
  const transfers = {
    getMyTransfers: overrides.getMyTransfers ?? vi.fn(() => of({ transfers: [] })),
    revokeTransfer: overrides.revokeTransfer ?? vi.fn(() => of(void 0)),
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
];

describe('MyTransfers', () => {
  it('loads the outgoing transfers', () => {
    const { component } = createFixture({ getMyTransfers: vi.fn(() => of({ transfers: TRANSFERS })) });
    expect(component.loading()).toBe(false);
    expect(component.items()).toHaveLength(3);
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
  });

  it('revoke marks the pending transfer revoked', () => {
    const { component, transfers } = createFixture({
      getMyTransfers: vi.fn(() => of({ transfers: TRANSFERS })),
    });

    component.revoke(TRANSFERS[0]);

    expect(transfers.revokeTransfer).toHaveBeenCalledWith('t1');
    expect(component.items().find((t) => t.linkId === 't1')?.status).toBe('Revoked');
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
