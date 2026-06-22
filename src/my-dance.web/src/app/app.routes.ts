import { Routes } from '@angular/router';
import { autoLoginPartialRoutesGuard } from 'angular-auth-oidc-client';

import { adminGuard } from './core/auth/auth.guard';

export const routes: Routes = [
  // --- Public ---
  {
    path: '',
    title: 'Dance Dance',
    loadComponent: () => import('./features/home/home').then((m) => m.Home),
  },
  {
    // Stable public URL — shared links are handed out to other people.
    path: 'shared/:linkId',
    title: 'Shared recording · Dance Dance',
    loadComponent: () =>
      import('./features/sharing/shared-link-viewer').then((m) => m.SharedLinkViewer),
  },

  // --- Authenticated ---
  {
    path: 'videos',
    title: 'Lesson recordings · Dance Dance',
    canActivate: [autoLoginPartialRoutesGuard],
    loadComponent: () => import('./features/videos/group-videos').then((m) => m.GroupVideos),
  },
  {
    path: 'videos/my',
    title: 'My recordings · Dance Dance',
    canActivate: [autoLoginPartialRoutesGuard],
    loadComponent: () => import('./features/videos/my-videos').then((m) => m.MyVideos),
  },
  {
    path: 'videos/upload',
    title: 'Upload · Dance Dance',
    canActivate: [autoLoginPartialRoutesGuard],
    loadComponent: () => import('./features/upload/upload').then((m) => m.Upload),
  },
  {
    path: 'transfers',
    title: 'My transfers · Dance Dance',
    canActivate: [autoLoginPartialRoutesGuard],
    loadComponent: () => import('./features/transfers/my-transfers').then((m) => m.MyTransfers),
  },
  {
    path: 'competitions',
    title: 'Competitions · Dance Dance',
    canActivate: [authGuard],
    loadComponent: () => import('./features/competitions/competitions').then((m) => m.Competitions),
  },
  {
    path: 'competitions/:competitionId',
    title: 'Competition · Dance Dance',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/competitions/competition-detail').then((m) => m.CompetitionDetail),
  },
  {
    // Stable URL — transfer links are handed out to other people (but require login).
    // autoLoginPartialRoutesGuard preserves this exact URL (incl. query string) through
    // the OIDC login round trip.
    path: 'transfer/:linkId',
    title: 'Incoming transfer · Dance Dance',
    canActivate: [autoLoginPartialRoutesGuard],
    loadComponent: () => import('./features/transfers/transfer-landing').then((m) => m.TransferLanding),
  },
  {
    path: 'videos/requestassignment',
    title: 'Request access · Dance Dance',
    canActivate: [autoLoginPartialRoutesGuard],
    loadComponent: () => import('./features/access/request-access').then((m) => m.RequestAccess),
  },
  {
    // Keep below the static `videos/*` paths so they aren't captured as ids.
    // Param is the blob id — the key the stream/info endpoints use.
    path: 'videos/:blobId',
    title: 'Watch · Dance Dance',
    canActivate: [autoLoginPartialRoutesGuard],
    loadComponent: () => import('./features/videos/video-player').then((m) => m.VideoPlayer),
  },
  {
    path: 'events',
    title: 'Events · Dance Dance',
    canActivate: [autoLoginPartialRoutesGuard],
    loadComponent: () => import('./features/events/events').then((m) => m.Events),
  },
  {
    path: 'access/requestedaccesses',
    title: 'Access requests · Dance Dance',
    canActivate: [autoLoginPartialRoutesGuard, adminGuard],
    loadComponent: () => import('./features/access/access-requests').then((m) => m.AccessRequests),
  },
  {
    path: 'groups/create',
    title: 'Create group · Dance Dance',
    canActivate: [authGuard],
    loadComponent: () => import('./features/groups/create-group').then((m) => m.CreateGroup),
  },
  {
    // Lists the groups the current user administers; select one to manage it.
    path: 'groups/manage',
    title: 'Manage groups · Dance Dance',
    canActivate: [authGuard],
    loadComponent: () => import('./features/groups/groups-list').then((m) => m.GroupsList),
  },
  {
    // Admin-only management for a single group; server enforces the admin check.
    path: 'groups/:groupId/manage',
    title: 'Group management · Dance Dance',
    canActivate: [authGuard],
    loadComponent: () => import('./features/groups/group-management').then((m) => m.GroupManagement),
  },

  // --- OIDC plumbing (public) ---
  {
    path: 'callback',
    loadComponent: () => import('./features/auth/callback').then((m) => m.Callback),
  },
  {
    path: 'logout',
    loadComponent: () => import('./features/auth/logout').then((m) => m.Logout),
  },
  {
    path: 'logout/callback',
    loadComponent: () => import('./features/auth/logout-callback').then((m) => m.LogoutCallback),
  },
  {
    path: 'silentrenew',
    loadComponent: () => import('./features/auth/silent-renew').then((m) => m.SilentRenew),
  },

  // --- Fallback ---
  {
    path: '**',
    title: 'Not found · Dance Dance',
    loadComponent: () => import('./features/not-found/not-found').then((m) => m.NotFound),
  },
];
