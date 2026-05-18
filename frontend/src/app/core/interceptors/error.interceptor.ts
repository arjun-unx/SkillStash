import {
  HttpErrorResponse,
  HttpEvent,
  HttpHandler,
  HttpInterceptor,
  HttpRequest
} from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Router } from '@angular/router';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { AuthService } from '@core/services/auth.service';
import { NotificationService } from '@core/services/notification.service';

@Injectable()
export class ErrorInterceptor implements HttpInterceptor {
  constructor(
    private readonly auth: AuthService,
    private readonly router: Router,
    private readonly notify: NotificationService
  ) {}

  intercept(req: HttpRequest<unknown>, next: HttpHandler): Observable<HttpEvent<unknown>> {
    return next.handle(req).pipe(
      catchError((err: HttpErrorResponse) => {
        if (err.status === 0) {
          this.notify.error('Unable to reach the API. Please check your connection.');
        } else if (err.status === 401) {
          this.auth.logout();
          this.router.navigate(['/auth/login']);
        } else if (err.status === 400 && err.error?.errors) {
          const first = Object.values(err.error.errors).flat()[0] as string | undefined;
          if (first) this.notify.error(first);
        } else if (err.status === 400 && typeof err.error === 'string' && err.error) {
          this.notify.error(err.error);
        } else if (err.error?.detail && err.status !== 404) {
          this.notify.error(err.error.detail);
        } else if (err.error?.title) {
          this.notify.error(err.error.title);
        }
        return throwError(() => err);
      })
    );
  }
}
