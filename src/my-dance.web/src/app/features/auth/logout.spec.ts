import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';

import { Logout } from './logout';
import { AuthService } from '../../core/auth/auth.service';

describe('Logout', () => {
  it('triggers logout on creation', () => {
    const logout = vi.fn(() => of(null));
    TestBed.configureTestingModule({
      imports: [Logout],
      providers: [{ provide: AuthService, useValue: { logout } }],
    });

    const fixture = TestBed.createComponent(Logout);
    fixture.detectChanges();

    expect(logout).toHaveBeenCalledTimes(1);
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('Signing you out');
  });
});
