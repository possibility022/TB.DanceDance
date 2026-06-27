import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  OnInit,
  computed,
  inject,
  input,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { forkJoin } from 'rxjs';

import { GroupsService } from '../../core/api/groups.service';
import { InviteLinksService } from '../../core/api/invite-links.service';
import {
  GroupAdminModel,
  GroupMemberModel,
  GroupModel,
  InviteLinkModel,
} from '../../core/api/api-models';
import { SeasonRangePipe } from '../../shared/format/season-range.pipe';
import { LongDatePipe } from '../../shared/format/long-date.pipe';
import { CopyLink } from '../../shared/ui/copy-link/copy-link';
import { buildShareMessage } from '../../shared/share/share-message';
import { AccessRequests } from '../access/access-requests';

/** Per-group admin screen: pending requests, members, admins, and invite links. Admin-only (server-enforced). */
@Component({
  selector: 'app-group-management',
  imports: [AccessRequests, SeasonRangePipe, LongDatePipe, CopyLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './group-management.html',
})
export class GroupManagement implements OnInit {
  /** Route param (`groups/:groupId/manage`). */
  readonly groupId = input.required<string>();

  private readonly groups = inject(GroupsService);
  private readonly inviteLinks = inject(InviteLinksService);
  private readonly destroyRef = inject(DestroyRef);

  readonly loading = signal(true);
  readonly failed = signal(false);

  readonly group = signal<GroupModel | null>(null);
  readonly admins = signal<readonly GroupAdminModel[]>([]);
  readonly members = signal<readonly GroupMemberModel[]>([]);

  readonly newAdminUserId = signal('');
  readonly addingAdmin = signal(false);
  readonly processingAdminId = signal<string | null>(null);
  readonly processingMemberId = signal<string | null>(null);

  readonly inviteLinksList = signal<readonly InviteLinkModel[]>([]);
  readonly generatingInviteLink = signal(false);
  readonly newInviteLinkUrl = signal<string | null>(null);
  readonly processingInviteLinkId = signal<string | null>(null);

  /** Pending edits to member join dates, keyed by user id (`yyyy-MM-dd`). */
  readonly editedJoinDates = signal<Readonly<Record<string, string>>>({});

  /** A group must always keep at least one admin, so the last one can't be removed. */
  readonly canRemoveAdmin = computed(() => this.admins().length > 1);

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.failed.set(false);

    forkJoin({
      admins: this.groups.listAdmins(this.groupId()),
      members: this.groups.listMembers(this.groupId()),
      myGroups: this.groups.listMyGroups(),
      inviteLinks: this.inviteLinks.listForGroup(this.groupId()),
    })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: ({ admins, members, myGroups, inviteLinks }) => {
          this.admins.set(admins.admins ?? []);
          this.members.set(members.members ?? []);
          this.group.set((myGroups.groups ?? []).find((g) => g.id === this.groupId()) ?? null);
          this.inviteLinksList.set(inviteLinks.inviteLinks ?? []);
          this.editedJoinDates.set({});
          this.loading.set(false);
        },
        error: () => {
          this.failed.set(true);
          this.loading.set(false);
        },
      });
  }

  generateInviteLink(): void {
    if (this.generatingInviteLink()) {
      return;
    }
    this.generatingInviteLink.set(true);
    this.newInviteLinkUrl.set(null);

    this.inviteLinks
      .createForGroup(this.groupId())
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (link) => {
          this.generatingInviteLink.set(false);
          this.newInviteLinkUrl.set(link.url ?? null);
          this.load();
        },
        error: () => this.generatingInviteLink.set(false),
      });
  }

  revokeInviteLink(link: InviteLinkModel): void {
    if (!link.id || this.processingInviteLinkId()) {
      return;
    }
    if (!window.confirm('Revoke this invite link? It can no longer be used to join.')) {
      return;
    }
    this.processingInviteLinkId.set(link.id);
    this.inviteLinks
      .revoke(link.id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.processingInviteLinkId.set(null);
          this.load();
        },
        error: () => this.processingInviteLinkId.set(null),
      });
  }

  /** A warm, ready-to-send invite message naming this group. */
  inviteMessage(url: string): string {
    return buildShareMessage('group', this.group()?.name, url);
  }

  memberName(entry: GroupAdminModel | GroupMemberModel): string {
    return (
      [entry.firstName, entry.lastName].filter(Boolean).join(' ').trim() ||
      (entry.email ?? 'Unknown')
    );
  }

  /** A member's join date as a `yyyy-MM-dd` string for the date input. */
  joinDateValue(member: GroupMemberModel): string {
    const edited = this.editedJoinDates()[member.userId ?? ''];
    if (edited !== undefined) {
      return edited;
    }
    return member.whenJoined ? new Date(member.whenJoined).toISOString().slice(0, 10) : '';
  }

  setJoinDate(userId: string, date: string): void {
    this.editedJoinDates.update((dates) => ({ ...dates, [userId]: date }));
  }

  addAdmin(): void {
    const userId = this.newAdminUserId().trim();
    if (!userId || this.addingAdmin()) {
      return;
    }
    this.addingAdmin.set(true);
    this.groups
      .addAdmin(this.groupId(), { userId })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.addingAdmin.set(false);
          this.newAdminUserId.set('');
          this.load();
        },
        error: () => this.addingAdmin.set(false),
      });
  }

  removeAdmin(admin: GroupAdminModel): void {
    if (!admin.userId || !this.canRemoveAdmin() || this.processingAdminId()) {
      return;
    }
    if (!window.confirm(`Remove ${this.memberName(admin)} as an admin?`)) {
      return;
    }
    this.processingAdminId.set(admin.userId);
    this.groups
      .removeAdmin(this.groupId(), admin.userId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.processingAdminId.set(null);
          this.load();
        },
        error: () => this.processingAdminId.set(null),
      });
  }

  saveJoinDate(member: GroupMemberModel): void {
    const userId = member.userId;
    const date = userId ? this.editedJoinDates()[userId] : undefined;
    if (!userId || !date || this.processingMemberId()) {
      return;
    }
    this.processingMemberId.set(userId);
    this.groups
      .updateMember(this.groupId(), userId, { whenJoined: new Date(date) })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.processingMemberId.set(null);
          this.load();
        },
        error: () => this.processingMemberId.set(null),
      });
  }

  removeMember(member: GroupMemberModel): void {
    if (!member.userId || this.processingMemberId()) {
      return;
    }
    if (!window.confirm(`Revoke access for ${this.memberName(member)}?`)) {
      return;
    }
    this.processingMemberId.set(member.userId);
    this.groups
      .removeMember(this.groupId(), member.userId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.processingMemberId.set(null);
          this.load();
        },
        error: () => this.processingMemberId.set(null),
      });
  }
}
