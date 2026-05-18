import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '@env/environment';
import { API_ROUTES } from '@core/models/app.constants';
import { BookmarkCollectionDto, PaginatedList, SkillDto } from '@core/models/skill.model';

@Injectable({ providedIn: 'root' })
export class LibraryService {
  private readonly base = `${environment.apiBaseUrl}${API_ROUTES.library}`;

  constructor(private readonly http: HttpClient) {}

  listCollections(): Observable<BookmarkCollectionDto[]> {
    return this.http.get<BookmarkCollectionDto[]>(`${this.base}/collections`);
  }

  createCollection(name: string): Observable<BookmarkCollectionDto> {
    return this.http.post<BookmarkCollectionDto>(`${this.base}/collections`, { name });
  }

  deleteCollection(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/collections/${id}`);
  }

  bookmarks(page = 1, pageSize = 20, collectionId?: string | null): Observable<PaginatedList<SkillDto>> {
    let params = new HttpParams().set('page', page).set('pageSize', pageSize);
    if (collectionId) params = params.set('collectionId', collectionId);
    return this.http.get<PaginatedList<SkillDto>>(`${this.base}/bookmarks`, { params });
  }

  moveBookmark(skillId: string, collectionId: string | null): Observable<void> {
    return this.http.put<void>(`${this.base}/bookmarks/${skillId}/collection`, {
      collectionId
    });
  }
}
