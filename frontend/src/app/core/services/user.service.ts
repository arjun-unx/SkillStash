import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '@env/environment';
import { API_ROUTES } from '@core/models/app.constants';
import { ToggleFollowResponse, UserProfileDto } from '@core/models/skill.model';

@Injectable({ providedIn: 'root' })
export class UserService {
  private readonly base = `${environment.apiBaseUrl}${API_ROUTES.users}`;

  constructor(private readonly http: HttpClient) {}

  getProfile(userName: string): Observable<UserProfileDto> {
    return this.http.get<UserProfileDto>(`${this.base}/${encodeURIComponent(userName)}`);
  }

  toggleFollow(userName: string): Observable<ToggleFollowResponse> {
    return this.http.post<ToggleFollowResponse>(`${this.base}/${encodeURIComponent(userName)}/follow`, {});
  }
}
