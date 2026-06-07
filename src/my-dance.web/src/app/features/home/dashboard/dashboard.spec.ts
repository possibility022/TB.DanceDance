import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { Observable, of, throwError } from 'rxjs';

import { Dashboard } from './dashboard';
import { VideosService } from '../../../core/api/videos.service';
import { GroupsService } from '../../../core/api/groups.service';
import { AccessService } from '../../../core/api/access.service';
import { PagedResponseOfVideoInformation, PagedResponseOfVideoFromGroupInformation, GetUserAccessResponse } from '../../../core/api/api-models';

function d(y: number, m: number, day: number): Date {
  return new Date(y, m, day);
}

async function setup(opts: {
  my?: Observable<PagedResponseOfVideoInformation>;
  groups?: Observable<PagedResponseOfVideoFromGroupInformation>;
  access?: Observable<GetUserAccessResponse>;
}): Promise<ComponentFixture<Dashboard>> {
  await TestBed.configureTestingModule({
    imports: [Dashboard],
    providers: [
      provideRouter([]),
      { provide: VideosService, useValue: { getMyVideos: () => opts.my ?? of({ items: [] }) } },
      { provide: GroupsService, useValue: { getGroupVideos: () => opts.groups ?? of({ items: [] }) } },
      { provide: AccessService, useValue: { getMyAccess: () => opts.access ?? of({}) } },
    ],
  }).compileComponents();

  const fixture = TestBed.createComponent(Dashboard);
  fixture.detectChanges();
  return fixture;
}

describe('Dashboard', () => {
  it('loads the three libraries and exposes their counts', async () => {
    const c = (
      await setup({
        my: of({ items: [{ name: 'a' }, { name: 'b' }] }),
        groups: of({ items: [{ name: 'g' }] }),
        access: of({ assigned: { events: [{ name: 'e1', date: d(2026, 0, 1) }] } }),
      })
    ).componentInstance;

    expect(c.loading()).toBe(false);
    expect(c.failed()).toBe(false);
    expect(c.myCount()).toBe(2);
    expect(c.groupCount()).toBe(1);
    expect(c.eventCount()).toBe(1);
  });

  it('reports the latest recorded date per library, ignoring undated videos', async () => {
    const c = (
      await setup({
        my: of({ items: [{ recordedDateTime: d(2026, 0, 10) }, { recordedDateTime: d(2026, 2, 1) }] }),
        groups: of({ items: [{ recordedDateTime: d(2026, 1, 15) }, {}] }),
      })
    ).componentInstance;

    expect(c.myLatest()).toEqual(d(2026, 2, 1));
    expect(c.groupLatest()).toEqual(d(2026, 1, 15));
  });

  it('has no latest date for an empty / fully-undated library', async () => {
    const c = (await setup({ my: of({ items: [{}, {}] }) })).componentInstance;
    expect(c.myLatest()).toBeNull();
    expect(c.groupLatest()).toBeNull();
  });

  it('merges group and personal videos into a recent feed sorted newest-first', async () => {
    const c = (
      await setup({
        my: of({ items: [{ name: 'm-old', recordedDateTime: d(2026, 0, 1) }, { name: 'no-date' }] }),
        groups: of({
          items: [
            { name: 'g-new', recordedDateTime: d(2026, 5, 1) },
            { name: 'g-mid', recordedDateTime: d(2026, 2, 1) },
          ],
        }),
      })
    ).componentInstance;

    const names = c.recent().map((v) => v.name);
    expect(names).toEqual(['g-new', 'g-mid', 'm-old']);
    expect(names).not.toContain('no-date');
  });

  it('caps the recent feed at five entries', async () => {
    const videos = Array.from({ length: 8 }, (_, i) => ({ recordedDateTime: d(2026, 0, i + 1) }));
    const c = (await setup({ my: of({ items: videos }) })).componentInstance;
    expect(c.recent()).toHaveLength(5);
  });

  it('enters the failed state when a request errors', async () => {
    const c = (await setup({ my: throwError(() => new Error('boom')) })).componentInstance;
    expect(c.failed()).toBe(true);
    expect(c.loading()).toBe(false);
  });
});
