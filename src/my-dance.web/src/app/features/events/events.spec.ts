import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';

import { Events } from './events';
import { AccessService } from '../../core/api/access.service';
import { EventsService } from '../../core/api/events.service';
import { EventModel } from '../../core/api/api-models';

const GALA: EventModel = { id: 'e1', name: 'Gala', date: new Date(2026, 0, 1) };

function createEventsFixture(overrides: {
  getMyAccess?: ReturnType<typeof vi.fn>;
  getEventVideos?: ReturnType<typeof vi.fn>;
  createEvent?: ReturnType<typeof vi.fn>;
}) {
  const access = {
    getMyAccess: overrides.getMyAccess ?? vi.fn(() => of({ assigned: { events: [GALA] } })),
  };
  const events = {
    getEventVideos: overrides.getEventVideos ?? vi.fn(() => of({ videos: [] })),
    createEvent: overrides.createEvent ?? vi.fn(() => of({ id: 'new' })),
  };

  TestBed.configureTestingModule({
    imports: [Events],
    providers: [
      provideRouter([]),
      { provide: AccessService, useValue: access },
      { provide: EventsService, useValue: events },
    ],
  });

  const fixture = TestBed.createComponent(Events);
  fixture.detectChanges();
  return { fixture, access, events, component: fixture.componentInstance };
}

describe('Events', () => {
  it('loads the assigned events', () => {
    const { component } = createEventsFixture({});
    expect(component.loading()).toBe(false);
    expect(component.items()).toHaveLength(1);
  });

  it('enters the failed state when loading errors', () => {
    const { component } = createEventsFixture({ getMyAccess: vi.fn(() => throwError(() => new Error('x'))) });
    expect(component.failed()).toBe(true);
  });

  it('select() loads videos for the chosen event', () => {
    const getEventVideos = vi.fn(() => of({ videos: [{ name: 'v1' }] }));
    const { component, events } = createEventsFixture({ getEventVideos });

    component.select(GALA);

    expect(events.getEventVideos).toHaveBeenCalledWith('e1');
    expect(component.selected()?.id).toBe('e1');
    expect(component.videos()).toHaveLength(1);
    expect(component.videosLoading()).toBe(false);
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

  it('createEvent() does nothing while the form is invalid', () => {
    const { component, events } = createEventsFixture({});
    component.createEvent();
    expect(events.createEvent).not.toHaveBeenCalled();
  });

  it('createEvent() submits a trimmed name and parsed date, then reloads', () => {
    const createEvent = vi.fn(() => of({ id: 'new' }));
    const { component, access, events } = createEventsFixture({ createEvent });

    component.form.setValue({ name: '  Winter Ball  ', date: '2026-02-14' });
    component.createEvent();

    expect(events.createEvent).toHaveBeenCalledWith({
      event: { name: 'Winter Ball', date: new Date('2026-02-14') },
    });
    expect(component.creating()).toBe(false);
    // load() ran once on init and once after creating.
    expect(access.getMyAccess).toHaveBeenCalledTimes(2);
  });

  it('createEvent() clears the creating flag when the request fails', () => {
    const createEvent = vi.fn(() => throwError(() => new Error('x')));
    const { component } = createEventsFixture({ createEvent });

    component.form.setValue({ name: 'Winter Ball', date: '2026-02-14' });
    component.createEvent();

    expect(component.creating()).toBe(false);
  });
});
