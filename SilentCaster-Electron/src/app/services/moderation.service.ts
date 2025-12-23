import { Injectable } from '@angular/core';
import { Observable, Subject } from 'rxjs';
import { ModerationAction, ModerationActionType, ModerationResult } from '../models/moderation-action.model';
import { ChatMessage } from '../models/chat-message.model';
import { NotificationService } from './notification.service';

@Injectable({
  providedIn: 'root'
})
export class ModerationService {
  private moderationActionSubject = new Subject<ModerationAction>();
  private moderationResultSubject = new Subject<ModerationResult>();
  
  public moderationAction$: Observable<ModerationAction> = this.moderationActionSubject.asObservable();
  public moderationResult$: Observable<ModerationResult> = this.moderationResultSubject.asObservable();

  constructor(private notificationService: NotificationService) {}

  /**
   * Удалить сообщение из чата
   */
  async deleteMessage(channel: string, messageId: string, username: string): Promise<ModerationResult> {
    try {
      const action: ModerationAction = {
        type: ModerationActionType.DELETE,
        username,
        messageId
      };

      this.moderationActionSubject.next(action);

      // Выполняем действие через Electron IPC или напрямую
      if (window.electronAPI && (window.electronAPI as any).moderateChat) {
        const result = await (window.electronAPI as any).moderateChat(action);
        this.moderationResultSubject.next(result);
        return result;
      }

      // Fallback - просто удаляем из UI
      const result: ModerationResult = {
        success: true,
        message: `Сообщение от ${username} удалено`
      };
      this.moderationResultSubject.next(result);
      this.notificationService.success(result.message);
      return result;
    } catch (error: any) {
      const result: ModerationResult = {
        success: false,
        message: 'Ошибка удаления сообщения',
        error: error.message
      };
      this.moderationResultSubject.next(result);
      this.notificationService.error(result.message);
      return result;
    }
  }

  /**
   * Таймаут пользователя
   */
  async timeoutUser(channel: string, username: string, duration: number = 600, reason?: string): Promise<ModerationResult> {
    try {
      const action: ModerationAction = {
        type: ModerationActionType.TIMEOUT,
        username,
        duration,
        reason
      };

      this.moderationActionSubject.next(action);

      if (window.electronAPI && (window.electronAPI as any).timeoutUser) {
        const result = await (window.electronAPI as any).timeoutUser(channel, username, duration, reason);
        this.moderationResultSubject.next(result);
        return result;
      }

      const result: ModerationResult = {
        success: true,
        message: `${username} получил таймаут на ${duration} секунд`
      };
      this.moderationResultSubject.next(result);
      this.notificationService.success(result.message);
      return result;
    } catch (error: any) {
      const result: ModerationResult = {
        success: false,
        message: 'Ошибка таймаута',
        error: error.message
      };
      this.moderationResultSubject.next(result);
      return result;
    }
  }

  /**
   * Забанить пользователя
   */
  async banUser(channel: string, username: string, reason?: string): Promise<ModerationResult> {
    try {
      const action: ModerationAction = {
        type: ModerationActionType.BAN,
        username,
        reason
      };

      this.moderationActionSubject.next(action);

      if (window.electronAPI && (window.electronAPI as any).banUser) {
        const result = await (window.electronAPI as any).banUser(channel, username, reason);
        this.moderationResultSubject.next(result);
        return result;
      }

      const result: ModerationResult = {
        success: true,
        message: `${username} забанен`
      };
      this.moderationResultSubject.next(result);
      this.notificationService.warning(result.message);
      return result;
    } catch (error: any) {
      const result: ModerationResult = {
        success: false,
        message: 'Ошибка бана',
        error: error.message
      };
      this.moderationResultSubject.next(result);
      return result;
    }
  }

