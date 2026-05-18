import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { SkillCommentDto, SkillDto } from '@core/models/skill.model';
import { AuthService } from '@core/services/auth.service';
import { NotificationService } from '@core/services/notification.service';
import { SkillService } from '@core/services/skill.service';
import { UI } from '@core/ui/ui-classes';

@Component({
  selector: 'ps-skill-detail',
  templateUrl: './skill-detail.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  standalone: false
})
export class SkillDetailComponent implements OnInit {
  readonly ui = UI;
  loading = true;
  skill: SkillDto | null = null;
  isMine = false;

  comments: SkillCommentDto[] = [];
  loadingComments = false;
  commentBody = '';

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly skills: SkillService,
    readonly auth: AuthService,
    private readonly notify: NotificationService,
    private readonly cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id')!;
    this.skills.byId(id).subscribe({
      next: p => {
        this.skill = p;
        this.isMine = this.auth.currentUserSnapshot?.id === p.authorId;
        this.loading = false;
        this.cdr.markForCheck();
        this.loadComments(id);
      },
      error: () => {
        this.loading = false;
        this.cdr.markForCheck();
      }
    });

    this.route.fragment.subscribe(f => {
      if (f === 'comments') {
        queueMicrotask(() => document.getElementById('comments')?.scrollIntoView({ behavior: 'smooth', block: 'start' }));
      }
    });
  }

  private loadComments(skillId: string): void {
    this.loadingComments = true;
    this.cdr.markForCheck();
    this.skills.comments(skillId, 1, 40).subscribe({
      next: res => {
        this.comments = res.items;
        this.loadingComments = false;
        this.cdr.markForCheck();
      },
      error: () => {
        this.loadingComments = false;
        this.cdr.markForCheck();
      }
    });
  }

  toggleLike(p: SkillDto): void {
    if (!this.auth.isAuthenticated) {
      this.router.navigate(['/auth/login']);
      return;
    }
    this.skills.toggleLike(p.id).subscribe(res => {
      p.likedByCurrentUser = res.liked;
      p.likeCount = res.likeCount;
      this.cdr.markForCheck();
    });
  }

  toggleBookmark(p: SkillDto): void {
    if (!this.auth.isAuthenticated) {
      this.router.navigate(['/auth/login']);
      return;
    }
    this.skills.toggleBookmark(p.id).subscribe(res => {
      p.bookmarkedByCurrentUser = res.bookmarked;
      p.bookmarkCount = res.bookmarkCount;
      this.cdr.markForCheck();
    });
  }

  async copy(p: SkillDto): Promise<void> {
    try {
      await navigator.clipboard.writeText(p.body);
      this.skills.trackCopy(p.id).subscribe(res => {
        p.copyCount = res.copyCount;
        this.cdr.markForCheck();
      });
      this.notify.success('Skill copied to clipboard');
    } catch {
      this.notify.error('Could not copy to clipboard');
    }
  }

  async share(p: SkillDto): Promise<void> {
    const url = `${window.location.origin}/skills/${p.id}`;
    try {
      if (navigator.share) await navigator.share({ title: p.title, url });
      else {
        await navigator.clipboard.writeText(url);
        this.notify.success('Link copied');
      }
    } catch {
      try {
        await navigator.clipboard.writeText(url);
        this.notify.success('Link copied');
      } catch {
        this.notify.error('Could not share');
      }
    }
  }

  comment(_p: SkillDto): void {
    document.getElementById('comments')?.scrollIntoView({ behavior: 'smooth' });
  }

  submitComment(): void {
    if (!this.skill || !this.commentBody.trim()) return;
    if (!this.auth.isAuthenticated) {
      this.router.navigate(['/auth/login']);
      return;
    }
    this.skills.addComment(this.skill.id, this.commentBody.trim()).subscribe({
      next: c => {
        this.comments = [c, ...this.comments];
        if (this.skill) this.skill.commentCount = (this.skill.commentCount ?? 0) + 1;
        this.commentBody = '';
        this.notify.success('Comment posted');
        this.cdr.markForCheck();
      },
      error: () => this.notify.error('Could not post comment')
    });
  }

  edit(): void {
    if (this.skill) this.router.navigate(['/skills', this.skill.id, 'edit']);
  }

  remove(): void {
    if (!this.skill) return;
    if (!confirm('Delete this skill?')) return;
    this.skills.delete(this.skill.id).subscribe(() => {
      this.notify.success('Skill deleted');
      this.router.navigate(['/skills/mine']);
    });
  }
}
