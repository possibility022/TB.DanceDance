import { buildShareMessage } from './share-message';

describe('buildShareMessage', () => {
  const url = 'https://example.test/shared/abc';

  it('builds video messages with and without a title', () => {
    expect(buildShareMessage('video', 'Tango Basics', url)).toBe(
      `Hey! I'm sharing the recording “Tango Basics” with you. Watch it here:\n${url}`,
    );
    expect(buildShareMessage('video', '', url)).toBe(
      `Hey! I'm sharing a recording with you. Watch it here:\n${url}`,
    );
  });

  it('builds competition messages with and without a title', () => {
    expect(buildShareMessage('competition', 'Summer Cup', url)).toBe(
      `Hey! I'm sharing the competition “Summer Cup” with you. Watch it here:\n${url}`,
    );
    expect(buildShareMessage('competition', undefined, url)).toBe(
      `Hey! I'm sharing a competition with you. Watch it here:\n${url}`,
    );
  });

  it('builds group invite messages with and without a title', () => {
    expect(buildShareMessage('group', 'Beginners', url)).toBe(
      `Hey! I'd like to invite you to the “Beginners” group. Join using this link:\n${url}`,
    );
    expect(buildShareMessage('group', '   ', url)).toBe(
      `Hey! I'd like to invite you to a group. Join using this link:\n${url}`,
    );
  });

  it('builds event invite messages with and without a title', () => {
    expect(buildShareMessage('event', 'Summer Camp 2026', url)).toBe(
      `Hey! I'd like to invite you to “Summer Camp 2026”. Join using this link:\n${url}`,
    );
    expect(buildShareMessage('event', undefined, url)).toBe(
      `Hey! I'd like to invite you to an event. Join using this link:\n${url}`,
    );
  });

  it('builds transfer messages with and without a title', () => {
    expect(buildShareMessage('transfer', 'My Solo', url)).toBe(
      `Hey! I'd like to give you the recording “My Solo”. Open this link to take ownership:\n${url}`,
    );
    expect(buildShareMessage('transfer', '', url)).toBe(
      `Hey! I'd like to give you a recording. Open this link to take ownership:\n${url}`,
    );
  });

  it('trims whitespace around the title', () => {
    expect(buildShareMessage('video', '  Waltz  ', url)).toContain('“Waltz”');
  });
});
