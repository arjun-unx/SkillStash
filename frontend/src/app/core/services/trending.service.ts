import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { environment } from '@env/environment';
import { normalizeToggleTrendingBookmark } from '@core/utils/api-normalize';
import {
  ToggleTrendingBookmarkResponse,
  TrendingSkillDto,
  TrendingProviderDto,
  TrendingSortMode
} from '@core/models/trending.model';
import { PaginatedList } from '@core/models/skill.model';

export interface TrendingSearchQuery {
  provider?: string | null;
  role?: string | null;
  category?: string | null;
  search?: string | null;
  sort?: TrendingSortMode;
  page?: number;
  pageSize?: number;
}

@Injectable({ providedIn: 'root' })
export class TrendingService {
  private readonly base = `${environment.apiBaseUrl}/trending`;

  constructor(private readonly http: HttpClient) {}

  providers(): Observable<TrendingProviderDto[]> {
    return this.http.get<TrendingProviderDto[]>(`${this.base}/providers`);
  }

  search(q: TrendingSearchQuery = {}): Observable<PaginatedList<TrendingSkillDto>> {
    let params = new HttpParams()
      .set('page', (q.page ?? 1).toString())
      .set('pageSize', (q.pageSize ?? 20).toString())
      .set('sort', q.sort ?? 'trending');
    if (q.provider) params = params.set('provider', q.provider);
    if (q.role) params = params.set('role', q.role);
    if (q.category) params = params.set('category', q.category);
    if (q.search) params = params.set('search', q.search);
    return this.http.get<PaginatedList<TrendingSkillDto>>(`${this.base}/skills`, { params });
  }

  byId(id: string): Observable<TrendingSkillDto> {
    return this.http.get<TrendingSkillDto>(`${this.base}/skills/${id}`);
  }

  setBookmark(id: string, bookmarked: boolean): Observable<ToggleTrendingBookmarkResponse> {
    return this.http
      .post<unknown>(`${this.base}/skills/${id}/bookmark`, { bookmarked })
      .pipe(map(normalizeToggleTrendingBookmark));
  }

  trackUse(id: string): Observable<{ useCount: number }> {
    return this.http.post<{ useCount: number }>(`${this.base}/skills/${id}/use`, {});
  }

  sync(): Observable<{ synced: number }> {
    return this.http.post<{ synced: number }>(`${this.base}/sync`, {});
  }
}
