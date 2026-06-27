import { ChangeDetectionStrategy, Component, inject } from '@angular/core';

import { AuthService } from '../../../core/auth/auth.service';

/** Icon keys for the capability cards; rendered as decorative inline SVGs. */
type CapabilityIcon = 'trophy' | 'library' | 'collection';

interface CapabilityDescriptor {
  readonly icon: CapabilityIcon;
  readonly title: string;
  readonly description: string;
}

/**
 * Public introduction shown on the home page to visitors who are not logged in.
 * Explains, in priority order, what the application lets people do, with a
 * sign-in call to action at the top and after the descriptions.
 */
@Component({
  selector: 'app-landing',
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './landing.html',
  styles: `
    :host {
      display: block;
    }

    .landing-hero {
      background: color-mix(in srgb, var(--bulma-primary) 4%, transparent);
    }

    .landing-hero__title {
      color: var(--bulma-primary);
    }

    .landing-hero__rule {
      width: 4rem;
      height: 3px;
      margin: 0.75rem auto 1.25rem;
      border: 0;
      border-radius: 999px;
      background: var(--bulma-primary);
    }

    .landing-hero__lead {
      margin-inline: auto;
      max-width: 42rem;
    }

    .landing-intro {
      margin-bottom: 0;
    }

    .landing-capability {
      height: 100%;
      padding: 1.5rem 1.25rem;
    }

    .landing-capability__icon {
      display: inline-grid;
      width: 4rem;
      height: 4rem;
      place-items: center;
      margin-bottom: 0.75rem;
      border-radius: 999px;
      background: color-mix(in srgb, var(--bulma-primary) 14%, transparent);
      color: var(--bulma-primary);
    }
  `,
})
export class Landing {
  private readonly auth = inject(AuthService);

  /** Ordered by importance — competitions & events, personal library, then sharing. */
  readonly capabilities: readonly CapabilityDescriptor[] = [
    {
      icon: 'trophy',
      title: 'Competitions & events',
      description:
        'Group your footage by competition or event — your starts, your recordings, all in one place.',
    },
    {
      icon: 'library',
      title: 'Your personal library',
      description:
        'Upload and keep your own recordings, neatly organised by the groups and classes you attend.',
    },
    {
      icon: 'collection',
      title: 'Share for feedback',
      description:
        'Send any recording to a coach or peer with a single link — no account needed on their end.',
    },
  ];

  login(): void {
    this.auth.login();
  }
}
