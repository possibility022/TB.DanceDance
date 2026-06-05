import { SharingWithType } from '../../core/api/api-models';
import {
  COMMENT_VISIBILITY_LABELS,
  CommentVisibility,
  SHARING_WITH_TYPE_LABELS,
  commentVisibilityLabel,
} from './enums';

describe('CommentVisibility enum', () => {
  it('pins the numeric contract shared with the backend', () => {
    expect(CommentVisibility.LoggedInOnly).toBe(0);
    expect(CommentVisibility.OwnerOnly).toBe(1);
    expect(CommentVisibility.Everyone).toBe(2);
  });

  it('labels every visibility value', () => {
    expect(COMMENT_VISIBILITY_LABELS[CommentVisibility.LoggedInOnly]).toBe(
      'Logged-in users only',
    );
    expect(COMMENT_VISIBILITY_LABELS[CommentVisibility.OwnerOnly]).toBe('Only me');
    expect(COMMENT_VISIBILITY_LABELS[CommentVisibility.Everyone]).toBe('Everyone with access');
  });
});

describe('commentVisibilityLabel', () => {
  it.each([
    [0, 'Logged-in users only'],
    [1, 'Only me'],
    [2, 'Everyone with access'],
  ])('maps %i to its label', (value, label) => {
    expect(commentVisibilityLabel(value)).toBe(label);
  });

  it('returns "Unknown" for undefined', () => {
    expect(commentVisibilityLabel(undefined)).toBe('Unknown');
  });

  it('returns "Unknown" for an out-of-range value', () => {
    expect(commentVisibilityLabel(99)).toBe('Unknown');
  });
});

describe('SHARING_WITH_TYPE_LABELS', () => {
  it('labels every sharing-with type', () => {
    expect(SHARING_WITH_TYPE_LABELS[SharingWithType.NotSpecified]).toBe('Unspecified');
    expect(SHARING_WITH_TYPE_LABELS[SharingWithType.Group]).toBe('Group');
    expect(SHARING_WITH_TYPE_LABELS[SharingWithType.Event]).toBe('Event');
    expect(SHARING_WITH_TYPE_LABELS[SharingWithType.Private]).toBe('Private library');
  });
});
