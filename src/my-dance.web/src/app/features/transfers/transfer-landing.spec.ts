import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpErrorResponse } from '@angular/common/http';
import { provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';

import { TransferLanding } from './transfer-landing';
import { TransfersService } from '../../core/api/transfers.service';
import { AuthService } from '../../core/auth/auth.service';
import { TransferInfoResponse } from '../../core/api/api-models';

const INFO: TransferInfoResponse = {
  linkId: 't1',
  status: 'Pending',
  totalSizeBytes: 3000,
  items: [
    { videoId: 'v1', name: 'One', sizeBytes: 1000 },
    { videoId: 'v2', name: 'Two', sizeBytes: 2000 },
  ],
};

const INFO_ACCEPTED: TransferInfoResponse = { ...INFO, status: 'Accepted' };
const INFO_APPROVED: TransferInfoResponse = { ...INFO, status: 'Approved' };

function createFixture(overrides: {
  getTransfer?: ReturnType<typeof vi.fn>;
  acceptTransfer?: ReturnType<typeof vi.fn>;
  declineTransfer?: ReturnType<typeof vi.fn>;
}) {
  const transfers = {
    getTransfer: overrides.getTransfer ?? vi.fn(() => of(INFO)),
    acceptTransfer: overrides.acceptTransfer ?? vi.fn(() => of({ accepted: true })),
    declineTransfer: overrides.declineTransfer ?? vi.fn(() => of(void 0)),
    transferStreamUrl: vi.fn((linkId: string, videoId: string) => `stream/${linkId}/${videoId}`),
  };
  const auth = { getAccessToken: vi.fn(() => of('tok')) };

  TestBed.configureTestingModule({
    imports: [TransferLanding],
    providers: [
      provideRouter([]),
      { provide: TransfersService, useValue: transfers },
      { provide: AuthService, useValue: auth },
    ],
  });

  const fixture: ComponentFixture<TransferLanding> = TestBed.createComponent(TransferLanding);
  fixture.componentRef.setInput('linkId', 't1');
  return { fixture, transfers, auth, component: fixture.componentInstance };
}

describe('TransferLanding', () => {
  it('loads the transfer and builds per-item stream urls', () => {
    const { fixture, component } = createFixture({});
    fixture.detectChanges(); // triggers ngOnInit

    expect(component.loading()).toBe(false);
    expect(component.info()?.items).toHaveLength(2);
    expect(component.streamUrl('v1')).toBe('stream/t1/v1');
  });

  it('enters the failed state when the link is invalid', () => {
    const { fixture, component } = createFixture({
      getTransfer: vi.fn(() => throwError(() => new Error('not found'))),
    });
    fixture.detectChanges();
    expect(component.failed()).toBe(true);
  });

  it('accept success sets accepted-pending outcome', () => {
    const { fixture, component, transfers } = createFixture({});
    fixture.detectChanges();

    component.accept();

    expect(transfers.acceptTransfer).toHaveBeenCalledWith('t1');
    expect(component.outcome()).toBe('accepted-pending');
  });

  it('accept quota block (409) surfaces required/available bytes', () => {
    const { fixture, component } = createFixture({
      acceptTransfer: vi.fn(() =>
        throwError(
          () =>
            new HttpErrorResponse({
              status: 409,
              error: { accepted: false, requiredBytes: 100, availableBytes: 50 },
            }),
        ),
      ),
    });
    fixture.detectChanges();

    component.accept();

    expect(component.quotaError()).toEqual({ required: 100, available: 50 });
    expect(component.outcome()).toBe('none');
  });

  it('accept other error flags actionFailed', () => {
    const { fixture, component } = createFixture({
      acceptTransfer: vi.fn(() => throwError(() => new HttpErrorResponse({ status: 404 }))),
    });
    fixture.detectChanges();

    component.accept();

    expect(component.actionFailed()).toBe(true);
  });

  it('decline sets the declined outcome', () => {
    const { fixture, component, transfers } = createFixture({});
    fixture.detectChanges();

    component.decline();

    expect(transfers.declineTransfer).toHaveBeenCalledWith('t1');
    expect(component.outcome()).toBe('declined');
  });

  it('shows waiting-for-approval when loaded with Accepted status', () => {
    const { fixture, component } = createFixture({
      getTransfer: vi.fn(() => of(INFO_ACCEPTED)),
    });
    fixture.detectChanges();

    // The info is loaded; outcome remains 'none' but info.status drives the UI
    expect(component.info()?.status).toBe('Accepted');
    expect(component.outcome()).toBe('none');
  });

  it('shows approved state when loaded with Approved status', () => {
    const { fixture, component } = createFixture({
      getTransfer: vi.fn(() => of(INFO_APPROVED)),
    });
    fixture.detectChanges();

    expect(component.info()?.status).toBe('Approved');
    expect(component.outcome()).toBe('none');
  });
});
