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

  onBookmarkClick(ev: Event): void {
    ev.stopPropagation();
    ev.preventDefault();
    this.bookmarkToggled.emit(this.skill);
  }

  onCopyClick(ev: Event): void {
    ev.stopPropagation();
    ev.preventDefault();
    this.copyClicked.emit(this.skill);
  }

  onShareClick(ev: Event): void {
    ev.stopPropagation();
    ev.preventDefault();
    this.shareClicked.emit(this.skill);
  }

  onUseClick(ev: Event): void {
    ev.stopPropagation();
    ev.preventDefault();
    this.useClicked.emit(this.skill);
  }
}
