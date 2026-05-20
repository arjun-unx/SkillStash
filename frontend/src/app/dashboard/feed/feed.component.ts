import {
  AfterViewInit,
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  Component,
  ElementRef,
  OnDestroy,
  OnInit,
  ViewChild
} from '@angular/core';
import { FormControl } from '@angular/forms';
import { Router } from '@angular/router';
import { debounceTime, distinctUntilChanged } from 'rxjs/operators';
import { EXPLORE_TAG_FILTERS } from '@core/models/discover.constants';
import { PaginatedList, SkillDto } from '@core/models/skill.model';
import { AuthService } from '@core/services/auth.service';
import { NotificationService } from '@core/services/notification.service';
import { SkillService } from '@core/services/skill.service';
import { applySkillBookmarkToggle } from '@core/utils/bookmark-actions';

type DiscoverTab = 'following' | 'explore';

@Component({
  selector: 'ps-feed',
  templateUrl: './feed.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  standalone: false,
  host: { class: 'flex min-h-0 flex-1 flex-col w-full' }
})
export class FeedComponent implements OnInit, AfterViewInit, OnDestroy {
  readonly exploreTags = EXPLORE_TAG_FILTERS;

  tab: DiscoverTab = 'explore';
  loading = false;
  loadingMore = false;
  searchCtrl = new FormControl<string>('', { nonNullable: true });
  /** At most one explore tag filter at a time; null = all tags. */
  selectedTag: string | null = null;

  items: SkillDto[] = [];
  page = 1;
  hasNext = false;
  totalCount = 0;

  @ViewChild('sentinel', { read: ElementRef }) sentinel?: ElementRef<HTMLElement>;
  private observer?: IntersectionObserver;
  private loadToken = 0;
  private readonly bookmarkInFlight = new Set<string>();

  constructor(
    readonly auth: AuthService,
    private readonly skills: SkillService,
    private readonly notify: NotificationService,
    private readonly router: Router,
    private readonly cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.reloadFromStart();

    this.searchCtrl.valueChanges.pipe(debounceTime(320), distinctUntilChanged()).subscribe(() => {
      if (this.tab === 'explore') this.reloadFromStart();
    });
  }

  ngAfterViewInit(): void {
    queueMicrotask(() => this.bindObserver());
  }

  ngOnDestroy(): void {
    this.observer?.disconnect();
  }

  trackById(_: number, p: SkillDto): string {
    return p.id;
  }

  setTab(next: DiscoverTab): void {
    if (this.tab === next) return;
    this.tab = next;
    this.reloadFromStart();
  }

  selectTag(tag: string | null): void {
    const next = this.selectedTag === tag ? null : tag;
    if (this.selectedTag === next) return;
    this.selectedTag = next;
    this.reloadFromStart();
  }

  isTagOn(tag: string): boolean {
    return this.selectedTag === tag;
  }

  reloadFromStart(): void {
    this.loadToken++;
    this.page = 1;
    this.items = [];
    this.hasNext = false;
    this.loading = true;
    document.querySelector('main')?.scrollTo({ top: 0, behavior: 'auto' });
    this.cdr.markForCheck();
    this.fetchPage(true);
  }

  private fetchPage(isInitial: boolean): void {
    const token = this.loadToken;
    const page = this.page;
    const pageSize = 12;

    if (this.tab === 'following') {
      if (!this.auth.isAuthenticated) {
        this.loading = false;
        this.loadingMore = false;
        this.items = [];
        this.cdr.markForCheck();
        return;
      }
      this.skills.followingFeed({ page, pageSize }).subscribe({
        next: data => this.applyPage(data, token, isInitial),
        error: () => this.onFetchError(token)
      });
      return;
    }

    const tags = this.selectedTag ? [this.selectedTag] : null;
    this.skills
      .feed({
        page,
        pageSize,
        search: this.searchCtrl.value?.trim() || null,
        tags
      })
      .subscribe({
        next: data => this.applyPage(data, token, isInitial),
        error: () => this.onFetchError(token)
      });
  }

  private applyPage(data: PaginatedList<SkillDto>, token: number, isInitial: boolean): void {
    if (token !== this.loadToken) return;
    if (isInitial) this.items = [...data.items];
    else this.items = [...this.items, ...data.items];
    this.page = data.pageNumber;
    this.hasNext = data.hasNext;
    this.totalCount = data.totalCount;
    this.loading = false;
    this.loadingMore = false;
    this.cdr.markForCheck();
    queueMicrotask(() => this.bindObserver());
  }

  private onFetchError(token: number): void {
    if (token !== this.loadToken) return;
    this.loading = false;
    this.loadingMore = false;
    this.cdr.markForCheck();
  }

  loadMore(): void {
    if (!this.hasNext || this.loading || this.loadingMore) return;
    if (this.tab === 'following' && !this.auth.isAuthenticated) return;
    this.loadingMore = true;
    this.page += 1;
    this.cdr.markForCheck();
    this.fetchPage(false);
  }

  private bindObserver(): void {
    this.observer?.disconnect();
    const el = this.sentinel?.nativeElement;
    const root = document.querySelector('main');
    if (!el || !root || !this.hasNext || this.loading) return;

    this.observer = new IntersectionObserver(
      entries => {
        if (entries[0]?.isIntersecting) this.loadMore();
      },
      { root, rootMargin: '400px 0px', threshold: 0 }
    );
    this.observer.observe(el);
  }

  onLikeToggled(p: SkillDto): void {
    if (!this.auth.isAuthenticated) {
      this.router.navigate(['/auth/login'], { queryParams: { redirect: '/feed' } });
      return;
    }
    this.skills.toggleLike(p.id).subscribe(res => {
      p.likedByCurrentUser = res.liked;
      p.likeCount = res.likeCount;
      this.cdr.markForCheck();
    });
  }

  onBookmarkToggled(p: SkillDto): void {
    if (!this.auth.isAuthenticated) {
      this.router.navigate(['/auth/login'], { queryParams: { redirect: '/feed' } });
      return;
    }
    if (this.bookmarkInFlight.has(p.id)) return;

    const wantBookmarked = !p.bookmarkedByCurrentUser;
    this.bookmarkInFlight.add(p.id);
    this.skills.setBookmark(p.id, wantBookmarked).subscribe({
      next: res => {
        applySkillBookmarkToggle(p, res.bookmarked, res.bookmarkCount);
        this.bookmarkInFlight.delete(p.id);
        this.notify.success(res.bookmarked ? 'Saved to library' : 'Removed from library');
        this.cdr.markForCheck();
      },
      error: () => {
        this.bookmarkInFlight.delete(p.id);
        this.notify.error('Could not update bookmark');
      }
    });
  }

  onComment(p: SkillDto): void {
    this.router.navigate(['/skills', p.id], { fragment: 'comments' });
  }

  async onShare(p: SkillDto): Promise<void> {
    const url = `${window.location.origin}/skills/${p.id}`;
    try {
      if (navigator.share) {
        await navigator.share({ title: p.title, url });
      } else {
        await navigator.clipboard.writeText(url);
        this.notify.success('Link copied');
      }
    } catch {
      try {
        await navigator.clipboard.writeText(url);
        this.notify.success('Link copied');
      } catch {
        this.notify.error('Could not share link');
      }
    }
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
      this.notify.error('Could not copy to clipboard');
    }
  }
}
