import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { of, throwError } from 'rxjs';

import { Events } from './events';
import { AccessService } from '../../core/api/access.service';
import { EventsService } from '../../core/api/events.service';
import { EventModel } from '../../core/api/api-models';
import { UploadService } from '../../core/api/upload.service';
import { BlobUploadService } from '../../core/api/blob-upload.service';

const GALA: EventModel = { id: 'e1', name: 'Gala', date: new Date(2026, 0, 1) };
const WORKSHOP: EventModel = { id: 'e2', name: 'Workshop', date: new Date(2099, 5, 1) };

function createEventsFixture(overrides: {
  getMyAccess?: ReturnType<typeof vi.fn>;
  getEventVideos?: ReturnType<typeof vi.fn>;
  createEvent?: ReturnType<typeof vi.fn>;
}) {
  const access = {
    getMyAccess: overrides.getMyAccess ?? vi.fn(() => of({ assigned: { events: [GALA] } })),
  };
  const events = {
    getEventVideos: overrides.getEventVideos ?? vi.fn(() => of({ items: [], totalCount: 0 })),
    createEvent: overrides.createEvent ?? vi.fn(() => of({ id: 'new' })),
  };
  const uploads = { produceUploadUrl: vi.fn(() => of({ sas: '', videoId: 'v' })) };
  const blob = { upload: vi.fn(() => of(100)) };

  TestBed.configureTestingModule({
    imports: [Events],
    providers: [
      provideRouter([]),
      { provide: AccessService, useValue: access },
      { provide: EventsService, useValue: events },
      { provide: UploadService, useValue: uploads },
      { provide: BlobUploadService, useValue: blob },
    ],
  });

  const router = TestBed.inject(Router);
  const navigate = vi.spyOn(router, 'navigate').mockResolvedValue(true);

  const fixture = TestBed.createComponent(Events);
  fixture.detectChanges();
  return { fixture, access, events, navigate, component: fixture.componentInstance };
}

