import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { AccessService } from '../../core/api/access.service';
import { RequestedAccessModel } from '../../core/api/api-models';
import { LongDatePipe } from '../../shared/format/long-date.pipe';

@Component({
  selector: 'app-access-requests',
  imports: [LongDatePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './access-requests.html',
})
export class AccessRequests {
  private readonly access = inject(AccessService);
  private readonly destroyRef = inject(DestroyRef);

  readonly loading = signal(true);
  readonly failed = signal(false);
  readonly requests = signal<readonly RequestedAccessModel[]>([]);
  readonly processingId = signal<string | null>(null);

  constructor() {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.failed.set(false);

    this.access
      .listAccessRequests()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (response) => {
          this.requests.set(response.accessRequests ?? []);
          this.loading.set(false);
        },
        error: () => {
          this.failed.set(true);
          this.loading.set(false);
        },
      });
  }

  requestorName(request: RequestedAccessModel): string {
    return [request.requestorFirstName, request.requestorLastName].filter(Boolean).join(' ').trim() || 'Someone';
  }

  decide(request: RequestedAccessModel, isApproved: boolean): void {
    if (!request.requestId || this.processingId()) {
      return;
    }
    this.processingId.set(request.requestId);

    this.access
      .approveAccessRequest({
        requestId: request.requestId,
        isGroup: request.isGroup,
        isApproved,
      })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.processingId.set(null);
          this.load();
        },
        error: () => this.processingId.set(null),
      });
  }
}
