import { FileSizePipe } from './file-size.pipe';

describe('FileSizePipe', () => {
  const pipe = new FileSizePipe();

  it('formats zero / nullish / negative as "0 B"', () => {
    expect(pipe.transform(0)).toBe('0 B');
    expect(pipe.transform(null)).toBe('0 B');
    expect(pipe.transform(undefined)).toBe('0 B');
    expect(pipe.transform(-5)).toBe('0 B');
  });

  it('formats bytes as whole numbers', () => {
    expect(pipe.transform(512)).toBe('512 B');
  });

  it('scales to KB / MB / GB with one decimal', () => {
    expect(pipe.transform(1536)).toBe('1.5 KB');
    expect(pipe.transform(5 * 1024 * 1024)).toBe('5.0 MB');
    expect(pipe.transform(2 * 1024 * 1024 * 1024)).toBe('2.0 GB');
  });
});
