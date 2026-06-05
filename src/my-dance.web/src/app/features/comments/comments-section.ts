import { ChangeDetectionStrategy, Component, effect, inject, input, output, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormBuilder, FormControl, ReactiveFormsModule, Validators } from '@angular/forms';

import { CommentResponse } from '../../core/api/api-models';

export interface CommentDraft {
  readonly content: string;
  readonly authorName?: string;
}

export interface CommentEdit {
  readonly commentId: string;
  readonly content: string;
}

export interface CommentReport {
  readonly commentId: string;
  readonly reason: string;
}

const CONTENT_MIN = 3;
const CONTENT_MAX = 2000;
const SIGNATURE_MIN = 3;
const SIGNATURE_MAX = 20;

/**
 * Reusable comments list + composer, driven by server-provided capability flags
 * on each comment (isOwn / canModerate / isHidden). The host owns loading and
 * persistence; this component emits intents and renders state.
 */
@Component({
  selector: 'app-comments-section',
  imports: [ReactiveFormsModule, DatePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './comments-section.html',
})
export class CommentsSection {
  private readonly fb = inject(FormBuilder);

  readonly comments = input.required<readonly CommentResponse[]>();
  /** Show the composer (e.g. on a shared link that allows comments). */
  readonly canCompose = input(false);
  /** Require a signature name (anonymous, not-logged-in commenter). */
  readonly requireSignature = input(false);
  /** A create is in flight — disables the composer. */
  readonly submitting = input(false);

  readonly create = output<CommentDraft>();
  readonly saveEdit = output<CommentEdit>();
  readonly remove = output<string>();
  readonly hide = output<string>();
  readonly unhide = output<string>();
  readonly report = output<CommentReport>();

  readonly composer = this.fb.nonNullable.group({
    authorName: [''],
    content: ['', [Validators.required, Validators.minLength(CONTENT_MIN), Validators.maxLength(CONTENT_MAX)]],
  });

  readonly editingId = signal<string | null>(null);
  readonly reportingId = signal<string | null>(null);
  readonly editControl = new FormControl('', {
    nonNullable: true,
    validators: [Validators.required, Validators.minLength(CONTENT_MIN), Validators.maxLength(CONTENT_MAX)],
  });
  readonly reportControl = new FormControl('', { nonNullable: true, validators: [Validators.maxLength(200)] });

  readonly contentMax = CONTENT_MAX;
  readonly signatureMax = SIGNATURE_MAX;

  constructor() {
    effect(() => {
      const control = this.composer.controls.authorName;
      control.setValidators(
        this.requireSignature()
          ? [Validators.required, Validators.minLength(SIGNATURE_MIN), Validators.maxLength(SIGNATURE_MAX)]
          : [],
      );
      control.updateValueAndValidity({ emitEvent: false });
    });
  }

  authorLabel(comment: CommentResponse): string {
    const name = comment.authorName?.trim() || 'Someone';
    return comment.postedAsAnonymous ? `${name} (guest)` : name;
  }

  submitNew(): void {
    if (this.composer.invalid || this.submitting()) {
      return;
    }
    const { content, authorName } = this.composer.getRawValue();
    this.create.emit({
      content: content.trim(),
      authorName: this.requireSignature() ? authorName.trim() : undefined,
    });
    this.composer.reset({ authorName: this.requireSignature() ? authorName : '', content: '' });
  }

  startEditing(comment: CommentResponse): void {
    this.reportingId.set(null);
    this.editingId.set(comment.id ?? null);
    this.editControl.setValue(comment.content ?? '');
  }

  cancelEditing(): void {
    this.editingId.set(null);
  }

  saveEditing(comment: CommentResponse): void {
    if (this.editControl.invalid || !comment.id) {
      return;
    }
    this.saveEdit.emit({ commentId: comment.id, content: this.editControl.value.trim() });
    this.editingId.set(null);
  }

  startReport(comment: CommentResponse): void {
    this.editingId.set(null);
    this.reportingId.set(comment.id ?? null);
    this.reportControl.setValue('');
  }

  cancelReport(): void {
    this.reportingId.set(null);
  }

  submitReport(comment: CommentResponse): void {
    if (this.reportControl.invalid || !comment.id) {
      return;
    }
    this.report.emit({ commentId: comment.id, reason: this.reportControl.value.trim() });
    this.reportingId.set(null);
  }
}
