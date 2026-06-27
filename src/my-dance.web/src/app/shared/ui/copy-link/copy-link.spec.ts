import { ComponentFixture, TestBed } from '@angular/core/testing';

import { CopyLink } from './copy-link';

async function setup(url: string, message = ''): Promise<ComponentFixture<CopyLink>> {
  const fixture = TestBed.createComponent(CopyLink);
  fixture.componentRef.setInput('url', url);
  fixture.componentRef.setInput('message', message);
  fixture.detectChanges();
  return fixture;
}

function buttonByText(el: HTMLElement, text: string): HTMLButtonElement | undefined {
  return Array.from(el.querySelectorAll('button')).find((b) => b.textContent?.includes(text)) as
    | HTMLButtonElement
    | undefined;
}

describe('CopyLink', () => {
  let writeText: ReturnType<typeof vi.fn>;

  beforeEach(async () => {
    writeText = vi.fn().mockResolvedValue(undefined);
    vi.stubGlobal('navigator', { clipboard: { writeText } });
    vi.useFakeTimers();
    await TestBed.configureTestingModule({ imports: [CopyLink] }).compileComponents();
  });

  afterEach(() => {
    vi.useRealTimers();
    vi.unstubAllGlobals();
  });

  it('shows the url in a readonly input', async () => {
    const el = (await setup('https://x.test/shared/abc')).nativeElement as HTMLElement;
    const input = el.querySelector('input') as HTMLInputElement;
    expect(input.value).toBe('https://x.test/shared/abc');
    expect(input.readOnly).toBe(true);
  });

  it('hides the "Copy message" button when no message is provided', async () => {
    const el = (await setup('https://x.test/shared/abc')).nativeElement as HTMLElement;
    expect(buttonByText(el, 'Copy message')).toBeUndefined();
    expect(buttonByText(el, 'Copy link')).toBeDefined();
  });

  it('copies just the link and flips the label, then resets', async () => {
    const fixture = await setup('https://x.test/shared/abc', 'Hey! …\nhttps://x.test/shared/abc');
    const el = fixture.nativeElement as HTMLElement;

    buttonByText(el, 'Copy link')!.click();
    await Promise.resolve();
    fixture.detectChanges();

    expect(writeText).toHaveBeenCalledWith('https://x.test/shared/abc');
    expect(buttonByText(el, 'Copied!')).toBeDefined();

    vi.advanceTimersByTime(2000);
    fixture.detectChanges();
    expect(buttonByText(el, 'Copied!')).toBeUndefined();
  });

  it('copies the full message when "Copy message" is clicked', async () => {
    const message = 'Hey! …\nhttps://x.test/shared/abc';
    const fixture = await setup('https://x.test/shared/abc', message);
    const el = fixture.nativeElement as HTMLElement;

    buttonByText(el, 'Copy message')!.click();
    await Promise.resolve();
    fixture.detectChanges();

    expect(writeText).toHaveBeenCalledWith(message);
  });
});
