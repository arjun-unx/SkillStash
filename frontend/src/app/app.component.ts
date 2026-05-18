import { ChangeDetectionStrategy, Component, OnInit } from '@angular/core';
import { AuthService } from '@core/services/auth.service';

@Component({
  selector: 'ps-root',
  templateUrl: './app.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  standalone: false
})
export class AppComponent implements OnInit {
  constructor(private readonly auth: AuthService) {}

  ngOnInit(): void {
    this.auth.refreshUserIfAuthenticated().subscribe({ error: () => void 0 });
  }
}
