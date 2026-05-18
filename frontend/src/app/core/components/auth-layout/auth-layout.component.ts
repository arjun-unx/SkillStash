import { ChangeDetectionStrategy, Component } from '@angular/core';

@Component({
  selector: 'ps-auth-layout',
  templateUrl: './auth-layout.component.html',  changeDetection: ChangeDetectionStrategy.OnPush,
  standalone: false
})
export class AuthLayoutComponent {}
