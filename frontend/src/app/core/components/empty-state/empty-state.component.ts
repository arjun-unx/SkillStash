import { ChangeDetectionStrategy, Component, Input } from '@angular/core';

@Component({
  selector: 'ps-empty-state',
  templateUrl: './empty-state.component.html',  changeDetection: ChangeDetectionStrategy.OnPush,
  standalone: false
})
export class EmptyStateComponent {
  @Input() icon = 'inbox';
  @Input() title = 'Nothing here yet';
  @Input() message: string | null = null;
}
