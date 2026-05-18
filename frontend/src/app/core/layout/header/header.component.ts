import { ChangeDetectionStrategy, Component } from '@angular/core';
import { Router } from '@angular/router';
import { Observable } from 'rxjs';
import { CurrentUser } from '@core/models/auth.model';
import { AuthService } from '@core/services/auth.service';

@Component({
  selector: 'ps-header',
  templateUrl: './header.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  standalone: false
})
export class HeaderComponent {
  readonly user$: Observable<CurrentUser | null>;

  constructor(private readonly auth: AuthService, private readonly router: Router) {
    this.user$ = this.auth.currentUser$;
  }

  logout(): void {
    this.auth.logout();
    this.router.navigate(['/feed']);
  }
}
