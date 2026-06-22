import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';

import { CompetitionCreateDialog } from './competition-create-dialog';
import { CompetitionsService } from '../../core/api/competitions.service';

interface Overrides {
  createCompetition?: ReturnType<typeof vi.fn>;
}

function createFixture(overrides: Overrides = {}) {
  const competitions = {
    createCompetition: overrides.createCompetition ?? vi.fn(() => of({ id: 'c2' })),
  };

  TestBed.configureTestingModule({
    imports: [CompetitionCreateDialog],
    providers: [{ provide: CompetitionsService, useValue: competitions }],
  });

  const fixture = TestBed.createComponent(CompetitionCreateDialog);
  fixture.detectChanges();
  return { fixture, competitions, component: fixture.componentInstance };
}

describe('CompetitionCreateDialog', () => {
  it('does not submit an invalid (empty name) form', () => {
    const { component, competitions } = createFixture();
    component.create();
    expect(competitions.createCompetition).not.toHaveBeenCalled();
  });

  it('create() posts the form and emits created', () => {
    const { component, competitions } = createFixture();
    const created = vi.fn();
    component.created.subscribe(created);
    component.form.setValue({ name: 'Worlds', location: 'Paris', commentVisibility: 1 });
    component.create();
    expect(competitions.createCompetition).toHaveBeenCalledWith({
      name: 'Worlds',
      location: 'Paris',
      commentVisibility: 1,
    });
    expect(created).toHaveBeenCalledTimes(1);
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

  it('does not emit created when the request fails', () => {
    const { component, competitions } = createFixture({
      createCompetition: vi.fn(() => throwError(() => new Error('x'))),
    });
    const created = vi.fn();
    component.created.subscribe(created);
    component.form.setValue({ name: 'Worlds', location: '', commentVisibility: 1 });
    component.create();
    expect(created).not.toHaveBeenCalled();
    expect(component.creating()).toBe(false);
  });

  it('close() resets the form and emits closed', () => {
    const { component } = createFixture();
    const closed = vi.fn();
    component.closed.subscribe(closed);
    component.form.setValue({ name: 'Worlds', location: 'Paris', commentVisibility: 1 });
    component.close();
    expect(closed).toHaveBeenCalledTimes(1);
    expect(component.form.getRawValue().name).toBe('');
  });
});
