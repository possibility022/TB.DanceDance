import { AnonymousIdService } from './anonymous-id.service';

const STORAGE_KEY = 'anonymousId';

describe('AnonymousIdService', () => {
  let service: AnonymousIdService;

  beforeEach(() => {
    localStorage.clear();
    service = new AnonymousIdService();
  });

  it('returns the id already stored under the legacy key', () => {
    localStorage.setItem(STORAGE_KEY, 'existing-id');
    expect(service.getId()).toBe('existing-id');
  });

  it('generates and persists a new id when none exists', () => {
    expect(localStorage.getItem(STORAGE_KEY)).toBeNull();

    const id = service.getId();

    expect(id).toBeTruthy();
    expect(localStorage.getItem(STORAGE_KEY)).toBe(id);
  });

  it('returns a stable id across calls (generates only once)', () => {
    const spy = vi.spyOn(crypto, 'randomUUID');

    const first = service.getId();
    const second = service.getId();

    expect(first).toBe(second);
    expect(spy).toHaveBeenCalledTimes(1);
  });

  it('generates a UUID via crypto.randomUUID', () => {
    vi.spyOn(crypto, 'randomUUID').mockReturnValue(
      '123e4567-e89b-12d3-a456-426614174000',
    );

    expect(service.getId()).toBe('123e4567-e89b-12d3-a456-426614174000');
  });
});
