import { Component, Input, OnInit, Output, EventEmitter } from '@angular/core';
import { ChatMessage } from '../../models/chat-message.model';
import { ModerationService } from '../../services/moderation.service';

@Component({
  selector: 'app-user-profile',
  templateUrl: './user-profile.component.html',
  styleUrls: ['./user-profile.component.scss']
})
export class UserProfileComponent implements OnInit {
  @Input() username: string = '';
  @Input() messages: ChatMessage[] = [];
  @Input() channel: string = '';
  @Output() close = new EventEmitter<void>();

  userMessages: ChatMessage[] = [];
  moderationActions: any[] = [];
  userInfo: any = null;

  constructor(private moderationService: ModerationService) {}

  ngOnInit(): void {
    this.loadUserData();
  }

  private loadUserData(): void {
    // Фильтруем сообщения пользователя
    this.userMessages = this.messages.filter(msg => msg.username === this.username);
    
    // Получаем информацию о пользователе из первого сообщения
    if (this.userMessages.length > 0) {
      const firstMessage = this.userMessages[0];
      this.userInfo = {
        username: firstMessage.username,
        userId: firstMessage.userId,
        avatarUrl: firstMessage.avatarUrl,
        userColor: firstMessage.userColor,
        isSubscriber: firstMessage.isSubscriber,
        isVip: firstMessage.isVip,
        isModerator: firstMessage.isModerator,
        isBroadcaster: firstMessage.isBroadcaster,
        isFollower: firstMessage.isFollower,
        isPrime: firstMessage.isPrime,
        accountCreated: firstMessage.accountCreated,
        followDate: firstMessage.followDate,
        totalMessages: this.userMessages.length,
        violations: this.userMessages.filter(m => m.violation?.hasViolation).length
      };
    }
  }

  onClose(): void {
    this.close.emit();
  }

  openTwitchProfile(): void {
    if (this.userInfo && this.userInfo.username) {
      const url = `https://www.twitch.tv/${this.userInfo.username}`;
      if (window.electronAPI && (window.electronAPI as any).openExternal) {
        (window.electronAPI as any).openExternal(url);
      } else {
        window.open(url, '_blank');
      }
    }
  }

  openSubscriptionsViewer(): void {
    if (this.userInfo && this.userInfo.username) {
      // Используем сервис для просмотра подписок (например, twitchtracker.com или streamelements.com)
      const url = `https://www.twitchtracker.com/${this.userInfo.username}/subscribers`;
      if (window.electronAPI && (window.electronAPI as any).openExternal) {
        (window.electronAPI as any).openExternal(url);
      } else {
        window.open(url, '_blank');
      }
    }
  }
}

