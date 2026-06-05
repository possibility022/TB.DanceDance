import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';

import { LogoutCallback } from './logout-callback';

describe('LogoutCallback', () => {
  it('returns the user home on creation', () => {
    const router = { navigateByUrl: vi.fn() };
    TestBed.configureTestingModule({
      imports: [LogoutCallback],
      providers: [{ provide: Router, useValue: router }],
    });

    const fixture = TestBed.createComponent(LogoutCallback);
    fixture.detectChanges();

    expect(router.navigateByUrl).toHaveBeenCalledWith('/', { replaceUrl: true });
  });
});
