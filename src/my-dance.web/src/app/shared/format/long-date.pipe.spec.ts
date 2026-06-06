import { LongDatePipe } from './long-date.pipe';

describe('LongDatePipe', () => {
  let pipe: LongDatePipe;

  beforeEach(() => {
    pipe = new LongDatePipe();
  });

  it('formats a Date as "dd MMMM yyyy"', () => {
    // Local-time constructor avoids timezone day-shift in the assertion.
    expect(pipe.transform(new Date(2026, 5, 4))).toBe('04 June 2026');
  });

  it('zero-pads the day', () => {
    expect(pipe.transform(new Date(2024, 0, 9))).toBe('09 January 2024');
  });

  it('formats a numeric timestamp', () => {
    const timestamp = new Date(2023, 11, 25).getTime();
    expect(pipe.transform(timestamp)).toBe('25 December 2023');
  });

  it('formats an ISO date-time string', () => {
    expect(pipe.transform('2025-03-15T10:30:00')).toBe('15 March 2025');
  });

  it.each([null, undefined, ''] as const)('returns empty string for %s', (value) => {
    expect(pipe.transform(value)).toBe('');
  });
});
