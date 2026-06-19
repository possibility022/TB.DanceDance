import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';

import { Competitions } from './competitions';
import { CompetitionsService } from '../../core/api/competitions.service';

interface Overrides {
  getMyCompetitions?: ReturnType<typeof vi.fn>;
  createCompetition?: ReturnType<typeof vi.fn>;
}

function createFixture(overrides: Overrides = {}) {
  const competitions = {
    getMyCompetitions:
      overrides.getMyCompetitions ??
      vi.fn(() => of({ competitions: [{ id: 'c1', name: 'Nationals', videoCount: 2 }] })),
    createCompetition: overrides.createCompetition ?? vi.fn(() => of({ id: 'c2' })),
  };

  TestBed.configureTestingModule({
    imports: [Competitions],
    providers: [provideRouter([]), { provide: CompetitionsService, useValue: competitions }],
  });

  const fixture = TestBed.createComponent(Competitions);
  fixture.detectChanges();
  return { fixture, competitions, component: fixture.componentInstance };
}

describe('Competitions', () => {
  it('loads the user competitions', () => {
    const { component } = createFixture();
    expect(component.loading()).toBe(false);
    expect(component.items()).toHaveLength(1);
  });

  it('enters the failed state on error', () => {
    const { component } = createFixture({
      getMyCompetitions: vi.fn(() => throwError(() => new Error('x'))),
    });
    expect(component.failed()).toBe(true);
  });

  it('does not submit an invalid (empty name) form', () => {
    const { component, competitions } = createFixture();
    component.create();
    expect(competitions.createCompetition).not.toHaveBeenCalled();
  });

  it('create() posts the form and reloads', () => {
    const { component, competitions } = createFixture();
    component.form.setValue({ name: 'Worlds', location: 'Paris', commentVisibility: 1 });
    component.create();
    expect(competitions.createCompetition).toHaveBeenCalledWith({
      name: 'Worlds',
      location: 'Paris',
      commentVisibility: 1,
    });
    // Reloaded the list after creating.
    expect(competitions.getMyCompetitions).toHaveBeenCalledTimes(2);
  });

  it('create() omits an empty location', () => {
    const { component, competitions } = createFixture();
    component.form.setValue({ name: 'Worlds', location: '', commentVisibility: 1 });
    component.create();
    expect(competitions.createCompetition).toHaveBeenCalledWith({
      name: 'Worlds',
      location: undefined,
      commentVisibility: 1,
    });
  });
});
