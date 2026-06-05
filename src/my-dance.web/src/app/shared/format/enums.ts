import { SharingWithType } from '../../core/api/api-models';

/**
 * Who may see a recording's comments. The API carries this as a raw `number`
 * (see `VideoInformation.commentVisibility`); this enum names the contract.
 * Numeric values per docs/angular-rewrite/10-data-formatting-and-enums.md.
 */
export enum CommentVisibility {
  LoggedInOnly = 0,
  OwnerOnly = 1,
  Everyone = 2,
}

export const COMMENT_VISIBILITY_LABELS: Readonly<Record<CommentVisibility, string>> = {
  [CommentVisibility.LoggedInOnly]: 'Logged-in users only',
  [CommentVisibility.OwnerOnly]: 'Only me',
  [CommentVisibility.Everyone]: 'Everyone with access',
};

export const SHARING_WITH_TYPE_LABELS: Readonly<Record<SharingWithType, string>> = {
  [SharingWithType.NotSpecified]: 'Unspecified',
  [SharingWithType.Group]: 'Group',
  [SharingWithType.Event]: 'Event',
  [SharingWithType.Private]: 'Private library',
};

export function commentVisibilityLabel(value: number | undefined): string {
  return COMMENT_VISIBILITY_LABELS[value as CommentVisibility] ?? 'Unknown';
}
