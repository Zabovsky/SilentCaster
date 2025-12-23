import { Injectable } from '@angular/core';
import { Subject, Observable } from 'rxjs';
import { ChatMessage, ChatMessageImpl } from '../models/chat-message.model';

declare const tmi: any;

@Injectable({
  providedIn: 'root'
})
export class TwitchService {
  private client: any = null;
  private messageSubject = new Subject<ChatMessage>();
  private connectionStatusSubject = new Subject<string>();

  public messageReceived$: Observable<ChatMessage> = this.messageSubject.asObservable();
  public connectionStatusChanged$: Observable<string> = this.connectionStatusSubject.asObservable();

  private isConnected: boolean = false;

  constructor() {
    // Инициализация будет через Electron main process
  }

  async connect(channel: string, oauthToken?: string, username?: string): Promise<void> {
    try {
      this.connectionStatusSubject.next('Подключение...');

      // Используем Electron IPC для подключения
      if (window.electronAPI && (window.electronAPI as any).connectTwitch) {
        const result = await (window.electronAPI as any).connectTwitch({ 
          channel, 
          anonymous: !oauthToken,
          username: username,
          oauthToken: oauthToken
        });
        
        if (result.success) {
          this.isConnected = true;
          this.connectionStatusSubject.next('Подключено');
          
          // Настраиваем слушатели событий от main process
          this.setupElectronEventListeners();
        } else {
          throw new Error(result.message || 'Ошибка подключения');
        }
        return;
      }

      throw new Error('Electron API недоступен');
    } catch (error: any) {
      this.isConnected = false;
      this.connectionStatusSubject.next('Ошибка подключения');
      throw error;
    }
  }

  async connectAnonymously(channel: string): Promise<void> {
    return this.connect(channel);
  }

  async connectWithAuth(username: string, oauthToken: string, channel: string): Promise<void> {
    return this.connect(channel, oauthToken, username);
  }

  private setupClientEvents(): void {
    if (!this.client) return;

    this.client.on('message', (channel: string, tags: any, message: string, self: boolean) => {
      if (self) return;

      const chatMessage: ChatMessage = new ChatMessageImpl();
      chatMessage.username = tags['display-name'] || tags.username || 'Unknown';
      chatMessage.message = message;
      chatMessage.timestamp = new Date();
      chatMessage.isSubscriber = tags.subscriber === true;
      chatMessage.isVip = tags.badges?.vip === '1';
      chatMessage.isModerator = tags.mod === true || tags.badges?.moderator === '1';

      this.messageSubject.next(chatMessage);
    });

    this.client.on('connected', () => {
      this.isConnected = true;
      this.connectionStatusSubject.next('Подключено');
    });

    this.client.on('disconnected', () => {
      this.isConnected = false;
      this.connectionStatusSubject.next('Отключено');
    });
  }

  private setupElectronEventListeners(): void {
    if (!window.electronAPI) return;

    // Слушаем сообщения от main process
    if ((window.electronAPI as any).onTwitchMessage) {
      (window.electronAPI as any).onTwitchMessage((data: any) => {
        const chatMessage: ChatMessage = new ChatMessageImpl();
        chatMessage.username = data.username;
        chatMessage.message = data.message;
        chatMessage.timestamp = new Date(data.timestamp);
        chatMessage.userId = data.userId;
        chatMessage.userColor = data.userColor || '#FFFFFF';
        chatMessage.isSubscriber = data.isSubscriber || false;
        chatMessage.isVip = data.isVip || false;
        chatMessage.isModerator = data.isModerator || false;
        chatMessage.isBroadcaster = data.isBroadcaster || false;
        chatMessage.isFollower = data.isFollower || false;
        chatMessage.avatarUrl = data.avatarUrl || null;

        this.messageSubject.next(chatMessage);
      });
    }

    // Слушаем изменения статуса подключения
    if ((window.electronAPI as any).onTwitchStatus) {
      (window.electronAPI as any).onTwitchStatus((data: any) => {
        if (data.status === 'connected' || data.status === 'joined') {
          this.isConnected = true;
        } else if (data.status === 'disconnected') {
          this.isConnected = false;
        }
        this.connectionStatusSubject.next(data.message || data.status);
      });
    }
  }

  async disconnect(): Promise<void> {
    try {
      if (window.electronAPI && (window.electronAPI as any).disconnectTwitch) {
        const result = await (window.electronAPI as any).disconnectTwitch();
        this.isConnected = false;
        this.connectionStatusSubject.next('Отключено');
        return;
      }

      if (this.client) {
        await this.client.disconnect();
        this.client = null;
        this.isConnected = false;
        this.connectionStatusSubject.next('Отключено');
      }
    } catch (error: any) {
      console.error('Error disconnecting from Twitch:', error);
      this.isConnected = false;
      this.connectionStatusSubject.next('Ошибка отключения');
    }
  }

  async sendMessage(channel: string, message: string): Promise<void> {
    if (window.electronAPI && (window.electronAPI as any).sendMessage) {
      const result = await (window.electronAPI as any).sendMessage(channel, message);
      if (!result.success) {
        throw new Error(result.message || 'Ошибка отправки сообщения');
      }
    } else {
      throw new Error('Electron API not available');
    }
  }

  async deleteMessage(channel: string, messageId: string): Promise<void> {
    if (window.electronAPI && (window.electronAPI as any).deleteMessage) {
      const result = await (window.electronAPI as any).deleteMessage(channel, messageId);
      if (!result.success) {
        throw new Error(result.message || 'Ошибка удаления сообщения');
      }
    } else {
      throw new Error('Electron API not available');
    }
  }

  async timeoutUser(channel: string, username: string, duration: number, reason?: string): Promise<void> {
    if (window.electronAPI && (window.electronAPI as any).timeoutUser) {
      const result = await (window.electronAPI as any).timeoutUser(channel, username, duration, reason);
      if (!result.success) {
        throw new Error(result.message || 'Ошибка таймаута');
      }
    } else {
      throw new Error('Electron API not available');
    }
  }

  async banUser(channel: string, username: string, reason?: string): Promise<void> {
    if (window.electronAPI && (window.electronAPI as any).banUser) {
      const result = await (window.electronAPI as any).banUser(channel, username, reason);
      if (!result.success) {
        throw new Error(result.message || 'Ошибка бана');
      }
    } else {
      throw new Error('Electron API not available');
    }
  }

  async clearChat(channel: string): Promise<void> {
    if (window.electronAPI && (window.electronAPI as any).clearChat) {
      const result = await (window.electronAPI as any).clearChat(channel);
      if (!result.success) {
        throw new Error(result.message || 'Ошибка очистки чата');
      }
    } else {
      throw new Error('Electron API not available');
    }
  }

  async sendWhisper(username: string, message: string): Promise<void> {
    if (window.electronAPI && (window.electronAPI as any).sendWhisper) {
      const result = await (window.electronAPI as any).sendWhisper(username, message);
      if (!result.success) {
        throw new Error(result.message || 'Ошибка отправки шепота');
      }
    } else {
      throw new Error('Electron API not available');
    }
  }

  getIsConnected(): boolean {
    return this.isConnected;
  }
}

