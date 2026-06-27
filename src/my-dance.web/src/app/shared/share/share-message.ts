/** The kinds of shareable link the app generates. */
export type ShareKind = 'video' | 'competition' | 'group' | 'event' | 'transfer';

/**
 * Builds a warm, ready-to-send message naming what's being shared and including the link,
 * so it can be pasted straight into a chat. Falls back to a title-less variant when no
 * title is available.
 */
export function buildShareMessage(kind: ShareKind, title: string | undefined, url: string): string {
  const name = title?.trim();
  switch (kind) {
    case 'video':
      return name
        ? `Hey! I'm sharing the recording “${name}” with you. Watch it here:\n${url}`
        : `Hey! I'm sharing a recording with you. Watch it here:\n${url}`;
    case 'competition':
      return name
        ? `Hey! I'm sharing the competition “${name}” with you. Watch it here:\n${url}`
        : `Hey! I'm sharing a competition with you. Watch it here:\n${url}`;
    case 'group':
      return name
        ? `Hey! I'd like to invite you to the “${name}” group. Join using this link:\n${url}`
        : `Hey! I'd like to invite you to a group. Join using this link:\n${url}`;
    case 'event':
      return name
        ? `Hey! I'd like to invite you to “${name}”. Join using this link:\n${url}`
        : `Hey! I'd like to invite you to an event. Join using this link:\n${url}`;
    case 'transfer':
      return name
        ? `Hey! I'd like to give you the recording “${name}”. Open this link to take ownership:\n${url}`
        : `Hey! I'd like to give you a recording. Open this link to take ownership:\n${url}`;
  }
}
