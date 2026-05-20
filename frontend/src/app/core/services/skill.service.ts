import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { environment } from '@env/environment';
import { normalizeToggleBookmark } from '@core/utils/api-normalize';
import { API_ROUTES } from '@core/models/app.constants';
import {
  BookmarkCollectionDto,
  CreateSkillRequest,
  PaginatedList,
  SkillCommentDto,
  SkillDto,
  ToggleBookmarkResponse,
  ToggleLikeResponse,
  TrackCopyResponse,
  UpdateSkillRequest
} from '@core/models/skill.model';

export interface FeedQuery {
  page?: number;
  pageSize?: number;
  search?: string | null;
  tags?: string[] | null;
}

@Injectable({ providedIn: 'root' })
export class SkillService {
  private readonly base = `${environment.apiBaseUrl}${API_ROUTES.skills}`;

  constructor(private readonly http: HttpClient) {}

  private feedParams(q: FeedQuery): HttpParams {
    let params = new HttpParams();
    const page = q.page ?? 1;
    const pageSize = q.pageSize ?? 20;
    params = params.set('page', page).set('pageSize', pageSize);
    if (q.search) params = params.set('search', q.search);
    (q.tags ?? []).forEach(t => {
      const v = t?.trim();
      if (v) params = params.append('tags', v);
    });
    return params;
  }

  feed(q: FeedQuery = {}): Observable<PaginatedList<SkillDto>> {
    return this.http.get<PaginatedList<SkillDto>>(this.base, { params: this.feedParams(q) });
  }

  followingFeed(q: FeedQuery = {}): Observable<PaginatedList<SkillDto>> {
    return this.http.get<PaginatedList<SkillDto>>(`${this.base}/following`, { params: this.feedParams(q) });
  }

  mine(q: FeedQuery = {}): Observable<PaginatedList<SkillDto>> {
    return this.http.get<PaginatedList<SkillDto>>(`${this.base}/mine`, { params: this.feedParams(q) });
  }

  byId(id: string): Observable<SkillDto> {
    return this.http.get<SkillDto>(`${this.base}/${id}`);
  }

  create(body: CreateSkillRequest): Observable<SkillDto> {
    return this.http.post<SkillDto>(this.base, body);
  }

  update(id: string, body: UpdateSkillRequest): Observable<SkillDto> {
    return this.http.put<SkillDto>(`${this.base}/${id}`, { ...body, skillId: id });
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/${id}`);
  }

  toggleLike(id: string): Observable<ToggleLikeResponse> {
    return this.http.post<ToggleLikeResponse>(`${this.base}/${id}/like`, {});
  }

  setBookmark(id: string, bookmarked: boolean, collectionId?: string | null): Observable<ToggleBookmarkResponse> {
    return this.http
      .post<unknown>(`${this.base}/${id}/bookmark`, { bookmarked, collectionId: collectionId ?? null })
      .pipe(map(normalizeToggleBookmark));
  }

  trackCopy(id: string): Observable<TrackCopyResponse> {
    return this.http.post<TrackCopyResponse>(`${this.base}/${id}/copy`, {});
  }

  comments(id: string, page = 1, pageSize = 20): Observable<PaginatedList<SkillCommentDto>> {
    const params = new HttpParams().set('page', page).set('pageSize', pageSize);
    return this.http.get<PaginatedList<SkillCommentDto>>(`${this.base}/${id}/comments`, { params });
  }

  addComment(id: string, body: string): Observable<SkillCommentDto> {
    return this.http.post<SkillCommentDto>(`${this.base}/${id}/comments`, { body });
  }
}
