import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { TrendingProviderDto } from '@core/models/trending.model';
import { NotificationService } from '@core/services/notification.service';
import { TrendingService } from '@core/services/trending.service';

@Component({
  selector: 'ps-trending-hub',
  templateUrl: './trending-hub.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  standalone: false
})
export class TrendingHubComponent implements OnInit {
  loading = true;
  syncing = false;
  providers: TrendingProviderDto[] = [];

  constructor(
    private readonly trending: TrendingService,
    private readonly notify: NotificationService,
    private readonly cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading = true;
    this.cdr.markForCheck();
    this.trending.providers().subscribe({
      next: list => {
        this.providers = list.filter(p => p.skillCount > 0 || p.slug);
        this.loading = false;
        this.cdr.markForCheck();
      },
      error: () => {
        this.loading = false;
        this.notify.error('Could not load trending providers');
        this.cdr.markForCheck();
      }
    });
  }

  refreshCatalog(): void {
    this.syncing = true;
    this.cdr.markForCheck();
    this.trending.sync().subscribe({
      next: res => {
        this.syncing = false;
        this.notify.success(`Synced ${res.synced} skills from GitHub`);
        this.load();
      },
      error: () => {
        this.syncing = false;
        this.notify.error('Sync failed — check API logs');
        this.cdr.markForCheck();
      }
    });
  }

  tileClass(slug: string): string {
    const map: Record<string, string> = {
      claude:
        'bg-gradient-to-br from-orange-500/25 via-amber-500/10 to-transparent ring-orange-400/20 hover:ring-orange-300/40',
      openai:
        'bg-gradient-to-br from-emerald-500/25 via-teal-500/10 to-transparent ring-emerald-400/25 hover:ring-emerald-300/45',
      gemini: 'bg-gradient-to-br from-sky-500/25 via-blue-500/10 to-transparent ring-sky-400/25 hover:ring-sky-300/45',
      grok: 'bg-gradient-to-br from-slate-400/20 via-slate-500/10 to-transparent ring-slate-400/20 hover:ring-slate-300/40',
      cursor:
        'bg-gradient-to-br from-cyan-500/20 via-violet-500/10 to-transparent ring-cyan-400/25 hover:ring-cyan-300/40',
      community:
        'bg-gradient-to-br from-violet-500/25 via-fuchsia-500/10 to-transparent ring-violet-400/25 hover:ring-violet-300/45'
    };
    return map[slug] ?? map['community'];
  }
}
