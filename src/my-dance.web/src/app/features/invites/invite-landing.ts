import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, inject, input, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { AuthService } from '../../core/auth/auth.service';
import { InviteLinksService } from '../../core/api/invite-links.service';
import { InviteLinkInfoModel } from '../../core/api/api-models';

type Outcome = 'none' | 'redeemed' | 'alreadyMember';

/**
 * Public-ish landing page for an invite link, reached at /invite/:linkId. Deliberately carries no
 * auth guard (unlike /transfer/:linkId): a signed-out visitor sees an explicit "please sign in"
 * message with a manual action, never an automatic redirect (FR-012). After signing in, the
 * existing return-URL mechanism brings them straight back here, where ngOnInit completes the
 * redemption without them needing to re-open the link (FR-013).
 */
@Component({
  selector: 'app-invite-landing',
  imports: [RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './invite-landing.html',
})
export class InviteLanding implements OnInit {
  /** Bound from the route `:linkId` param via withComponentInputBinding(). */
  readonly linkId = input.required<string>();

  private readonly inviteLinks = inject(InviteLinksService);
  private readonly auth = inject(AuthService);
  private readonly destroyRef = inject(DestroyRef);

  readonly loading = signal(true);
  readonly failed = signal(false);
  readonly info = signal<InviteLinkInfoModel | null>(null);

  readonly submitting = signal(false);
  readonly outcome = signal<Outcome>('none');
  readonly redeemFailed = signal(false);

  readonly isAuthenticated = this.auth.isAuthenticated;

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.failed.set(false);

    this.inviteLinks
      .getInfo(this.linkId())
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (info) => {
          this.info.set(info);
          this.loading.set(false);
          if (info.isRedeemable && this.isAuthenticated()) {
            this.redeem();
          }
        },
        error: () => {
          this.failed.set(true);
          this.loading.set(false);
        },
      });
  }

  signIn(): void {
    this.auth.login();
  }

  redeem(): void {
    if (this.submitting()) {
      return;
    }
    this.submitting.set(true);
    this.redeemFailed.set(false);

    this.inviteLinks
      .redeem(this.linkId())
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (response) => {
          this.submitting.set(false);
          this.outcome.set(response.alreadyMember ? 'alreadyMember' : 'redeemed');
        },
        error: () => {
          this.submitting.set(false);
          this.redeemFailed.set(true);
        },
      });
  }
}
