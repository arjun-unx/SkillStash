import { ChangeDetectionStrategy, Component } from '@angular/core';

@Component({
  selector: 'ps-main-layout',
  templateUrl: './main-layout.component.html',  changeDetection: ChangeDetectionStrategy.OnPush,
  standalone: false
})
export class MainLayoutComponent {}
