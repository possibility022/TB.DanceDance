import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { Observable, of, throwError } from 'rxjs';

import { GroupVideos } from './group-videos';
import { GroupsService } from '../../core/api/groups.service';
import { AccessService } from '../../core/api/access.service';
import { ListGroupVideosResponse, GetUserAccessResponse } from '../../core/api/api-models';

async function setup(opts: {
  videos?: Observable<ListGroupVideosResponse>;
  access?: Observable<GetUserAccessResponse>;
}): Promise<ComponentFixture<GroupVideos>> {
  await TestBed.configureTestingModule({
    imports: [GroupVideos],
    providers: [
      provideRouter([]),
      { provide: GroupsService, useValue: { getGroupVideos: () => opts.videos ?? of({ videos: [] }) } },
      { provide: AccessService, useValue: { getMyAccess: () => opts.access ?? of({}) } },
    ],
  }).compileComponents();

  const fixture = TestBed.createComponent(GroupVideos);
  fixture.detectChanges();
  return fixture;
}

describe('GroupVideos', () => {
  it('buckets videos by group in first-seen order and attaches seasons', async () => {
    const seasonStart = new Date(2023, 8, 1);
    const seasonEnd = new Date(2024, 4, 1);
    const c = (
      await setup({
        videos: of({
          videos: [
            { videoId: 'v1', groupId: 'g1', groupName: 'Salsa' },
            { videoId: 'v2', groupId: 'g2', groupName: 'Bachata' },
            { videoId: 'v3', groupId: 'g1', groupName: 'Salsa' },
          ],
        }),
        access: of({
          assigned: { groups: [{ id: 'g1', seasonStart, seasonEnd }] },
          available: { groups: [{ id: 'g2', seasonStart: new Date(2022, 8, 1) }] },
        }),
      })
    ).componentInstance;

    const sections = c.sections();
    expect(sections.map((s) => s.groupId)).toEqual(['g1', 'g2']);
    expect(sections[0].videos).toHaveLength(2);
    expect(sections[0].seasonStart).toEqual(seasonStart);
    expect(sections[0].seasonEnd).toEqual(seasonEnd);
    expect(sections[1].videos).toHaveLength(1);
  });

  it('falls back to "unknown" group id and "Group" name for ungrouped videos', async () => {
    const c = (
      await setup({ videos: of({ videos: [{ videoId: 'v1' }] }) })
    ).componentInstance;

    const sections = c.sections();
    expect(sections).toHaveLength(1);
    expect(sections[0].groupId).toBe('unknown');
    expect(sections[0].groupName).toBe('Group');
    expect(sections[0].seasonStart).toBeUndefined();
  });

  it('enters the failed state on error', async () => {
    const c = (await setup({ videos: throwError(() => new Error('boom')) })).componentInstance;
    expect(c.failed()).toBe(true);
    expect(c.loading()).toBe(false);
  });
});
