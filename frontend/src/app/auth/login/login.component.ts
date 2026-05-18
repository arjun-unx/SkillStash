import { ChangeDetectionStrategy, ChangeDetectorRef, Component } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { NotificationService } from '@core/services/notification.service';
import { AuthService } from '@core/services/auth.service';
import { UI } from '@core/ui/ui-classes';

@Component({
  selector: 'ps-login',
  templateUrl: './login.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  standalone: false
})
export class LoginComponent {
  readonly ui = UI;
  loading = false;

  readonly form = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', Validators.required]
  });

  constructor(
    private readonly fb: FormBuilder,
    private readonly auth: AuthService,
    private readonly router: Router,
    private readonly route: ActivatedRoute,
    private readonly notify: NotificationService,
    private readonly cdr: ChangeDetectorRef
  ) {}

  submit(): void {
    if (this.form.invalid) return;
    this.loading = true;
    const { email, password } = this.form.getRawValue();
    this.auth.login({ email, password }).subscribe({
      next: () => {
        const redirect = this.route.snapshot.queryParamMap.get('redirect') ?? '/feed';
        this.router.navigateByUrl(redirect);
      },
      error: () => {
        this.loading = false;
        this.cdr.markForCheck();
      }
    });
  }
}
