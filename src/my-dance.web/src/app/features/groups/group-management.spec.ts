import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';

import { GroupManagement } from './group-management';
import { GroupsService } from '../../core/api/groups.service';
import { AccessService } from '../../core/api/access.service';
import { InviteLinksService } from '../../core/api/invite-links.service';

interface Overrides {
  admins?: { userId?: string; firstName?: string; lastName?: string; email?: string }[];
  members?: { userId?: string; email?: string; whenJoined?: Date }[];
  myGroups?: { id?: string; name?: string; seasonStart?: Date; seasonEnd?: Date }[];
  inviteLinks?: { id?: string; status?: string }[];
  addAdmin?: ReturnType<typeof vi.fn>;
  removeAdmin?: ReturnType<typeof vi.fn>;
  updateMember?: ReturnType<typeof vi.fn>;
  removeMember?: ReturnType<typeof vi.fn>;
}

function createFixture(overrides: Overrides = {}) {
  const groups = {
    listAdmins: vi.fn(() => of({ admins: overrides.admins ?? [{ userId: 'a1', email: 'a1@x' }] })),
    listMembers: vi.fn(() => of({ members: overrides.members ?? [{ userId: 'm1', email: 'm1@x' }] })),
    listMyGroups: vi.fn(() =>
      of({
        groups: overrides.myGroups ?? [
          { id: 'g1', name: 'Beginners', seasonStart: new Date('2024-09-01'), seasonEnd: new Date('2025-08-31') },
        ],
      }),
    ),
    addAdmin: overrides.addAdmin ?? vi.fn(() => of(void 0)),
    removeAdmin: overrides.removeAdmin ?? vi.fn(() => of(void 0)),
    updateMember: overrides.updateMember ?? vi.fn(() => of(void 0)),
    removeMember: overrides.removeMember ?? vi.fn(() => of(void 0)),
  };
  const access = {
    listAccessRequests: vi.fn(() => of({ accessRequests: [] })),
    approveAccessRequest: vi.fn(() => of(void 0)),
  };
  const inviteLinks = {
    listForGroup: vi.fn(() => of({ inviteLinks: overrides.inviteLinks ?? [] })),
    createForGroup: vi.fn(() => of({ id: 'l1', url: 'https://example.test/invite/l1' })),
    revoke: vi.fn(() => of(void 0)),
  };

  TestBed.configureTestingModule({
    imports: [GroupManagement],
    providers: [
      { provide: GroupsService, useValue: groups },
      { provide: AccessService, useValue: access },
      { provide: InviteLinksService, useValue: inviteLinks },
    ],
  });

  const fixture = TestBed.createComponent(GroupManagement);
  fixture.componentRef.setInput('groupId', 'g1');
  fixture.detectChanges();
  return { fixture, groups, component: fixture.componentInstance };
}

describe('GroupManagement', () => {
  it('loads admins and members for the group', () => {
    const { component, groups } = createFixture();
    expect(groups.listAdmins).toHaveBeenCalledWith('g1');
    expect(groups.listMembers).toHaveBeenCalledWith('g1');
    expect(component.loading()).toBe(false);
    expect(component.admins()).toHaveLength(1);
    expect(component.members()).toHaveLength(1);
  });

  it('shows the group name and season in the header', () => {
    const { fixture } = createFixture();
    const el = fixture.nativeElement as HTMLElement;
    expect(el.querySelector('h1')?.textContent).toContain('Beginners');
    expect(el.querySelector('h1')?.textContent).toContain('Sep 2024 – Aug 2025');
  });

  it('disallows removing the only admin (last-admin guard)', () => {
    const { component } = createFixture({ admins: [{ userId: 'a1', email: 'a1@x' }] });
    expect(component.canRemoveAdmin()).toBe(false);
  });

  it('allows removing an admin when more than one remains', () => {
    const { component } = createFixture({
      admins: [
        { userId: 'a1', email: 'a1@x' },
        { userId: 'a2', email: 'a2@x' },
      ],
    });
    expect(component.canRemoveAdmin()).toBe(true);
  });

  it('does not call removeAdmin while the last-admin guard is active', () => {
    const removeAdmin = vi.fn(() => of(void 0));
    const { component, groups } = createFixture({ admins: [{ userId: 'a1', email: 'a1@x' }], removeAdmin });
    component.removeAdmin({ userId: 'a1' });
    expect(groups.removeAdmin).not.toHaveBeenCalled();
  });

  it('adds an admin by user id then reloads', () => {
    const { component, groups } = createFixture();
    component.newAdminUserId.set('u9');
    component.addAdmin();
    expect(groups.addAdmin).toHaveBeenCalledWith('g1', { userId: 'u9' });
    expect(component.newAdminUserId()).toBe('');
    expect(groups.listAdmins).toHaveBeenCalledTimes(2);
  });

  it('saves an edited member join date', () => {
    const { component, groups } = createFixture({ members: [{ userId: 'm1', email: 'm1@x' }] });
    component.setJoinDate('m1', '2024-03-04');
    component.saveJoinDate({ userId: 'm1' });
    expect(groups.updateMember).toHaveBeenCalledWith('g1', 'm1', { whenJoined: new Date('2024-03-04') });
  });

  it('removes a member only after confirmation', () => {
    const confirm = vi.spyOn(window, 'confirm').mockReturnValue(false);
    const { component, groups } = createFixture({ members: [{ userId: 'm1', email: 'm1@x' }] });

    component.removeMember({ userId: 'm1' });
    expect(groups.removeMember).not.toHaveBeenCalled();

    confirm.mockReturnValue(true);
    component.removeMember({ userId: 'm1' });
    expect(groups.removeMember).toHaveBeenCalledWith('g1', 'm1');

    confirm.mockRestore();
  });
});
