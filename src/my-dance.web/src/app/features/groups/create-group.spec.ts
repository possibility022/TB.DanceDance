import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { of, throwError } from 'rxjs';

import { CreateGroup } from './create-group';
import { GroupsService } from '../../core/api/groups.service';

function createFixture(overrides: { createGroup?: ReturnType<typeof vi.fn> } = {}) {
  const groups = {
    createGroup: overrides.createGroup ?? vi.fn(() => of({ id: 'g1' })),
  };

  TestBed.configureTestingModule({
    imports: [CreateGroup],
    providers: [provideRouter([]), { provide: GroupsService, useValue: groups }],
  });

  const fixture = TestBed.createComponent(CreateGroup);
  fixture.detectChanges();
  return { fixture, groups, component: fixture.componentInstance };
}

describe('CreateGroup', () => {
  it('starts with an invalid form', () => {
    const { component } = createFixture();
    expect(component.form.invalid).toBe(true);
  });

  it('flags an inverted season range', () => {
    const { component } = createFixture();
    component.form.setValue({ name: 'Beginners', seasonStart: '2025-08-31', seasonEnd: '2024-09-01' });
    expect(component.seasonOrderInvalid()).toBe(true);
  });

  it('does not submit while the form is invalid', () => {
    const { component, groups } = createFixture();
    component.submit();
    expect(groups.createGroup).not.toHaveBeenCalled();
  });

  it('creates the group and navigates to its management screen', () => {
    const { component, groups } = createFixture();
    const router = TestBed.inject(Router);
    const navigate = vi.spyOn(router, 'navigate').mockResolvedValue(true);

    component.form.setValue({ name: 'Beginners', seasonStart: '2024-09-01', seasonEnd: '2025-08-31' });
    component.submit();

    expect(groups.createGroup).toHaveBeenCalledWith({
      name: 'Beginners',
      seasonStart: new Date('2024-09-01'),
      seasonEnd: new Date('2025-08-31'),
    });
    expect(navigate).toHaveBeenCalledWith(['/groups', 'g1', 'manage']);
    expect(component.submitting()).toBe(false);
  });

  it('enters the failed state on error', () => {
    const { component } = createFixture({ createGroup: vi.fn(() => throwError(() => new Error('x'))) });
    component.form.setValue({ name: 'Beginners', seasonStart: '2024-09-01', seasonEnd: '2025-08-31' });
    component.submit();
    expect(component.failed()).toBe(true);
    expect(component.submitting()).toBe(false);
  });
});
