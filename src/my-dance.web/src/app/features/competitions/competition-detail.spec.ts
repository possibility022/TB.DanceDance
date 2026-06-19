import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';

import { CompetitionDetail } from './competition-detail';
import { CompetitionsService } from '../../core/api/competitions.service';
import { VideosService } from '../../core/api/videos.service';

interface Overrides {
  getCompetition?: ReturnType<typeof vi.fn>;
  renameCompetition?: ReturnType<typeof vi.fn>;
  deleteCompetition?: ReturnType<typeof vi.fn>;
  addVideo?: ReturnType<typeof vi.fn>;
  removeVideo?: ReturnType<typeof vi.fn>;
  getMyVideos?: ReturnType<typeof vi.fn>;
}

function createFixture(overrides: Overrides = {}) {
  const competitions = {
    getCompetition:
      overrides.getCompetition ??
      vi.fn(() =>
        of({ id: 'c1', name: 'Nationals', videos: [{ videoId: 'v1', name: 'Round 1' }] }),
      ),
    renameCompetition: overrides.renameCompetition ?? vi.fn(() => of(void 0)),
    deleteCompetition: overrides.deleteCompetition ?? vi.fn(() => of(void 0)),
    addVideo: overrides.addVideo ?? vi.fn(() => of(void 0)),
    removeVideo: overrides.removeVideo ?? vi.fn(() => of(void 0)),
  };
  const videos = {
    getMyVideos:
      overrides.getMyVideos ??
      vi.fn(() =>
        of({
          items: [
            { videoId: 'v1', name: 'Round 1' },
            { videoId: 'v2', name: 'Round 2' },
          ],
        }),
      ),
  };
  TestBed.configureTestingModule({
    imports: [CompetitionDetail],
    providers: [
      provideRouter([]),
      { provide: CompetitionsService, useValue: competitions },
      { provide: VideosService, useValue: videos },
    ],
  });

  const router = TestBed.inject(Router);
  const navigate = vi.spyOn(router, 'navigate').mockResolvedValue(true);

  const fixture = TestBed.createComponent(CompetitionDetail);
  fixture.componentRef.setInput('competitionId', 'c1');
  fixture.detectChanges();
  return { fixture, competitions, videos, router: { navigate }, component: fixture.componentInstance };
}

describe('CompetitionDetail', () => {
  it('loads the competition and its videos', () => {
    const { component } = createFixture();
    expect(component.loading()).toBe(false);
    expect(component.competition()?.name).toBe('Nationals');
    expect(component.groupedVideos()).toHaveLength(1);
  });

  it('lists only ungrouped videos as addable', () => {
    const { component } = createFixture();
    // v1 is already grouped, so only v2 is addable.
    expect(component.addableVideos().map((v) => v.videoId)).toEqual(['v2']);
  });

  it('enters the failed state on error', () => {
    const { component } = createFixture({ getCompetition: vi.fn(() => throwError(() => new Error('x'))) });
    expect(component.failed()).toBe(true);
  });

  it('add() calls the service and moves the video into the competition', () => {
    const { component, competitions } = createFixture();
    component.add({ videoId: 'v2', name: 'Round 2' });
    expect(competitions.addVideo).toHaveBeenCalledWith('c1', 'v2');
    expect(component.groupedVideos().map((v) => v.videoId)).toContain('v2');
  });

  it('add() surfaces an error when the video is already in another competition', () => {
    const { component } = createFixture({ addVideo: vi.fn(() => throwError(() => new Error('400'))) });
    component.add({ videoId: 'v2', name: 'Round 2' });
    expect(component.addError()).toContain('Round 2');
  });

  it('remove() calls the service and drops the video', () => {
    const { component, competitions } = createFixture();
    component.remove({ videoId: 'v1', name: 'Round 1' });
    expect(competitions.removeVideo).toHaveBeenCalledWith('c1', 'v1');
    expect(component.groupedVideos()).toHaveLength(0);
  });

  it('rename() patches the competition with the prompted name', () => {
    vi.spyOn(window, 'prompt').mockReturnValue('Worlds');
    const { component, competitions } = createFixture();
    component.rename();
    expect(competitions.renameCompetition).toHaveBeenCalledWith('c1', { newName: 'Worlds' });
    expect(component.competition()?.name).toBe('Worlds');
  });

  it('rename() does nothing when the prompt is cancelled', () => {
    vi.spyOn(window, 'prompt').mockReturnValue(null);
    const { component, competitions } = createFixture();
    component.rename();
    expect(competitions.renameCompetition).not.toHaveBeenCalled();
  });

  it('deleteCompetition() confirms, deletes, and navigates away', () => {
    vi.spyOn(window, 'confirm').mockReturnValue(true);
    const { component, competitions, router } = createFixture();
    component.deleteCompetition();
    expect(competitions.deleteCompetition).toHaveBeenCalledWith('c1');
    expect(router.navigate).toHaveBeenCalledWith(['/competitions']);
  });

  it('deleteCompetition() does nothing when not confirmed', () => {
    vi.spyOn(window, 'confirm').mockReturnValue(false);
    const { component, competitions } = createFixture();
    component.deleteCompetition();
    expect(competitions.deleteCompetition).not.toHaveBeenCalled();
  });

  it('openShare()/closeShare() toggle the share dialog', () => {
    const { component } = createFixture();
    component.openShare();
    expect(component.shareOpen()).toBe(true);
    component.closeShare();
    expect(component.shareOpen()).toBe(false);
  });
});
