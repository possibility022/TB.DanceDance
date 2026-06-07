import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { Params, RouterLink } from '@angular/router';

import { VideoInformation } from '../../../core/api/api-models';
import { LongDatePipe } from '../../format/long-date.pipe';

/** A single recording: name, recorded date, duration, and actions. */
@Component({
  selector: 'app-video-card',
  imports: [RouterLink, LongDatePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <article
      class="card video-card"
      [class.has-background-link-light]="selected()"
      [class.is-selected]="selected()"
    >
      <div
        class="video-card__preview"
        aria-hidden="true"
        [style.--thumb]="thumbnailUrl() ? 'url(' + thumbnailUrl() + ')' : null"
        [class.has-thumbnail]="!!thumbnailUrl()"
      >
        <span class="video-card__play"></span>
        @if (formattedDuration()) {
          <span class="video-card__duration">{{ formattedDuration() }}</span>
        }
      </div>

      <div class="card-content">
        <div class="video-card__header">
          <div>
            @if (badge()) {
              <span
                class="tag video-card__badge"
                [attr.data-badge-tone]="badgeTone().index"
                [style.--video-card-badge-bg]="badgeTone().background"
                [style.--video-card-badge-border]="badgeTone().border"
                [style.--video-card-badge-text]="badgeTone().text"
              >
                {{ badge() }}
              </span>
            }
            <h3 class="title is-6 video-card__title">{{ video().name || 'Untitled recording' }}</h3>
          </div>

          @if (!video().converted || !video().blobId) {
            <span class="tag is-warning is-light video-card__status">Processing&hellip;</span>
          }
        </div>

        <p class="is-size-7 has-text-grey video-card__meta">
          <span class="video-card__meta-icon" aria-hidden="true"></span>
          <span>{{ video().recordedDateTime | longDate }}</span>
        </p>

        <div class="buttons are-small video-card__actions">
          @if (video().converted && video().blobId) {
            <a
              class="button is-primary video-card__watch"
              [routerLink]="['/videos', video().blobId]"
              [queryParams]="queryParams()"
            >
              <span class="video-card__button-icon" aria-hidden="true"></span>
              Watch
            </a>
          }
          @if (shareable()) {
            <button
              type="button"
              class="button is-light video-card__share"
              (click)="share.emit(video())"
            >
              Share
            </button>
          }
        </div>
      </div>
    </article>
  `,
  styles: `
    :host {
      display: block;
      height: 100%;
    }

    .video-card {
      height: 100%;
      overflow: hidden;
      border: 1px solid color-mix(in srgb, var(--bulma-border) 80%, transparent);
      border-radius: 8px;
      box-shadow: 0 0.4rem 1rem rgba(20, 26, 44, 0.08);
      transition:
        transform 140ms ease,
        box-shadow 140ms ease,
        border-color 140ms ease;
    }

    .video-card:hover,
    .video-card:focus-within {
      transform: translateY(-2px);
      border-color: color-mix(in srgb, var(--bulma-primary) 42%, var(--bulma-border));
      box-shadow: 0 0.8rem 1.6rem rgba(20, 26, 44, 0.13);
    }

    .video-card.is-selected {
      border-color: var(--bulma-link);
      box-shadow:
        0 0 0 1px var(--bulma-link),
        0 0.8rem 1.6rem rgba(72, 95, 199, 0.18);
    }

    .video-card__preview {
      position: relative;
      display: grid;
      min-height: 7rem;
      place-items: center;
      background:
        radial-gradient(circle at 82% 18%, rgba(255, 255, 255, 0.34), transparent 24%),
        linear-gradient(135deg, #20314f 0%, #2f6f73 56%, #f0b35a 100%);
    }

    .video-card__preview.has-thumbnail {
      background: var(--thumb) center / cover no-repeat;
    }

    .video-card__preview::after {
      position: absolute;
      inset: auto 0 0;
      height: 42%;
      content: '';
      background: linear-gradient(to top, rgba(0, 0, 0, 0.22), transparent);
    }

    .video-card__play {
      z-index: 1;
      display: grid;
      width: 3rem;
      height: 3rem;
      place-items: center;
      border-radius: 999px;
      background: rgba(255, 255, 255, 0.92);
      box-shadow: 0 0.45rem 1.2rem rgba(0, 0, 0, 0.22);
    }

    .video-card__play::before,
    .video-card__button-icon::before {
      display: block;
      width: 0;
      height: 0;
      margin-left: 0.15rem;
      border-top: 0.45rem solid transparent;
      border-bottom: 0.45rem solid transparent;
      border-left: 0.7rem solid currentColor;
      color: var(--bulma-primary);
      content: '';
    }

    .video-card__duration {
      position: absolute;
      right: 0.75rem;
      bottom: 0.65rem;
      z-index: 1;
      padding: 0.2rem 0.45rem;
      border-radius: 0.35rem;
      background: rgba(0, 0, 0, 0.68);
      color: #fff;
      font-size: 0.75rem;
      font-weight: 700;
      line-height: 1;
    }

    .card-content {
      display: flex;
      min-height: 11rem;
      flex-direction: column;
      gap: 0.75rem;
      padding: 1rem;
    }

    .video-card__header {
      display: flex;
      align-items: flex-start;
      justify-content: space-between;
      gap: 0.75rem;
    }

    .video-card__badge {
      max-width: 100%;
      margin-bottom: 0.45rem;
      border: 1px solid var(--video-card-badge-border);
      background: var(--video-card-badge-bg);
      color: var(--video-card-badge-text);
      font-weight: 700;
    }

    .video-card__title {
      display: -webkit-box;
      margin-bottom: 0;
      overflow: hidden;
      -webkit-box-orient: vertical;
      -webkit-line-clamp: 2;
      line-height: 1.25;
    }

    .video-card__status {
      flex: 0 0 auto;
    }

    .video-card__meta {
      display: flex;
      align-items: center;
      flex-wrap: wrap;
      gap: 0.35rem;
      margin: 0;
    }

    .video-card__meta-icon {
      width: 0.8rem;
      height: 0.8rem;
      border: 2px solid currentColor;
      border-radius: 999px;
      opacity: 0.7;
    }

    .video-card__actions {
      margin-top: auto;
      margin-bottom: 0;
      padding-top: 0.25rem;
    }

    .video-card__watch {
      min-width: 6rem;
      font-weight: 700;
    }

    .video-card__button-icon {
      display: inline-grid;
      width: 1rem;
      place-items: center;
    }

    .video-card__button-icon::before {
      border-top-width: 0.3rem;
      border-bottom-width: 0.3rem;
      border-left-width: 0.48rem;
      color: currentColor;
    }

    .video-card__share {
      font-weight: 600;
    }
  `,
})
export class VideoCard {
  readonly video = input.required<VideoInformation>();
  /** Show the Share action (e.g. in the user's own library). */
  readonly shareable = input(false);
  /** Query params carried to the player to preserve the group/event scope. */
  readonly queryParams = input<Params>({});
  /** Highlight this card as the currently-playing recording. */
  readonly selected = input(false);
  /** Optional contextual label shown above the title (e.g. group name). */
  readonly badge = input('');
  readonly share = output<VideoInformation>();

  readonly formattedDuration = computed(() => formatDuration(this.video().duration));
  readonly badgeTone = computed(() => getBadgeTone(this.badge()));
  readonly thumbnailUrl = computed(() => this.video().thumbnailUrl ?? null);
}

interface BadgeTone {
  readonly index: number;
  readonly background: string;
  readonly border: string;
  readonly text: string;
}

const BADGE_TONES: readonly Omit<BadgeTone, 'index'>[] = [
  { background: '#e8f3ff', border: '#a9d2ff', text: '#15518a' },
  { background: '#eaf7ef', border: '#acdcbc', text: '#1f6b3b' },
  { background: '#fff1d7', border: '#f2c16b', text: '#7a4a08' },
  { background: '#f0edff', border: '#c7bbff', text: '#4a3a96' },
  { background: '#ffecef', border: '#f4adbb', text: '#8a2d42' },
  { background: '#e7f7f7', border: '#9fd7d6', text: '#176365' },
];

function getBadgeTone(text: string): BadgeTone {
  const index = hashText(text.trim().toLowerCase()) % BADGE_TONES.length;
  return { index, ...BADGE_TONES[index] };
}

function hashText(text: string): number {
  let hash = 0;
  for (let i = 0; i < text.length; i++) {
    hash = (hash * 31 + text.charCodeAt(i)) >>> 0;
  }

  return hash;
}

function formatDuration(duration: string | undefined): string {
  const trimmed = duration?.trim();
  if (!trimmed) {
    return '';
  }

  const seconds = parseDurationSeconds(trimmed);
  if (seconds === null) {
    return trimmed;
  }

  return formatSeconds(seconds);
}

function parseDurationSeconds(duration: string): number | null {
  const iso = duration.match(
    /^P(?:\d+D)?T(?:(\d+(?:\.\d+)?)H)?(?:(\d+(?:\.\d+)?)M)?(?:(\d+(?:\.\d+)?)S)?$/i,
  );
  if (iso) {
    const [, hours = '0', minutes = '0', seconds = '0'] = iso;
    return Math.round(Number(hours) * 3600 + Number(minutes) * 60 + Number(seconds));
  }

  if (/^\d+(?:\.\d+)?$/.test(duration)) {
    return Math.round(Number(duration));
  }

  const timeSpan = duration.match(/^(?:(\d+)\.)?(\d+):(\d{1,2})(?::(\d{1,2}(?:\.\d+)?))?$/);
  if (timeSpan) {
    const [, days = '0', first, second, third] = timeSpan;
    const hours = third === undefined ? '0' : first;
    const minutes = third === undefined ? first : second;
    const seconds = third === undefined ? second : third;

    return Math.round(
      Number(days) * 86400 + Number(hours) * 3600 + Number(minutes) * 60 + Number(seconds),
    );
  }

  return null;
}

function formatSeconds(totalSeconds: number): string {
  const clampedSeconds = Math.max(0, totalSeconds);
  const hours = Math.floor(clampedSeconds / 3600);
  const minutes = Math.floor((clampedSeconds % 3600) / 60);
  const seconds = clampedSeconds % 60;

  if (hours > 0) {
    return `${hours}:${padTimePart(minutes)}:${padTimePart(seconds)}`;
  }

  return `${minutes}:${padTimePart(seconds)}`;
}

function padTimePart(value: number): string {
  return value.toString().padStart(2, '0');
}
