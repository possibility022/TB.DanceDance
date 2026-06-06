import { TestBed } from '@angular/core/testing';

import { SilentRenew } from './silent-renew';

describe('SilentRenew', () => {
  it('creates without rendering meaningful content', () => {
    TestBed.configureTestingModule({ imports: [SilentRenew] });

    const fixture = TestBed.createComponent(SilentRenew);
    fixture.detectChanges();

    expect(fixture.componentInstance).toBeTruthy();
    expect((fixture.nativeElement as HTMLElement).textContent?.trim()).toBe('');
  });
});
