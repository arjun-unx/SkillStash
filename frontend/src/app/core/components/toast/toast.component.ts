import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnDestroy, OnInit } from '@angular/core';
import { Subscription } from 'rxjs';
import { NotificationService, ToastMessage } from '@core/services/notification.service';
import { UI } from '@core/ui/ui-classes';

@Component({
  selector: 'ps-toast-container',
  templateUrl: './toast.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  standalone: false
})
export class ToastContainerComponent implements OnInit, OnDestroy {
  readonly ui = UI;
  toasts: ToastMessage[] = [];
  private sub?: Subscription;

  constructor(
    private readonly notifications: NotificationService,
    private readonly cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.sub = this.notifications.toasts$.subscribe(list => {
      this.toasts = list;
      this.cdr.markForCheck();
    });
  }

  ngOnDestroy(): void {
    this.sub?.unsubscribe();
  }

  dismiss(id: number): void {
    this.notifications.dismiss(id);
  }

  trackById(_: number, toast: ToastMessage): number {
    return toast.id;
  }

  toastClasses(type: ToastMessage['type']): string {
    const base =
      'pointer-events-auto flex w-full max-w-sm items-start gap-3 rounded-xl border px-4 py-3 text-sm shadow-lg backdrop-blur-md';
    if (type === 'success') {
      return `${base} border-emerald-500/30 bg-emerald-950/90 text-emerald-100`;
    }
    if (type === 'error') {
      return `${base} border-rose-500/35 bg-rose-950/90 text-rose-100`;
    }
    return `${base} border-line-strong bg-surface-overlay/95 text-slate-200`;
  }
}
