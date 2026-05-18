import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  EventEmitter,
  HostListener,
  inject,
  Input,
  Output
} from '@angular/core';
import { SkillDto } from '@core/models/skill.model';

@Component({
  selector: 'ps-skill-card',
  templateUrl: './skill-card.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  standalone: false
})
export class SkillCardComponent {
  private readonly host = inject(ElementRef<HTMLElement>);

  @Input({ required: true }) skill!: SkillDto;
  @Input() showAuthor = true;
  @Input() canEdit = false;
  @Input() libraryMode = false;
  @Input() collections: { id: string; name: string }[] | null = null;

  @Output() likeToggled = new EventEmitter<SkillDto>();
  @Output() bookmarkToggled = new EventEmitter<SkillDto>();
  @Output() copyClicked = new EventEmitter<SkillDto>();
  @Output() commentClicked = new EventEmitter<SkillDto>();
  @Output() shareClicked = new EventEmitter<SkillDto>();
  @Output() editClicked = new EventEmitter<SkillDto>();
  @Output() moveCollection = new EventEmitter<{ skill: SkillDto; collectionId: string | null }>();

  menuOpen = false;

  @HostListener('document:click', ['$event'])
  onDocClick(ev: MouseEvent): void {
    if (!this.menuOpen) return;
    const t = ev.target as Node;
    if (!this.host.nativeElement.contains(t)) this.menuOpen = false;
  }

  toggleMenu(ev: Event): void {
    ev.stopPropagation();
    this.menuOpen = !this.menuOpen;
  }

  toggleLike(): void {
    this.likeToggled.emit(this.skill);
  }

  toggleBookmark(): void {
    this.bookmarkToggled.emit(this.skill);
  }

  copy(): void {
    this.copyClicked.emit(this.skill);
  }

  comment(): void {
    this.commentClicked.emit(this.skill);
  }

  share(): void {
    this.shareClicked.emit(this.skill);
  }

  edit(): void {
    this.editClicked.emit(this.skill);
  }

  onMoveSelect(ev: Event): void {
    const sel = ev.target as HTMLSelectElement;
    const v = sel.value;
    const collectionId = v === '' ? null : v;
    this.moveCollection.emit({ skill: this.skill, collectionId });
    sel.selectedIndex = 0;
  }
}
