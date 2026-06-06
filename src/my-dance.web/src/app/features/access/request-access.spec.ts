import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';

import { RequestAccess } from './request-access';
import { AccessService } from '../../core/api/access.service';
import { GetUserAccessResponse } from '../../core/api/api-models';

const DATE = new Date(2026, 0, 1);
const FULL_ACCESS: GetUserAccessResponse = {
  assigned: {
    groups: [{ id: 'ga', name: 'Assigned group' }],
    events: [{ id: 'ea', name: 'Assigned event', date: DATE }],
  },
  available: {
    groups: [{ id: 'g1', name: 'Salsa' }],
    events: [{ id: 'e1', name: 'Gala', date: DATE }],
  },
  pending: { groups: ['gp'], events: ['ep1', 'ep2'] },
};

function createFixture(overrides: {
  getMyAccess?: ReturnType<typeof vi.fn>;
  requestAccess?: ReturnType<typeof vi.fn>;
}) {
  const access = {
    getMyAccess: overrides.getMyAccess ?? vi.fn(() => of(FULL_ACCESS)),
    requestAccess: overrides.requestAccess ?? vi.fn(() => of(void 0)),
  };

  TestBed.configureTestingModule({
    imports: [RequestAccess],
    providers: [{ provide: AccessService, useValue: access }],
  });

  const fixture = TestBed.createComponent(RequestAccess);
  fixture.detectChanges();
  return { fixture, access, component: fixture.componentInstance };
}

describe('RequestAccess', () => {
  it('splits assigned, available, and pending access on load', () => {
    const { component } = createFixture({});
    expect(component.assignedGroups()).toHaveLength(1);
    expect(component.availableGroups()).toHaveLength(1);
    expect(component.availableEvents()).toHaveLength(1);
    expect(component.pendingCount()).toBe(3); // 1 group + 2 events
    expect(component.loading()).toBe(false);
  });

  it('enters the failed state on error', () => {
    const { component } = createFixture({ getMyAccess: vi.fn(() => throwError(() => new Error('x'))) });
    expect(component.failed()).toBe(true);
  });

  describe('selection', () => {
    it('toggles an event on and off', () => {
      const { component } = createFixture({});
      component.toggleEvent('e1');
      expect(component.checkedEvents().has('e1')).toBe(true);
      component.toggleEvent('e1');
      expect(component.checkedEvents().has('e1')).toBe(false);
    });

    it('ignores a toggle with no id', () => {
      const { component } = createFixture({});
      component.toggleEvent(undefined);
      expect(component.checkedEvents().size).toBe(0);
    });

    it('defaults a group join date to today when first checked', () => {
      const { component } = createFixture({});
      component.toggleGroup('g1');
      expect(component.checkedGroups().has('g1')).toBe(true);
      expect(component.groupDates()['g1']).toBe(component.today);
    });

    it('lets a group join date be overridden', () => {
      const { component } = createFixture({});
      component.toggleGroup('g1');
      component.setGroupDate('g1', '2026-01-15');
      expect(component.groupDates()['g1']).toBe('2026-01-15');
    });

    it('drives canSubmit from the current selection', () => {
      const { component } = createFixture({});
      expect(component.canSubmit()).toBe(false);
      component.toggleEvent('e1');
      expect(component.canSubmit()).toBe(true);
    });
  });

  describe('submit', () => {
    it('does nothing without a selection', () => {
      const { component, access } = createFixture({});
      component.submit();
      expect(access.requestAccess).not.toHaveBeenCalled();
    });

    it('builds the request payload from the selection and reloads', () => {
      const requestAccess = vi.fn(() => of(void 0));
      const { component, access } = createFixture({ requestAccess });

      component.toggleEvent('e1');
      component.toggleGroup('g1');
      component.setGroupDate('g1', '2026-03-01');
      component.submit();

      expect(access.requestAccess).toHaveBeenCalledWith({
        events: ['e1'],
        groups: [{ id: 'g1', joinedDate: new Date('2026-03-01') }],
      });
      expect(component.submitting()).toBe(false);
      // Reload clears the selection.
      expect(component.checkedEvents().size).toBe(0);
      expect(component.checkedGroups().size).toBe(0);
      expect(access.getMyAccess).toHaveBeenCalledTimes(2);
    });

    it('clears the submitting flag when the request fails', () => {
      const { component, access } = createFixture({
        requestAccess: vi.fn(() => throwError(() => new Error('x'))),
      });
      component.toggleEvent('e1');
      component.submit();

      expect(component.submitting()).toBe(false);
      // Selection is preserved so the user can retry.
      expect(component.checkedEvents().has('e1')).toBe(true);
      expect(access.getMyAccess).toHaveBeenCalledTimes(1);
    });
  });
});
