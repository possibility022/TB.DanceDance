import { ChangeDetectionStrategy, Component, computed, input, signal } from '@angular/core';

type Copied = 'link' | 'message' | null;

/**
 * A readonly view of a shareable URL with copy buttons:
 * - **Copy message** copies a ready-to-send sentence (only shown when `message` is set).
 * - **Copy link** copies just the URL.
 * The pressed button briefly shows "Copied!" before resetting.
 */
@Component({
  selector: 'app-copy-link',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="field has-addons">
      <div class="control is-expanded">
        <input
          class="input is-small"
          type="text"
          readonly
          [value]="url()"
          aria-label="Share link"
        />
      </div>
      @if (hasMessage()) {
        <div class="control">
          <button
            type="button"
            class="button is-small"
            (click)="copyMessage()"
            aria-label="Copy a ready-to-send message with the link"
          >
            {{ copied() === 'message' ? 'Copied!' : 'Copy message' }}
          </button>
        </div>
      }
      <div class="control">
        <button
          type="button"
          class="button is-small"
          (click)="copyLink()"
          aria-label="Copy just the link"
        >
          {{ copied() === 'link' ? 'Copied!' : 'Copy link' }}
        </button>
      </div>
    </div>
  `,
  styles: `
    :host {
      display: block;
    }
    .field {
      margin-bottom: 0;
    }
  `,
})
export class CopyLink {
  readonly url = input.required<string>();
  /** When empty, the "Copy message" button is hidden. */
  readonly message = input('');

  readonly copied = signal<Copied>(null);
  readonly hasMessage = computed(() => this.message().trim().length > 0);

  copyLink(): void {
    this.copy(this.url(), 'link');
  }

  copyMessage(): void {
    this.copy(this.message(), 'message');
  }

  private copy(text: string, which: Exclude<Copied, null>): void {
    if (!text) {
      return;
    }
    void navigator.clipboard?.writeText(text).then(() => {
      this.copied.set(which);
      setTimeout(() => this.copied.set(null), 2000);
    });
  }
}
