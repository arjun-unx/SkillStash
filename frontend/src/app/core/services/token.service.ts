import { Injectable } from '@angular/core';
import { CurrentUser } from '@core/models/auth.model';
import { STORAGE_KEYS } from '@core/models/app.constants';

@Injectable({ providedIn: 'root' })
export class TokenService {
  get accessToken(): string | null {
    return localStorage.getItem(STORAGE_KEYS.accessToken);
  }

  get expiresAtUtc(): string | null {
    return localStorage.getItem(STORAGE_KEYS.expiresAtUtc);
  }

  get user(): CurrentUser | null {
    const raw = localStorage.getItem(STORAGE_KEYS.user);
    return raw ? (JSON.parse(raw) as CurrentUser) : null;
  }

  saveSession(accessToken: string, expiresAtUtc: string, user: CurrentUser): void {
    localStorage.setItem(STORAGE_KEYS.accessToken, accessToken);
    localStorage.setItem(STORAGE_KEYS.expiresAtUtc, expiresAtUtc);
    localStorage.setItem(STORAGE_KEYS.user, JSON.stringify(user));
  }

  clearSession(): void {
    localStorage.removeItem(STORAGE_KEYS.accessToken);
    localStorage.removeItem(STORAGE_KEYS.expiresAtUtc);
    localStorage.removeItem(STORAGE_KEYS.user);
  }

  hasValidSession(): boolean {
    const token = this.accessToken;
    const expiry = this.expiresAtUtc;
    if (!token || !expiry) return false;
    return new Date(expiry).getTime() > Date.now();
  }
}
