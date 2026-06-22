import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';

import { GroupsList } from './groups-list';
import { GroupsService } from '../../core/api/groups.service';

const seasonGroup = {
  id: 'g1',
  name: 'Beginners',
  seasonStart: new Date('2024-09-01'),
  seasonEnd: new Date('2025-08-31'),
};

function createFixture(listMyGroups = vi.fn(() => of({ groups: [seasonGroup] }))) {
  TestBed.configureTestingModule({
    imports: [GroupsList],
    providers: [provideRouter([]), { provide: GroupsService, useValue: { listMyGroups } }],
  });

  const fixture = TestBed.createComponent(GroupsList);
  fixture.detectChanges();
  return { fixture, listMyGroups, component: fixture.componentInstance };
}

describe('GroupsList', () => {
  it('loads the groups the user administers', () => {
    const { component, listMyGroups } = createFixture();
    expect(listMyGroups).toHaveBeenCalledTimes(1);
    expect(component.loading()).toBe(false);
    expect(component.items()).toEqual([seasonGroup]);
  });

  it('renders a link to each group\'s management page with its season', () => {
    const { fixture } = createFixture();
    const el = fixture.nativeElement as HTMLElement;
    const link = el.querySelector('a[href="/groups/g1/manage"]');
    expect(link?.textContent).toContain('Beginners');
    expect(link?.textContent).toContain('Sep 2024 – Aug 2025');
  });

  it('shows an empty state when the user administers no groups', () => {
    const { fixture } = createFixture(vi.fn(() => of({ groups: [] })));
    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain("don't administer any groups");
  });

  it('shows a failure state and allows retrying', () => {
    const listMyGroups = vi
      .fn()
      .mockReturnValueOnce(throwError(() => new Error('boom')))
      .mockReturnValueOnce(of({ groups: [{ id: 'g1', name: 'Beginners' }] }));
    const { component, fixture } = createFixture(listMyGroups);

    expect(component.failed()).toBe(true);

    component.load();
    expect(component.failed()).toBe(false);
    expect(component.items()).toHaveLength(1);
    fixture.detectChanges();
  });
});
