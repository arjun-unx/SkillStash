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
import { ActivatedRoute, Router } from '@angular/router';
import { FormControl } from '@angular/forms';
import { debounceTime, distinctUntilChanged } from 'rxjs/operators';
import { AuthService } from '@core/services/auth.service';
import { NotificationService } from '@core/services/notification.service';
import { TrendingService } from '@core/services/trending.service';
import {
  TrendingSkillDto,
  TrendingProviderDto,
  TrendingSortMode,
  TRENDING_ROLE_FILTERS
} from '@core/models/trending.model';

interface RoleGroup {
  role: string;
  items: TrendingSkillDto[];
}

@Component({
  selector: 'ps-trending-browse',
  templateUrl: './trending-browse.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  standalone: false
})
export class TrendingBrowseComponent implements OnInit, AfterViewInit, OnDestroy {
  readonly roleFilters = TRENDING_ROLE_FILTERS;
  readonly sortOptions: { id: TrendingSortMode; label: string }[] = [
    { id: 'trending', label: 'Trending' },
    { id: 'bookmarked', label: 'Most saved' },
    { id: 'used', label: 'Most used' },
    { id: 'latest', label: 'Latest' },
    { id: 'rated', label: 'Highest rated' }
  ];

  provider: TrendingProviderDto | null = null;
  slug = '';

  searchCtrl = new FormControl<string>('', { nonNullable: true });
  sortMode: TrendingSortMode = 'trending';
  selectedRoles = new Set<string>();

  loading = true;
  loadingMore = false;
  items: TrendingSkillDto[] = [];
  grouped: RoleGroup[] = [];
  page = 1;
  hasNext = false;
  totalCount = 0;

  @ViewChild('sentinel', { read: ElementRef }) sentinel?: ElementRef<HTMLElement>;
  private observer?: IntersectionObserver;
  private loadToken = 0;

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly trending: TrendingService,
    readonly auth: AuthService,
    private readonly notify: NotificationService,
    private readonly cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.slug = this.route.snapshot.paramMap.get('slug') ?? '';
    this.trending.providers().subscribe({
      next: list => {
        this.provider = list.find(p => p.slug === this.slug) ?? null;
        if (!this.provider) {
          void this.router.navigate(['/trending']);
          return;
        }
        this.reload();
        this.cdr.markForCheck();
      },
      error: () => void this.router.navigate(['/trending'])
    });

    this.searchCtrl.valueChanges.pipe(debounceTime(280), distinctUntilChanged()).subscribe(() => this.reload());
  }

  ngAfterViewInit(): void {
    queueMicrotask(() => this.bindObserver());
  }

  ngOnDestroy(): void {
    this.observer?.disconnect();
  }

  setSort(mode: TrendingSortMode): void {
    this.sortMode = mode;
    this.reload();
  }

  toggleRole(role: string): void {
    if (this.selectedRoles.has(role)) this.selectedRoles.delete(role);
    else this.selectedRoles.add(role);
    this.reload();
  }

  isRoleOn(role: string): boolean {
    return this.selectedRoles.has(role);
  }

  reload(): void {
    this.loadToken++;
    this.page = 1;
    this.items = [];
    this.hasNext = false;
    this.loading = true;
    document.querySelector('main')?.scrollTo({ top: 0, behavior: 'auto' });
    this.cdr.markForCheck();
    this.fetch(false);
  }

  private fetch(append: boolean): void {
    const token = this.loadToken;
    const role = this.selectedRoles.size === 1 ? [...this.selectedRoles][0] : null;

    this.trending
      .search({
        provider: this.slug,
        role,
        search: this.searchCtrl.value.trim() || null,
        sort: this.sortMode,
        page: this.page,
        pageSize: 15
      })
      .subscribe({
        next: data => {
          if (token !== this.loadToken) return;
          let batch = data.items;
          if (this.selectedRoles.size > 1) {
            batch = batch.filter(p => this.selectedRoles.has(p.roleCategory));
          }
          this.items = append ? [...this.items, ...batch] : [...batch];
          this.hasNext = data.hasNext;
          this.totalCount = data.totalCount;
          this.rebuildGroups();
          this.loading = false;
          this.loadingMore = false;
          this.cdr.markForCheck();
          queueMicrotask(() => this.bindObserver());
        },
        error: () => {
          if (token !== this.loadToken) return;
          this.loading = false;
          this.loadingMore = false;
          this.notify.error('Failed to load skills');
          this.cdr.markForCheck();
        }
      });
  }

  private rebuildGroups(): void {
    const roles = [...new Set(this.items.map(p => p.roleCategory))].sort();
    this.grouped = roles.map(role => ({
      role,
      items: this.items.filter(p => p.roleCategory === role)
    }));
  }

  loadMore(): void {
    if (!this.hasNext || this.loading || this.loadingMore) return;
    this.loadingMore = true;
    this.page++;
    this.cdr.markForCheck();
    this.fetch(true);
  }

  private bindObserver(): void {
    this.observer?.disconnect();
    const el = this.sentinel?.nativeElement;
    const root = document.querySelector('main');
    if (!el || !root || !this.hasNext) return;
    this.observer = new IntersectionObserver(
      entries => {
        if (entries[0]?.isIntersecting) this.loadMore();
      },
      { root, rootMargin: '400px 0px', threshold: 0 }
    );
    this.observer.observe(el);
  }

  trackById(_: number, p: TrendingSkillDto): string {
    return p.id;
  }

  async copy(p: TrendingSkillDto): Promise<void> {
    try {
      await navigator.clipboard.writeText(p.body);
      this.trending.trackUse(p.id).subscribe(r => {
        p.useCount = r.useCount;
        this.cdr.markForCheck();
      });
      this.notify.success('Copied to clipboard');
    } catch {
      this.notify.error('Copy failed');
    }
  }

  useSkill(p: TrendingSkillDto): void {
    void this.copy(p);
  }

  toggleBookmark(p: TrendingSkillDto): void {
    if (!this.auth.isAuthenticated) {
      this.router.navigate(['/auth/login'], { queryParams: { redirect: this.router.url } });
      return;
    }
    this.trending.toggleBookmark(p.id).subscribe(res => {
      p.bookmarkedByCurrentUser = res.bookmarked;
      p.saveCount = res.saveCount;
      this.cdr.markForCheck();
    });
  }

  async share(p: TrendingSkillDto): Promise<void> {
    try {
      if (navigator.share) await navigator.share({ title: p.title, url: p.sourceUrl });
      else {
        await navigator.clipboard.writeText(p.sourceUrl);
        this.notify.success('Source link copied');
      }
    } catch {
      try {
        await navigator.clipboard.writeText(p.sourceUrl);
        this.notify.success('Source link copied');
      } catch {
        this.notify.error('Share failed');
      }
    }
  }
}
