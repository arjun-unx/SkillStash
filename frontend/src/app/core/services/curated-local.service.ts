import { Injectable } from '@angular/core';

export interface CuratedSavedItem {
  id: string;
  title: string;
  snippet: string;
  body: string;
  providerSlug: string;
  providerLabel: string;
  role: string;
  tags: string[];
  savedAtUtc: string;
}

const KEY = 'ps.curatedBookmarks';

@Injectable({ providedIn: 'root' })
export class CuratedLocalService {
  list(): CuratedSavedItem[] {
    try {
      const raw = localStorage.getItem(KEY);
      if (!raw) return [];
      const parsed = JSON.parse(raw) as CuratedSavedItem[];
      return Array.isArray(parsed) ? parsed : [];
    } catch {
      return [];
    }
  }

  isSaved(id: string): boolean {
    return this.list().some(x => x.id === id);
  }

  save(snapshot: Omit<CuratedSavedItem, 'savedAtUtc'>): void {
    const next = this.list().filter(x => x.id !== snapshot.id);
    next.unshift({ ...snapshot, savedAtUtc: new Date().toISOString() });
    localStorage.setItem(KEY, JSON.stringify(next));
  }

  remove(id: string): void {
    const next = this.list().filter(x => x.id !== id);
    localStorage.setItem(KEY, JSON.stringify(next));
  }

  toggle(snapshot: Omit<CuratedSavedItem, 'savedAtUtc'>): boolean {
    if (this.isSaved(snapshot.id)) {
      this.remove(snapshot.id);
      return false;
    }
    this.save(snapshot);
    return true;
  }
}
