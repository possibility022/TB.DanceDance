import { Injectable } from '@angular/core';

// Same key the previous app used, so anonymous comment ownership carries over.
const STORAGE_KEY = 'anonymousId';

/**
 * Stable per-browser identifier for attributing anonymous activity (commenting
 * on shared links while not logged in). Generated once and reused thereafter.
 */
@Injectable({ providedIn: 'root' })
export class AnonymousIdService {
  getId(): string {
    let id = localStorage.getItem(STORAGE_KEY);
    if (!id) {
      id = crypto.randomUUID();
      localStorage.setItem(STORAGE_KEY, id);
    }
    return id;
  }
}
