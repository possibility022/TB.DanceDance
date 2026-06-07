import { ComponentFixture, TestBed } from '@angular/core/testing';

import { CommentDraft, CommentEdit, CommentReport, CommentsSection } from './comments-section';
import { CommentResponse } from '../../core/api/api-models';

async function setup(
  inputs: Record<string, unknown> = {},
): Promise<ComponentFixture<CommentsSection>> {
  await TestBed.configureTestingModule({ imports: [CommentsSection] }).compileComponents();
  const fixture = TestBed.createComponent(CommentsSection);
  fixture.componentRef.setInput('comments', inputs['comments'] ?? []);
  for (const [key, value] of Object.entries(inputs)) {
    if (key !== 'comments') {
      fixture.componentRef.setInput(key, value);
    }
  }
  fixture.detectChanges();
  return fixture;
}

describe('CommentsSection', () => {
  describe('authorLabel', () => {
    it('marks anonymous authors as guests', async () => {
      const c = (await setup()).componentInstance;
      expect(c.authorLabel({ authorName: 'Ada', postedAsAnonymous: true })).toBe('Ada (guest)');
    });

    it('uses the plain name for signed-in authors', async () => {
      const c = (await setup()).componentInstance;
      expect(c.authorLabel({ authorName: 'Ada', postedAsAnonymous: false })).toBe('Ada');
    });

    it('falls back to "Someone" when the name is blank', async () => {
      const c = (await setup()).componentInstance;
      expect(c.authorLabel({ authorName: '   ', postedAsAnonymous: false })).toBe('Someone');
    });
  });

  describe('new comment composer', () => {
    it('does not emit when the content is too short', async () => {
      const fixture = await setup({ canCompose: true });
      const emitted: CommentDraft[] = [];
      fixture.componentInstance.create.subscribe((d) => emitted.push(d));

      fixture.componentInstance.composer.controls.content.setValue('ab');
      fixture.componentInstance.submitNew();

      expect(emitted).toEqual([]);
    });

    it('emits a trimmed draft for valid content', async () => {
      const fixture = await setup({ canCompose: true });
      const emitted: CommentDraft[] = [];
      fixture.componentInstance.create.subscribe((d) => emitted.push(d));

      fixture.componentInstance.composer.controls.content.setValue('  great moves  ');
      fixture.componentInstance.submitNew();

      expect(emitted).toEqual([{ content: 'great moves', authorName: undefined }]);
    });

    it('requires and includes a signature when requireSignature is set', async () => {
      const fixture = await setup({ canCompose: true, requireSignature: true });
      const emitted: CommentDraft[] = [];
      fixture.componentInstance.create.subscribe((d) => emitted.push(d));

      fixture.componentInstance.composer.controls.content.setValue('nice');
      fixture.componentInstance.submitNew();
      expect(emitted).toEqual([]); // signature missing -> invalid

      fixture.componentInstance.composer.controls.authorName.setValue('Guest');
      fixture.componentInstance.submitNew();
      expect(emitted).toEqual([{ content: 'nice', authorName: 'Guest' }]);
    });

    it('resetComposer keeps the signature but clears the content when a signature is required', async () => {
      const fixture = await setup({ canCompose: true, requireSignature: true });
      fixture.componentInstance.composer.setValue({ authorName: 'Guest', content: 'hello' });

      fixture.componentInstance.resetComposer();

      expect(fixture.componentInstance.composer.value).toEqual({ authorName: 'Guest', content: '' });
    });
  });

  describe('edit flow', () => {
    const comment: CommentResponse = { id: 'c1', content: 'original' };

    it('startEditing tracks the id and seeds the edit control', async () => {
      const c = (await setup({ comments: [comment] })).componentInstance;
      c.startEditing(comment);
      expect(c.editingId()).toBe('c1');
      expect(c.editControl.value).toBe('original');
    });

    it('saveEditing emits the trimmed edit and closes the editor', async () => {
      const fixture = await setup({ comments: [comment] });
      const emitted: CommentEdit[] = [];
      fixture.componentInstance.saveEdit.subscribe((e) => emitted.push(e));

      fixture.componentInstance.startEditing(comment);
      fixture.componentInstance.editControl.setValue('  updated  ');
      fixture.componentInstance.saveEditing(comment);

      expect(emitted).toEqual([{ commentId: 'c1', content: 'updated' }]);
      expect(fixture.componentInstance.editingId()).toBeNull();
    });

    it('does not emit an edit that fails validation', async () => {
      const fixture = await setup({ comments: [comment] });
      const emitted: CommentEdit[] = [];
      fixture.componentInstance.saveEdit.subscribe((e) => emitted.push(e));

      fixture.componentInstance.startEditing(comment);
      fixture.componentInstance.editControl.setValue('x');
      fixture.componentInstance.saveEditing(comment);

      expect(emitted).toEqual([]);
    });

    it('cancelEditing closes the editor', async () => {
      const c = (await setup({ comments: [comment] })).componentInstance;
      c.startEditing(comment);
      c.cancelEditing();
      expect(c.editingId()).toBeNull();
    });
  });

  describe('report flow', () => {
    const comment: CommentResponse = { id: 'c1', content: 'x' };

    it('submitReport emits the trimmed reason and closes the form', async () => {
      const fixture = await setup({ comments: [comment] });
      const emitted: CommentReport[] = [];
      fixture.componentInstance.report.subscribe((r) => emitted.push(r));

      fixture.componentInstance.startReport(comment);
      fixture.componentInstance.reportControl.setValue('  spam  ');
      fixture.componentInstance.submitReport(comment);

      expect(emitted).toEqual([{ commentId: 'c1', reason: 'spam' }]);
      expect(fixture.componentInstance.reportingId()).toBeNull();
    });
  });

  describe('load more button', () => {
    const comment: CommentResponse = { id: 'c1', content: 'x' };

    it('is hidden when canLoadMore is false', async () => {
      const fixture = await setup({ comments: [comment], canLoadMore: false });
      const buttons = [...fixture.nativeElement.querySelectorAll('button')] as HTMLButtonElement[];
      expect(buttons.some((b) => b.textContent?.trim() === 'Load more')).toBe(false);
    });

    it('renders, spins while loading, and emits loadMore on click', async () => {
      const fixture = await setup({ comments: [comment], canLoadMore: true, loadingMore: false });
      const emitted: void[] = [];
      fixture.componentInstance.loadMore.subscribe(() => emitted.push(undefined));

      const buttons = [...fixture.nativeElement.querySelectorAll('button')] as HTMLButtonElement[];
      const loadMoreButton = buttons.find((b) => b.textContent?.trim() === 'Load more');
      expect(loadMoreButton).toBeTruthy();
      expect(loadMoreButton!.disabled).toBe(false);
      expect(loadMoreButton!.classList.contains('is-loading')).toBe(false);

      loadMoreButton!.click();
      expect(emitted.length).toBe(1);

      fixture.componentRef.setInput('loadingMore', true);
      fixture.detectChanges();
      expect(loadMoreButton!.disabled).toBe(true);
      expect(loadMoreButton!.classList.contains('is-loading')).toBe(true);
    });
  });

  describe('mutually exclusive edit / report panels', () => {
    const comment: CommentResponse = { id: 'c1', content: 'x' };

    it('opening the editor closes an open report form', async () => {
      const c = (await setup({ comments: [comment] })).componentInstance;
      c.startReport(comment);
      c.startEditing(comment);
      expect(c.reportingId()).toBeNull();
      expect(c.editingId()).toBe('c1');
    });

    it('opening the report form closes an open editor', async () => {
      const c = (await setup({ comments: [comment] })).componentInstance;
      c.startEditing(comment);
      c.startReport(comment);
      expect(c.editingId()).toBeNull();
      expect(c.reportingId()).toBe('c1');
    });
  });
});
