import { ChangeDetectionStrategy, Component, Input } from '@angular/core';

@Component({
  selector: 'ps-loading-spinner',
  templateUrl: './loading-spinner.component.html',  changeDetection: ChangeDetectionStrategy.OnPush,
  standalone: false
})
export class LoadingSpinnerComponent {
  @Input() label = 'Loading…';
}
