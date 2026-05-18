import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { forkJoin } from 'rxjs';
import { BookmarkCollectionDto, PaginatedList, SkillDto } from '@core/models/skill.model';
import { CuratedLocalService, CuratedSavedItem } from '@core/services/curated-local.service';
import { LibraryService } from '@core/services/library.service';
import { NotificationService } from '@core/services/notification.service';
import { SkillService } from '@core/services/skill.service';

@Component({
  selector: 'ps-library',
  templateUrl: './library.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  standalone: false
})
export class LibraryComponent implements OnInit {
  loading = true;
  collections: BookmarkCollectionDto[] = [];
  /** null = all bookmarks */
  filterCollectionId: string | null = null;
  data: PaginatedList<SkillDto> | null = null;
  deviceSaves: CuratedSavedItem[] = [];
  section: 'community' | 'device' = 'community';

  constructor(
    private readonly library: LibraryService,
    private readonly skills: SkillService,
    private readonly curated: CuratedLocalService,
    private readonly notify: NotificationService,
    private readonly cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.refreshAll();
  }

  refreshAll(): void {
    this.loading = true;
    this.cdr.markForCheck();
    forkJoin({
      cols: this.library.listCollections(),
      marks: this.library.bookmarks(1, 60, this.filterCollectionId ?? undefined)
    }).subscribe({
      next: ({ cols, marks }) => {
        this.collections = cols;
        this.data = marks;
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
    this.skills.toggleBookmark(p.id).subscribe(res => {
      p.bookmarkedByCurrentUser = res.bookmarked;
      p.bookmarkCount = res.bookmarkCount;
      if (!res.bookmarked) this.refreshAll();
      this.cdr.markForCheck();
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

  removeDevice(s: CuratedSavedItem): void {
    this.curated.remove(s.id);
    this.deviceSaves = this.curated.list();
    this.notify.info('Removed');
    this.cdr.markForCheck();
  }

  async copyDevice(s: CuratedSavedItem): Promise<void> {
    try {
      await navigator.clipboard.writeText(s.body);
      this.notify.success('Copied');
    } catch {
      this.notify.error('Copy failed');
    }
  }
}
