import { ChangeDetectionStrategy, ChangeDetectorRef, Component } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '@core/services/auth.service';
import { NotificationService } from '@core/services/notification.service';
import { UI } from '@core/ui/ui-classes';

@Component({
  selector: 'ps-register',
  templateUrl: './register.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  standalone: false
})
export class RegisterComponent {
  readonly ui = UI;
  loading = false;
  readonly form = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    userName: ['', [Validators.required, Validators.minLength(3), Validators.pattern(/^[A-Za-z0-9_-]+$/)]],
    displayName: ['', [Validators.required, Validators.minLength(2)]],
    password: ['', [Validators.required, Validators.minLength(8)]]
  });

  constructor(
    private readonly fb: FormBuilder,
    private readonly auth: AuthService,
    private readonly notify: NotificationService,
    private readonly router: Router,
    private readonly cdr: ChangeDetectorRef
  ) {}

  submit(): void {
    if (this.form.invalid) return;
    this.loading = true;
    this.auth.register(this.form.getRawValue()).subscribe({
      next: () => {
        this.notify.success('Account created. Welcome to SkillStash!');
        this.router.navigateByUrl('/feed');
      },
      error: () => {
        this.loading = false;
        this.cdr.markForCheck();
      }
    });
  }
}
