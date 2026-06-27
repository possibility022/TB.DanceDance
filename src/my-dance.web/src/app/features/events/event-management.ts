import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  OnInit,
  inject,
  input,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { forkJoin } from 'rxjs';

import { InviteLinksService } from '../../core/api/invite-links.service';
import { AccessService } from '../../core/api/access.service';
import { InviteLinkModel } from '../../core/api/api-models';
import { LongDatePipe } from '../../shared/format/long-date.pipe';
import { CopyLink } from '../../shared/ui/copy-link/copy-link';
import { buildShareMessage } from '../../shared/share/share-message';

/**
 * Minimal, invite-link-only admin screen for a single event (route: `events/:eventId/manage`).
 * No broader event-membership UI exists yet — this is scoped strictly to invite links.
 * Admin-only; the server enforces the owner check.
 */
@Component({
  selector: 'app-event-management',
  imports: [LongDatePipe, CopyLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './event-management.html',
})
export class EventManagement implements OnInit {
  /** Route param (`events/:eventId/manage`). */
  readonly eventId = input.required<string>();

  private readonly inviteLinks = inject(InviteLinksService);
  private readonly access = inject(AccessService);
  private readonly destroyRef = inject(DestroyRef);

  readonly loading = signal(true);
  readonly failed = signal(false);
  readonly links = signal<readonly InviteLinkModel[]>([]);
  readonly eventName = signal<string | null>(null);

  readonly generating = signal(false);
  readonly newLinkUrl = signal<string | null>(null);
  readonly processingLinkId = signal<string | null>(null);

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.failed.set(false);

    forkJoin({
      links: this.inviteLinks.listForEvent(this.eventId()),
      access: this.access.getAllEventsAndGroups(),
    })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: ({ links, access }) => {
          this.links.set(links.inviteLinks ?? []);
          this.eventName.set(
            (access.events ?? []).find((e) => e.id === this.eventId())?.name ?? null,
          );
          this.loading.set(false);
        },
        error: () => {
          this.failed.set(true);
          this.loading.set(false);
        },
      });
  }

  /** A warm, ready-to-send invite message naming this event. */
  inviteMessage(url: string): string {
    return buildShareMessage('event', this.eventName() ?? undefined, url);
  }

  generateInviteLink(): void {
    if (this.generating()) {
      return;
    }
    this.generating.set(true);
    this.newLinkUrl.set(null);

    this.inviteLinks
      .createForEvent(this.eventId())
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (link) => {
          this.generating.set(false);
          this.newLinkUrl.set(link.url ?? null);
          this.load();
        },
        error: () => this.generating.set(false),
      });
  }

  revoke(link: InviteLinkModel): void {
    if (!link.id || this.processingLinkId()) {
      return;
    }
    if (!window.confirm('Revoke this invite link? It can no longer be used to join.')) {
      return;
    }
    this.processingLinkId.set(link.id);
    this.inviteLinks
      .revoke(link.id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.processingLinkId.set(null);
          this.load();
        },
        error: () => this.processingLinkId.set(null),
      });
  }
}
