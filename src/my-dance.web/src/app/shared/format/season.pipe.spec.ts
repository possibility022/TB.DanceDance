import { SeasonPipe } from './season.pipe';

describe('SeasonPipe', () => {
  let pipe: SeasonPipe;

  beforeEach(() => {
    pipe = new SeasonPipe();
  });

  it('renders a two-year span when the years differ', () => {
    expect(pipe.transform(new Date(2023, 8, 1), new Date(2024, 4, 1))).toBe('2023 – 2024');
  });

  it('renders a single year when both ends fall in the same year', () => {
    expect(pipe.transform(new Date(2024, 0, 1), new Date(2024, 11, 31))).toBe('2024');
  });

  it('falls back to the start year when the end is missing', () => {
    expect(pipe.transform(new Date(2022, 5, 1), null)).toBe('2022');
  });

  it('falls back to the end year when the start is missing', () => {
    expect(pipe.transform(null, new Date(2021, 5, 1))).toBe('2021');
  });

  it('returns empty string when both ends are missing', () => {
    expect(pipe.transform(null, undefined)).toBe('');
  });

  it('returns empty string for an empty-string start with no end', () => {
    expect(pipe.transform('', '')).toBe('');
  });

  it('accepts ISO strings and numeric timestamps', () => {
    expect(pipe.transform('2020-01-01T00:00:00', new Date(2021, 0, 1).getTime())).toBe(
      '2020 – 2021',
    );
  });

  it('treats an invalid date as a missing year', () => {
    expect(pipe.transform('not-a-date', new Date(2030, 0, 1))).toBe('2030');
  });

  it('returns empty string when both dates are invalid', () => {
    expect(pipe.transform('nope', 'also-nope')).toBe('');
  });
});
