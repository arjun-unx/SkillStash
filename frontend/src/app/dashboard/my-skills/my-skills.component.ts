import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { PaginatedList, SkillDto } from '@core/models/skill.model';
import { NotificationService } from '@core/services/notification.service';
import { SkillService } from '@core/services/skill.service';
import { applySkillBookmarkToggle } from '@core/utils/bookmark-actions';
import { UI } from '@core/ui/ui-classes';

@Component({
  selector: 'ps-my-skills',
  templateUrl: './my-skills.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  standalone: false
})
export class MySkillsComponent implements OnInit {
  readonly ui = UI;
  loading = true;
  data: PaginatedList<SkillDto> | null = null;

  constructor(
    private readonly skills: SkillService,
    private readonly router: Router,
    private readonly notify: NotificationService,
    private readonly cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.skills.mine().subscribe({
      next: data => {
        this.data = data;
        this.loading = false;
        this.cdr.markForCheck();
      },
      error: () => {
        this.loading = false;
        this.cdr.markForCheck();
      }
    });
  }

  trackById(_: number, p: SkillDto): string {
    return p.id;
  }

  edit(p: SkillDto): void {
    this.router.navigate(['/skills', p.id, 'edit']);
  }

  create(): void {
    this.router.navigate(['/skills/new']);
  }

  onLike(p: SkillDto): void {
    this.skills.toggleLike(p.id).subscribe(res => {
      p.likedByCurrentUser = res.liked;
      p.likeCount = res.likeCount;
      this.cdr.markForCheck();
    });
  }

  onBookmark(p: SkillDto): void {
    this.skills.setBookmark(p.id, !p.bookmarkedByCurrentUser).subscribe({
      next: res => {
        applySkillBookmarkToggle(p, res.bookmarked, res.bookmarkCount);
        this.cdr.markForCheck();
      },
      error: () => this.notify.error('Could not update bookmark')
    });
  }

  async onCopy(p: SkillDto): Promise<void> {
    try {
      await navigator.clipboard.writeText(p.body);
      this.skills.trackCopy(p.id).subscribe(r => {
        p.copyCount = r.copyCount;
        this.cdr.markForCheck();
      });
      this.notify.success('Skill copied to clipboard');
    } catch {
      this.notify.error('Copy failed');
    }
  }
}
