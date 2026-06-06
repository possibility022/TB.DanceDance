import { ComponentFixture, TestBed } from '@angular/core/testing';
import { signal } from '@angular/core';

import { CookieConsent } from './cookie-consent';
import { ConfigService } from '../../core/config/config.service';

const CONSENT_COOKIE = 'tbdancedanceappcookiesaccepted';

function clearConsent(): void {
  document.cookie = `${CONSENT_COOKIE}=; path=/; max-age=0`;
}

async function createComponent(): Promise<ComponentFixture<CookieConsent>> {
  await TestBed.configureTestingModule({
    imports: [CookieConsent],
    providers: [
      {
        provide: ConfigService,
        useValue: { config: signal({ apiBaseUrl: '', authUrl: 'https://auth.test', redirectUri: '' }) },
      },
    ],
  }).compileComponents();

  const fixture = TestBed.createComponent(CookieConsent);
  fixture.detectChanges();
  return fixture;
}

describe('CookieConsent', () => {
  beforeEach(() => clearConsent());
  afterEach(() => clearConsent());

  it('is visible when no consent cookie is present', async () => {
    const fixture = await createComponent();
    expect(fixture.componentInstance.visible()).toBe(true);
    expect((fixture.nativeElement as HTMLElement).querySelector('.cookie-consent')).not.toBeNull();
  });

  it('is hidden when the consent cookie is already set', async () => {
    document.cookie = `${CONSENT_COOKIE}=true; path=/`;
    const fixture = await createComponent();
    expect(fixture.componentInstance.visible()).toBe(false);
    expect((fixture.nativeElement as HTMLElement).querySelector('.cookie-consent')).toBeNull();
  });

  it('accept() records consent and dismisses the banner', async () => {
    const fixture = await createComponent();

    (fixture.nativeElement.querySelector('.cookie-consent button') as HTMLButtonElement).click();
    fixture.detectChanges();

    expect(fixture.componentInstance.visible()).toBe(false);
    expect(document.cookie).toContain(`${CONSENT_COOKIE}=true`);
    expect((fixture.nativeElement as HTMLElement).querySelector('.cookie-consent')).toBeNull();
  });

  it('links to the privacy policy derived from the auth url', async () => {
    const fixture = await createComponent();
    const link = (fixture.nativeElement as HTMLElement).querySelector('a');
    expect(link?.getAttribute('href')).toBe('https://auth.test/policy/dancedanceapp');
  });
});
