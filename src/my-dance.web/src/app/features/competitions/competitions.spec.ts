import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';

import { Competitions } from './competitions';
import { CompetitionsService } from '../../core/api/competitions.service';

interface Overrides {
  getMyCompetitions?: ReturnType<typeof vi.fn>;
}

function createFixture(overrides: Overrides = {}) {
  const competitions = {
    getMyCompetitions:
      overrides.getMyCompetitions ??
      vi.fn(() => of({ competitions: [{ id: 'c1', name: 'Nationals', videoCount: 2 }] })),
    createCompetition: vi.fn(() => of({ id: 'c2' })),
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

  it('openCreateModal() and closeCreateModal() toggle the create modal', () => {
    const { component } = createFixture();
    expect(component.createModalOpen()).toBe(false);
    component.openCreateModal();
    expect(component.createModalOpen()).toBe(true);
    component.closeCreateModal();
    expect(component.createModalOpen()).toBe(false);
  });

  it('onCompetitionCreated() closes the modal and reloads the list', () => {
    const { component, competitions } = createFixture();
    component.openCreateModal();
    component.onCompetitionCreated();
    expect(component.createModalOpen()).toBe(false);
    expect(competitions.getMyCompetitions).toHaveBeenCalledTimes(2);
  });
});
