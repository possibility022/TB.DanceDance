import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  computed,
  effect,
  inject,
  input,
  signal,
} from '@angular/core';
import { DOCUMENT } from '@angular/common';
import { Router, RouterLink } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { CompetitionsService } from '../../core/api/competitions.service';
import { VideosService } from '../../core/api/videos.service';
import { CompetitionResponse, VideoInformation } from '../../core/api/api-models';
import { ShareDialog } from '../sharing/share-dialog';
import { LongDatePipe } from '../../shared/format/long-date.pipe';

const PAGE_SIZE = 100;

/** A single competition: rename, delete, share, and manage its grouped recordings. */
@Component({
  selector: 'app-competition-detail',
  imports: [ShareDialog, RouterLink, LongDatePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './competition-detail.html',
})
export class CompetitionDetail {
  /** Route param (bound via withComponentInputBinding). */
  readonly competitionId = input<string>('');

  private readonly competitions = inject(CompetitionsService);
  private readonly videos = inject(VideosService);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);
  private readonly doc = inject(DOCUMENT);

  readonly loading = signal(true);
  readonly failed = signal(false);
  readonly competition = signal<CompetitionResponse | null>(null);

  /** The owner's recordings available to add (those not already in this competition). */
  readonly myVideos = signal<readonly VideoInformation[]>([]);
  readonly addError = signal<string | null>(null);
  readonly shareOpen = signal(false);

  readonly groupedVideos = computed(() => this.competition()?.videos ?? []);
  readonly addableVideos = computed(() => {
    const grouped = new Set(this.groupedVideos().map((v) => v.videoId));
    return this.myVideos().filter((v) => !grouped.has(v.videoId));
  });

  constructor() {
    effect(() => {
      const id = this.competitionId();
      if (id) {
        this.load(id);
      }
    });
  }

  private load(id: string): void {
    this.loading.set(true);
    this.failed.set(false);
    this.competitions
      .getCompetition(id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (competition) => {
          this.competition.set(competition);
          this.loading.set(false);
        },
        error: () => {
          this.failed.set(true);
          this.loading.set(false);
        },
      });

    this.videos
      .getMyVideos(1, PAGE_SIZE)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (response) => this.myVideos.set(response.items ?? []),
        error: () => this.myVideos.set([]),
      });
  }

  rename(): void {
    const current = this.competition();
    if (!current?.id) {
      return;
    }
    const newName = this.doc.defaultView?.prompt('Rename competition', current.name ?? '')?.trim();
    if (!newName || newName === current.name) {
      return;
    }
    this.competitions
      .renameCompetition(current.id, { newName })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => this.competition.update((c) => (c ? { ...c, name: newName } : c)),
      });
  }

  remove(video: VideoInformation): void {
    const competitionId = this.competition()?.id;
    if (!competitionId || !video.videoId) {
      return;
    }
    this.competitions
      .removeVideo(competitionId, video.videoId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () =>
          this.competition.update((c) =>
            c ? { ...c, videos: (c.videos ?? []).filter((v) => v.videoId !== video.videoId) } : c,
          ),
      });
  }

  add(video: VideoInformation): void {
    const competitionId = this.competition()?.id;
    if (!competitionId || !video.videoId) {
      return;
    }
    this.addError.set(null);
    this.competitions
      .addVideo(competitionId, video.videoId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () =>
          this.competition.update((c) =>
            c ? { ...c, videos: [...(c.videos ?? []), video] } : c,
          ),
        error: () =>
          this.addError.set(
            `Couldn’t add “${video.name}”. It may already belong to another competition.`,
          ),
      });
  }

  deleteCompetition(): void {
    const current = this.competition();
    if (!current?.id) {
      return;
    }
    const confirmed = this.doc.defaultView?.confirm(
      `Delete “${current.name}”? Its recordings stay in your library but are no longer grouped, and its shared links stop working.`,
    );
    if (!confirmed) {
      return;
    }
    this.competitions
      .deleteCompetition(current.id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => void this.router.navigate(['/competitions']),
      });
  }

  openShare(): void {
    this.shareOpen.set(true);
  }

  closeShare(): void {
    this.shareOpen.set(false);
  }
}