  /**
   * Разбанить пользователя
   */
  async unbanUser(username: string): Promise<ModerationResult> {
    try {
      const action: ModerationAction = {
        type: ModerationActionType.UNBAN,
        username
      };

      this.moderationActionSubject.next(action);

      if (window.electronAPI && (window.electronAPI as any).moderateChat) {
        const result = await (window.electronAPI as any).moderateChat(action);
        this.moderationResultSubject.next(result);
        return result;
      }

      const result: ModerationResult = {
        success: true,
        message: `${username} разбанен`
      };
      this.moderationResultSubject.next(result);
      return result;
    } catch (error: any) {
      const result: ModerationResult = {
        success: false,
        message: 'Ошибка разбана',
        error: error.message
      };
      this.moderationResultSubject.next(result);
      return result;
    }
  }

  /**
   * Очистить весь чат
   */
  async clearChat(channel: string): Promise<ModerationResult> {
    try {
      const action: ModerationAction = {
        type: ModerationActionType.CLEAR,
        username: 'all'
      };

      this.moderationActionSubject.next(action);

      if (window.electronAPI && (window.electronAPI as any).clearChat) {
        const result = await (window.electronAPI as any).clearChat(channel);
        this.moderationResultSubject.next(result);
        return result;
      }

      const result: ModerationResult = {
        success: true,
        message: 'Чат очищен'
      };
      this.moderationResultSubject.next(result);
      this.notificationService.info(result.message);
      return result;
    } catch (error: any) {
      const result: ModerationResult = {
        success: false,
        message: 'Ошибка очистки чата',
        error: error.message
      };
      this.moderationResultSubject.next(result);
      return result;
    }
  }

  /**
   * Отправить приватное сообщение пользователю
   */
  async whisperUser(username: string, message: string): Promise<ModerationResult> {
    try {
      const action: ModerationAction = {
        type: ModerationActionType.WHISPER,
        username,
        reason: message
      };

      this.moderationActionSubject.next(action);

      if (window.electronAPI && (window.electronAPI as any).moderateChat) {
        const result = await (window.electronAPI as any).moderateChat(action);
        this.moderationResultSubject.next(result);
        return result;
      }

      const result: ModerationResult = {
        success: true,
        message: `Сообщение отправлено ${username}`
      };
      this.moderationResultSubject.next(result);
      return result;
    } catch (error: any) {
      const result: ModerationResult = {
        success: false,
        message: 'Ошибка отправки сообщения',
        error: error.message
      };
      this.moderationResultSubject.next(result);
      return result;
    }
  }

  /**
   * Проверка сообщения на нарушение правил
   */
  checkMessage(message: ChatMessage, forbiddenWords: string[] = []): {
    isViolation: boolean;
    reason?: string;
    severity: 'low' | 'medium' | 'high';
  } {
    const messageLower = message.message.toLowerCase();

    // Проверка на запрещенные слова
    for (const word of forbiddenWords) {
      if (messageLower.includes(word.toLowerCase())) {
        return {
          isViolation: true,
          reason: `Запрещенное слово: ${word}`,
          severity: 'high'
        };
      }
    }

    // Проверка на спам (повторяющиеся символы)
    if (/(.)\1{4,}/.test(message.message)) {
      return {
        isViolation: true,
        reason: 'Возможный спам (повторяющиеся символы)',
        severity: 'medium'
      };
    }

    // Проверка на капс (более 70% заглавных букв)
    const capsCount = (message.message.match(/[A-ZА-Я]/g) || []).length;
    const totalLetters = (message.message.match(/[A-Za-zА-Яа-я]/g) || []).length;
    if (totalLetters > 10 && capsCount / totalLetters > 0.7) {
      return {
        isViolation: true,
        reason: 'Слишком много заглавных букв',
        severity: 'low'
      };
    }

    // Проверка на длину сообщения
    if (message.message.length > 500) {
      return {
        isViolation: true,
        reason: 'Сообщение слишком длинное',
        severity: 'low'
      };
    }

    return {
      isViolation: false,
      severity: 'low'
    };
  }
}

