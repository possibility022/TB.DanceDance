import { SeasonRangePipe } from './season-range.pipe';

describe('SeasonRangePipe', () => {
  let pipe: SeasonRangePipe;

  beforeEach(() => {
    pipe = new SeasonRangePipe();
  });

  it('formats a start and end as "MMM yyyy – MMM yyyy"', () => {
    expect(pipe.transform(new Date(2024, 8, 1), new Date(2025, 7, 31))).toBe('Sep 2024 – Aug 2025');
  });

  it('collapses to a single label when both fall in the same month', () => {
    expect(pipe.transform(new Date(2024, 8, 1), new Date(2024, 8, 30))).toBe('Sep 2024');
  });

  it('falls back to the end label when the start is missing', () => {
    expect(pipe.transform(null, new Date(2025, 7, 31))).toBe('Aug 2025');
  });

  it('falls back to the start label when the end is missing', () => {
    expect(pipe.transform(new Date(2024, 8, 1), undefined)).toBe('Sep 2024');
  });

  it('returns an empty string when both are missing', () => {
    expect(pipe.transform(null, undefined)).toBe('');
  });

  it('formats ISO date-time strings', () => {
    expect(pipe.transform('2024-09-01T00:00:00', '2025-08-31T00:00:00')).toBe('Sep 2024 – Aug 2025');
  });
});