describe('Events', () => {
  it('loads the assigned events', () => {
    const { component } = createEventsFixture({});
    expect(component.loading()).toBe(false);
    expect(component.items()).toHaveLength(1);
  });

  it('groups events by year and season', () => {
    const springEvent: EventModel = {
      id: 'e3',
      name: 'Spring Intensive',
      date: new Date(2026, 3, 1),
    };
    const autumnEvent: EventModel = { id: 'e4', name: 'Autumn Gala', date: new Date(2025, 9, 1) };
    const { component } = createEventsFixture({
      getMyAccess: vi.fn(() =>
        of({ assigned: { events: [GALA, springEvent, autumnEvent, WORKSHOP] } }),
      ),
    });

    expect(
      component.eventSeasonGroups().map((group) => ({
        label: group.label,
        eventIds: group.events.map((event) => event.id),
      })),
    ).toEqual([
      { label: '2099 Summer', eventIds: ['e2'] },
      { label: '2026 Spring', eventIds: ['e3'] },
      { label: '2026 Winter', eventIds: ['e1'] },
      { label: '2025 Autumn', eventIds: ['e4'] },
    ]);
  });

  it('enters the failed state when loading errors', () => {
    const { component } = createEventsFixture({
      getMyAccess: vi.fn(() => throwError(() => new Error('x'))),
    });
    expect(component.failed()).toBe(true);
  });

  it('select() loads videos for the chosen event', () => {
    const getEventVideos = vi.fn(() => of({ items: [{ name: 'v1' }], totalCount: 1 }));
    const { component, events } = createEventsFixture({ getEventVideos });

    component.select(GALA);

    expect(events.getEventVideos).toHaveBeenCalledWith('e1', 1, 20);
    expect(component.selected()?.id).toBe('e1');
    expect(component.videos()).toHaveLength(1);
    expect(component.videosLoading()).toBe(false);
  });

  it('select() pushes the event id into the URL so browser Back returns to the list', () => {
    const { component, navigate } = createEventsFixture({});
    component.select(GALA);
    expect(navigate).toHaveBeenCalledWith(['/events'], { queryParams: { eventId: 'e1' } });
  });

  it('clearSelection() removes the event id from the URL', () => {
    const { component, navigate } = createEventsFixture({});
    component.select(GALA);
    navigate.mockClear();

    component.clearSelection();

    expect(navigate).toHaveBeenCalledWith(['/events'], { queryParams: {} });
  });

  it('reflects the eventId query param (browser Back/Forward and deep links)', () => {
    const getEventVideos = vi.fn(() => of({ items: [{ name: 'v1' }], totalCount: 1 }));
    const { fixture, component, navigate } = createEventsFixture({ getEventVideos });

    // Forward to the detail view via the URL — no extra history entry pushed.
    fixture.componentRef.setInput('eventId', 'e1');
    fixture.detectChanges();
    expect(component.selected()?.id).toBe('e1');
    expect(component.videos()).toHaveLength(1);
    expect(navigate).not.toHaveBeenCalled();

    // Browser Back clears the param and returns to the list.
    fixture.componentRef.setInput('eventId', '');
    fixture.detectChanges();
    expect(component.selected()).toBeNull();
    expect(navigate).not.toHaveBeenCalled();
  });

  it('select() exposes videosCanLoadMore when there are more items than the first page returned', () => {
    const getEventVideos = vi.fn(() => of({ items: [{ name: 'v1' }], totalCount: 3 }));
    const { component } = createEventsFixture({ getEventVideos });

    component.select(GALA);

    expect(component.videosCanLoadMore()).toBe(true);
  });

  it('select() hides load more once every item has been loaded', () => {
    const getEventVideos = vi.fn(() => of({ items: [{ name: 'v1' }, { name: 'v2' }], totalCount: 2 }));
    const { component } = createEventsFixture({ getEventVideos });

    component.select(GALA);

    expect(component.videosCanLoadMore()).toBe(false);
  });

  it('loadMoreVideos appends the next page and tracks whether more remain', () => {
    const calls: Array<[string, number, number]> = [];
    const getEventVideos = vi.fn((eventId: string, page: number, pageSize: number) => {
      calls.push([eventId, page, pageSize]);
      return page === 1
        ? of({ items: [{ name: 'v1' }], totalCount: 3, pageNumber: 1, pageSize: 1 })
        : of({ items: [{ name: 'v2' }], totalCount: 3, pageNumber: 2, pageSize: 1 });
    });
    const { component } = createEventsFixture({ getEventVideos });

    component.select(GALA);
    expect(component.videosCanLoadMore()).toBe(true);

    component.loadMoreVideos();

    expect(component.videos().map((v) => v.name)).toEqual(['v1', 'v2']);
    expect(component.videosCanLoadMore()).toBe(true);
    expect(component.videosLoadingMore()).toBe(false);
    expect(calls).toEqual([['e1', 1, 20], ['e1', 2, 20]]);
  });

  it('loadMoreVideos is a no-op while already loading, when nothing more to load, or with no selection', () => {
    let callCount = 0;
    const getEventVideos = vi.fn(() => {
      callCount++;
      return of({ items: [{ name: 'v1' }, { name: 'v2' }], totalCount: 2 });
    });
    const { component } = createEventsFixture({ getEventVideos });

    component.loadMoreVideos();
    expect(callCount).toBe(0);

    component.select(GALA);
    expect(callCount).toBe(1);
    expect(component.videosCanLoadMore()).toBe(false);

    component.loadMoreVideos();
    expect(callCount).toBe(1);
  });

  it('select() on an event without an id selects but loads nothing', () => {
    const { component, events } = createEventsFixture({});
    component.select({ name: 'No id', date: new Date(2026, 0, 1) });
    expect(component.selected()?.name).toBe('No id');
    expect(events.getEventVideos).not.toHaveBeenCalled();
  });

  it('select() flags a video load failure', () => {
    const getEventVideos = vi.fn(() => throwError(() => new Error('x')));
    const { component } = createEventsFixture({ getEventVideos });
    component.select(GALA);
    expect(component.videosFailed()).toBe(true);
    expect(component.videosLoading()).toBe(false);
  });

  it('clearSelection() returns to the event list state', () => {
    const { component } = createEventsFixture({
      getEventVideos: vi.fn(() => of({ items: [{ name: 'v1' }], totalCount: 1 })),
    });

    component.select(GALA);
    component.clearSelection();

    expect(component.selected()).toBeNull();
    expect(component.videos()).toEqual([]);
    expect(component.videosLoading()).toBe(false);
    expect(component.videosFailed()).toBe(false);
    expect(component.videosCanLoadMore()).toBe(false);
    expect(component.videosLoadingMore()).toBe(false);
  });

  it('createEvent() does nothing while the form is invalid', () => {
    const { component, events } = createEventsFixture({});
    component.createEvent();
    expect(events.createEvent).not.toHaveBeenCalled();
  });

  it('createEvent() submits a trimmed name and parsed date, then reloads', () => {
    const createEvent = vi.fn(() => of({ id: 'new' }));
    const { component, access, events } = createEventsFixture({ createEvent });

    component.openCreateModal();
    component.form.setValue({ name: '  Winter Ball  ', date: '2026-02-14' });
    component.createEvent();

    expect(events.createEvent).toHaveBeenCalledWith({
      event: { name: 'Winter Ball', date: new Date('2026-02-14') },
    });
    expect(component.creating()).toBe(false);
    expect(component.createModalOpen()).toBe(false);
    // Events.load() ran once on init and once after creating;
    // UploadDialog.loadTargets() also calls getMyAccess once on init.
    expect(access.getMyAccess).toHaveBeenCalledTimes(3);
  });

  it('createEvent() clears the creating flag when the request fails', () => {
    const createEvent = vi.fn(() => throwError(() => new Error('x')));
    const { component } = createEventsFixture({ createEvent });

    component.openCreateModal();
    component.form.setValue({ name: 'Winter Ball', date: '2026-02-14' });
    component.createEvent();

    expect(component.creating()).toBe(false);
    expect(component.createModalOpen()).toBe(true);
    expect(component.createFailed()).toBe(true);
  });

  it('opens and closes the create modal', () => {
    const { component } = createEventsFixture({});

    component.openCreateModal();
    expect(component.createModalOpen()).toBe(true);

    component.form.setValue({ name: 'Draft', date: '2026-02-14' });
    component.closeCreateModal();

    expect(component.createModalOpen()).toBe(false);
    expect(component.form.getRawValue()).toEqual({ name: '', date: '' });
  });

  it('openUploadDialog() and closeUploadDialog() toggle the upload modal', () => {
    const { component } = createEventsFixture({});

    expect(component.uploadModalOpen()).toBe(false);
    component.openUploadDialog();
    expect(component.uploadModalOpen()).toBe(true);
    component.closeUploadDialog();
    expect(component.uploadModalOpen()).toBe(false);
  });

  it('uploadTargetKey returns undefined with no selection and the event key when one is selected', () => {
    const { component } = createEventsFixture({});

    expect(component.uploadTargetKey()).toBeUndefined();

    component.select(GALA);
    expect(component.uploadTargetKey()).toBe('e:e1');
  });

  it('clearSelection() also closes the upload dialog', () => {
    const { component } = createEventsFixture({});
    component.select(GALA);
    component.openUploadDialog();
    expect(component.uploadModalOpen()).toBe(true);

    component.clearSelection();

    expect(component.uploadModalOpen()).toBe(false);
    expect(component.selected()).toBeNull();
  });
});
