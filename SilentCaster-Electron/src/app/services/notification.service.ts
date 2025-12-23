import { Injectable } from '@angular/core';
import { Subject, Observable } from 'rxjs';

export interface Notification {
  id: string;
  type: 'success' | 'error' | 'warning' | 'info';
  message: string;
  duration?: number; // milliseconds, 0 = persistent
  timestamp: Date;
}

@Injectable({
  providedIn: 'root'
})
export class NotificationService {
  private notificationSubject = new Subject<Notification>();
  public notifications$: Observable<Notification> = this.notificationSubject.asObservable();

  private notificationIdCounter = 0;

  show(notification: Omit<Notification, 'id' | 'timestamp'>): string {
    const id = `notification-${++this.notificationIdCounter}`;
    const fullNotification: Notification = {
      ...notification,
      id,
      timestamp: new Date(),
      duration: notification.duration ?? 3000
    };

    this.notificationSubject.next(fullNotification);
    return id;
  }

  success(message: string, duration?: number): string {
    return this.show({ type: 'success', message, duration });
  }

  error(message: string, duration?: number): string {
    return this.show({ type: 'error', message, duration: duration ?? 5000 });
  }

  warning(message: string, duration?: number): string {
    return this.show({ type: 'warning', message, duration });
  }

  info(message: string, duration?: number): string {
    return this.show({ type: 'info', message, duration });
  }
}

