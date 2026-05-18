import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output } from '@angular/core';
import { TrendingSkillDto } from '@core/models/trending.model';

@Component({
  selector: 'ps-trending-skill-card',
  templateUrl: './trending-skill-card.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  standalone: false
})
export class TrendingSkillCardComponent {
  @Input({ required: true }) skill!: TrendingSkillDto;

  @Output() copyClicked = new EventEmitter<TrendingSkillDto>();
  @Output() bookmarkToggled = new EventEmitter<TrendingSkillDto>();
  @Output() shareClicked = new EventEmitter<TrendingSkillDto>();
  @Output() useClicked = new EventEmitter<TrendingSkillDto>();
}
