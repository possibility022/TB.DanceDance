import { TestBed } from '@angular/core/testing';
import { Subject, of, throwError } from 'rxjs';

import { AccessRequests } from './access-requests';
import { AccessService } from '../../core/api/access.service';
import { RequestedAccessModel } from '../../core/api/api-models';

function createFixture(overrides: {
  listAccessRequests?: ReturnType<typeof vi.fn>;
  approveAccessRequest?: ReturnType<typeof vi.fn>;
}) {
  const access = {
    listAccessRequests:
      overrides.listAccessRequests ??
      vi.fn(() => of({ accessRequests: [{ requestId: 'r1', name: 'Salsa', isGroup: true }] })),
    approveAccessRequest: overrides.approveAccessRequest ?? vi.fn(() => of(void 0)),
  };

  TestBed.configureTestingModule({
    imports: [AccessRequests],
    providers: [{ provide: AccessService, useValue: access }],
  });

  const fixture = TestBed.createComponent(AccessRequests);
  fixture.detectChanges();
  return { fixture, access, component: fixture.componentInstance };
}

describe('AccessRequests', () => {
  it('loads pending requests', () => {
    const { component } = createFixture({});
    expect(component.loading()).toBe(false);
    expect(component.requests()).toHaveLength(1);
  });

  it('enters the failed state on error', () => {
    const { component } = createFixture({ listAccessRequests: vi.fn(() => throwError(() => new Error('x'))) });
    expect(component.failed()).toBe(true);
  });

  describe('requestorName', () => {
    it('joins first and last name', () => {
      const { component } = createFixture({});
      const req: RequestedAccessModel = { requestorFirstName: 'Ada', requestorLastName: 'Lovelace' };
      expect(component.requestorName(req)).toBe('Ada Lovelace');
    });

    it('uses whichever name part is present', () => {
      const { component } = createFixture({});
      expect(component.requestorName({ requestorFirstName: 'Ada' })).toBe('Ada');
    });

    it('falls back to "Someone" when no name is present', () => {
      const { component } = createFixture({});
      expect(component.requestorName({})).toBe('Someone');
    });
  });

  describe('decide', () => {
    it('approves a request then reloads the list', () => {
      const approveAccessRequest = vi.fn(() => of(void 0));
      const { component, access } = createFixture({ approveAccessRequest });

      component.decide({ requestId: 'r1', isGroup: true }, true);

      expect(access.approveAccessRequest).toHaveBeenCalledWith({
        requestId: 'r1',
        isGroup: true,
        isApproved: true,
      });
      expect(component.processingId()).toBeNull();
      expect(access.listAccessRequests).toHaveBeenCalledTimes(2);
    });

    it('rejects a request with isApproved=false', () => {
      const approveAccessRequest = vi.fn(() => of(void 0));
      const { component } = createFixture({ approveAccessRequest });
      component.decide({ requestId: 'r1', isGroup: false }, false);
      expect(approveAccessRequest).toHaveBeenCalledWith({
        requestId: 'r1',
        isGroup: false,
        isApproved: false,
      });
    });

    it('ignores a request without an id', () => {
      const { component, access } = createFixture({});
      component.decide({}, true);
      expect(access.approveAccessRequest).not.toHaveBeenCalled();
    });

    it('ignores a decision while another is in flight', () => {
      // A never-completing observable keeps processingId set.
      const approveAccessRequest = vi.fn(() => new Subject<void>());
      const { component, access } = createFixture({ approveAccessRequest });

      component.decide({ requestId: 'r1', isGroup: true }, true);
      component.decide({ requestId: 'r2', isGroup: true }, true);

      expect(access.approveAccessRequest).toHaveBeenCalledTimes(1);
    });

    it('clears the processing flag when the decision fails', () => {
      const approveAccessRequest = vi.fn(() => throwError(() => new Error('x')));
      const { component, access } = createFixture({ approveAccessRequest });

      component.decide({ requestId: 'r1', isGroup: true }, true);

      expect(component.processingId()).toBeNull();
      expect(access.listAccessRequests).toHaveBeenCalledTimes(1); // no reload on failure
    });
  });
});
