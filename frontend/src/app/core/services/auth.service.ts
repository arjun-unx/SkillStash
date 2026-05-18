import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable, of, tap } from 'rxjs';
import { environment } from '@env/environment';
import { API_ROUTES } from '@core/models/app.constants';
import { AuthResponse, CurrentUser, LoginRequest, RegisterRequest } from '@core/models/auth.model';
import { TokenService } from './token.service';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly _currentUser$ = new BehaviorSubject<CurrentUser | null>(this.tokens.user);
  readonly currentUser$ = this._currentUser$.asObservable();

  constructor(private readonly http: HttpClient, private readonly tokens: TokenService) {}

  get isAuthenticated(): boolean {
    return this.tokens.hasValidSession();
  }

  get currentUserSnapshot(): CurrentUser | null {
    return this._currentUser$.value;
  }

  login(payload: LoginRequest): Observable<AuthResponse> {
    return this.http
      .post<AuthResponse>(`${environment.apiBaseUrl}${API_ROUTES.authLogin}`, payload)
      .pipe(tap(res => this.persistSession(res)));
  }

  register(payload: RegisterRequest): Observable<AuthResponse> {
    return this.http
      .post<AuthResponse>(`${environment.apiBaseUrl}${API_ROUTES.authRegister}`, payload)
      .pipe(tap(res => this.persistSession(res)));
  }

  fetchMe(): Observable<CurrentUser> {
    return this.http
      .get<CurrentUser>(`${environment.apiBaseUrl}${API_ROUTES.authMe}`)
      .pipe(tap(user => this._currentUser$.next(user)));
  }

  refreshUserIfAuthenticated(): Observable<CurrentUser | null> {
    if (!this.isAuthenticated) return of(null);
    return this.fetchMe();
  }

  logout(): void {
    this.tokens.clearSession();
    this._currentUser$.next(null);
  }

  private persistSession(res: AuthResponse): void {
    const user: CurrentUser = {
      id: res.userId,
      email: res.email,
      userName: res.userName,
      displayName: res.displayName,
      bio: null,
      avatarUrl: null,
      emailNotificationsEnabled: true
    };
    this.tokens.saveSession(res.accessToken, res.expiresAtUtc, user);
    this._currentUser$.next(user);
  }
}
