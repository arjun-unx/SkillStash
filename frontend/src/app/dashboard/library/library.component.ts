import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnDestroy, OnInit } from '@angular/core';
import { NavigationEnd, Router } from '@angular/router';
import { filter, Subscription } from 'rxjs';
import { forkJoin, of } from 'rxjs';
import { BookmarkCollectionDto, PaginatedList, SkillDto } from '@core/models/skill.model';
import { TrendingSkillDto } from '@core/models/trending.model';
import { AuthService } from '@core/services/auth.service';
import { CuratedLocalService, CuratedSavedItem } from '@core/services/curated-local.service';
import { LibraryService } from '@core/services/library.service';
import { NotificationService } from '@core/services/notification.service';
import { SkillService } from '@core/services/skill.service';
import { TrendingService } from '@core/services/trending.service';
import { applySkillBookmarkToggle } from '@core/utils/bookmark-actions';

@Component({
  selector: 'ps-library',
  templateUrl: './library.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  standalone: false
})
export class LibraryComponent implements OnInit, OnDestroy {
  loading = true;
  collections: BookmarkCollectionDto[] = [];
  /** null = all bookmarks */
  filterCollectionId: string | null = null;
  data: PaginatedList<SkillDto> | null = null;
  deviceSaves: CuratedSavedItem[] = [];
  trendingSaves: TrendingSkillDto[] = [];
  section: 'community' | 'device' = 'community';

  private navSub?: Subscription;

  constructor(
    private readonly library: LibraryService,
    private readonly skills: SkillService,
    private readonly trending: TrendingService,
    private readonly curated: CuratedLocalService,
    readonly auth: AuthService,
    private readonly notify: NotificationService,
    private readonly router: Router,
    private readonly cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.refreshAll();
    this.navSub = this.router.events
      .pipe(filter((e): e is NavigationEnd => e instanceof NavigationEnd))
      .subscribe(e => {
        if (e.urlAfterRedirects.startsWith('/library')) this.refreshAll();
      });
  }

  ngOnDestroy(): void {
    this.navSub?.unsubscribe();
  }

  refreshAll(): void {
    this.loading = true;
    this.cdr.markForCheck();
    const trending$ = this.auth.isAuthenticated
      ? this.library.trendingBookmarks(1, 60)
      : of<PaginatedList<TrendingSkillDto>>({
          items: [],
          pageNumber: 1,
          pageSize: 60,
          totalCount: 0,
          totalPages: 0,
          hasPrevious: false,
          hasNext: false
        });

    forkJoin({
      cols: this.library.listCollections(),
      marks: this.library.bookmarks(1, 60, this.filterCollectionId ?? undefined),
      trending: trending$
    }).subscribe({
      next: ({ cols, marks, trending }) => {
        this.collections = cols;
        this.data = marks;
        this.trendingSaves = trending.items;
        this.deviceSaves = this.curated.list();
        this.loading = false;
        this.cdr.markForCheck();
      },
      error: () => {
        this.loading = false;
        this.cdr.markForCheck();
      }
    });
  }

  setSection(next: 'community' | 'device'): void {
    this.section = next;
    this.cdr.markForCheck();
  }

  selectCollection(id: string | null): void {
    this.filterCollectionId = id;
    this.loading = true;
    this.cdr.markForCheck();
    this.library.bookmarks(1, 60, id ?? undefined).subscribe({
      next: marks => {
        this.data = marks;
        this.loading = false;
        this.cdr.markForCheck();
      },
      error: () => {
        this.loading = false;
        this.cdr.markForCheck();
      }
    });
  }

  createCollection(): void {
    const name = window.prompt('Collection name');
    if (!name?.trim()) return;
    this.library.createCollection(name.trim()).subscribe({
      next: c => {
        this.collections = [...this.collections, c];
        this.notify.success('Collection created');
        this.cdr.markForCheck();
      },
      error: () => this.notify.error('Could not create')
    });
  }

  deleteCollection(c: BookmarkCollectionDto): void {
    if (!confirm(`Delete “${c.name}” ? Bookmarks stay saved.`)) return;
    this.library.deleteCollection(c.id).subscribe({
      next: () => {
        this.collections = this.collections.filter(x => x.id !== c.id);
        if (this.filterCollectionId === c.id) this.filterCollectionId = null;
        this.refreshAll();
        this.notify.success('Collection deleted');
      },
      error: () => this.notify.error('Could not delete')
    });
  }

  trackById(_: number, p: SkillDto): string {
    return p.id;
  }

  onLike(p: SkillDto): void {
    this.skills.toggleLike(p.id).subscribe(res => {
      p.likedByCurrentUser = res.liked;
      p.likeCount = res.likeCount;
      this.cdr.markForCheck();
    });
  }

  onBookmark(p: SkillDto): void {
    const wantBookmarked = !p.bookmarkedByCurrentUser;
    this.skills.setBookmark(p.id, wantBookmarked).subscribe({
      next: res => {
        applySkillBookmarkToggle(p, res.bookmarked, res.bookmarkCount);
        this.refreshAll();
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
      this.notify.success('Copied');
    } catch {
      this.notify.error('Copy failed');
    }
  }

  onMove(ev: { skill: SkillDto; collectionId: string | null }): void {
    this.library.moveBookmark(ev.skill.id, ev.collectionId).subscribe({
      next: () => {
        this.notify.success('Moved');
        this.refreshAll();
      },
      error: () => this.notify.error('Move failed')
    });
  }

  removeTrendingSave(p: TrendingSkillDto): void {
    if (this.auth.isAuthenticated) {
      this.trending.setBookmark(p.id, false).subscribe({
        next: () => {
          this.trendingSaves = this.trendingSaves.filter(x => x.id !== p.id);
          this.curated.remove(p.id);
          this.notify.info('Removed');
          this.cdr.markForCheck();
        },
        error: () => this.notify.error('Could not remove save')
      });
      return;
    }
    this.curated.remove(p.id);
    this.deviceSaves = this.curated.list();
    this.notify.info('Removed');
    this.cdr.markForCheck();
  }

  removeDevice(s: CuratedSavedItem): void {
    this.curated.remove(s.id);
    this.deviceSaves = this.curated.list();
    this.notify.info('Removed');
    this.cdr.markForCheck();
  }

  async copyTrending(p: TrendingSkillDto): Promise<void> {
    try {
      await navigator.clipboard.writeText(p.body);
      this.trending.trackUse(p.id).subscribe(r => {
        p.useCount = r.useCount;
        this.cdr.markForCheck();
      });
      this.notify.success('Copied');
    } catch {
      this.notify.error('Copy failed');
    }
  }

  async copyDevice(s: CuratedSavedItem): Promise<void> {
    try {
      await navigator.clipboard.writeText(s.body);
      this.notify.success('Copied');
    } catch {
      this.notify.error('Copy failed');
    }
  }

  /** Trending saves for the device tab: account sync when logged in, else local-only items. */
  get deviceTrendingItems(): TrendingSkillDto[] {
    if (this.auth.isAuthenticated) return this.trendingSaves;
    return this.deviceSaves.map(s => ({
      id: s.id,
      providerSlug: s.providerSlug,
      providerName: s.providerLabel,
      sourceName: s.providerLabel,
      sourceUrl: '',
      title: s.title,
      body: s.body,
      snippet: s.snippet,
      roleCategory: s.role,
      category: null,
      tags: s.tags,
      trendingScore: 0,
      useCount: 0,
      saveCount: 0,
      rating: 0,
      sourceUpdatedAtUtc: null,
      syncedAtUtc: s.savedAtUtc,
      bookmarkedByCurrentUser: true
    }));
  }
}
