import { ComponentFixture, TestBed } from '@angular/core/testing';

import { Landing } from './landing';
import { AuthService } from '../../../core/auth/auth.service';

describe('Landing', () => {
  let login: ReturnType<typeof vi.fn>;
  let fixture: ComponentFixture<Landing>;
  let el: HTMLElement;

  beforeEach(async () => {
    login = vi.fn();

    await TestBed.configureTestingModule({
      imports: [Landing],
      providers: [{ provide: AuthService, useValue: { login } }],
    }).compileComponents();

    fixture = TestBed.createComponent(Landing);
    fixture.detectChanges();
    el = fixture.nativeElement as HTMLElement;
  });

  // --- User Story 1: understand what the application offers ---

  it('renders exactly one headline explaining the application', () => {
    const headings = el.querySelectorAll('h1');
    expect(headings).toHaveLength(1);
    expect(headings[0].textContent).toContain('Dance Dance');
  });

  it('describes the three capabilities in priority order', () => {
    const titles = Array.from(el.querySelectorAll('h2')).map((h) => h.textContent?.trim());
    expect(titles).toEqual([
      'Competitions & events',
      'Your personal library',
      'Share for feedback',
    ]);
  });

  it('marks every capability icon as decorative', () => {
    const icons = el.querySelectorAll('.landing-capability__icon');
    expect(icons).toHaveLength(3);
    icons.forEach((icon) => expect(icon.getAttribute('aria-hidden')).toBe('true'));
  });

  it('uses visitor-facing language with no internal terminology', () => {
    const copy = el.textContent?.toLowerCase() ?? '';
    for (const term of ['blob', 'oidc', 'conversion', 'scope', 'token', 'azure']) {
      expect(copy).not.toContain(term);
    }
  });

  // --- User Story 2: get started from the landing page ---

  it('offers a sign-in call to action at the top and after the descriptions', () => {
    const buttons = Array.from(el.querySelectorAll('button'));
    expect(buttons.length).toBeGreaterThanOrEqual(2);
    buttons.forEach((button) => expect(button.textContent?.toLowerCase()).toContain('log in'));
  });

  it('starts the login flow when a call to action is activated', () => {
    const buttons = el.querySelectorAll('button');
    (buttons[0] as HTMLButtonElement).click();
    (buttons[buttons.length - 1] as HTMLButtonElement).click();
    expect(login).toHaveBeenCalledTimes(2);
  });
});
