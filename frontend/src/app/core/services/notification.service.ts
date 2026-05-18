import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';

export type ToastType = 'success' | 'error' | 'info';

export interface ToastMessage {
  id: number;
  type: ToastType;
  message: string;
}

@Injectable({ providedIn: 'root' })
export class NotificationService {
  private readonly _toasts = new BehaviorSubject<ToastMessage[]>([]);
  readonly toasts$ = this._toasts.asObservable();
  private nextId = 0;

  success(message: string): void {
    this.push('success', message, 3200);
  }

  error(message: string): void {
    this.push('error', message, 4800);
  }

  info(message: string): void {
    this.push('info', message, 3200);
  }

  dismiss(id: number): void {
    this._toasts.next(this._toasts.value.filter(t => t.id !== id));
  }

  private push(type: ToastType, message: string, duration: number): void {
    const id = ++this.nextId;
    const toast: ToastMessage = { id, type, message };
    this._toasts.next([...this._toasts.value, toast]);
    setTimeout(() => this.dismiss(id), duration);
  }
}
