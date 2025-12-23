export interface MessageViolation {
  hasViolation: boolean;
  severity: 'low' | 'medium' | 'high' | null;
  reason: string[];
  forbiddenWords?: string[];
}

export interface ChatMessage {
  username: string;
  message: string;
  timestamp: Date;
  isSubscriber: boolean;
  isVip: boolean;
  isModerator: boolean;
  isBroadcaster: boolean;
  displayText: string;
  violation?: MessageViolation;
  userId?: string;
  userColor?: string;
  isFollower?: boolean;
  followDate?: Date;
  accountCreated?: Date;
  avatarUrl?: string;
  isPrime?: boolean;
  isHighlighted?: boolean; // Для отметки пользователя
}

export class ChatMessageImpl implements ChatMessage {
  username: string = '';
  message: string = '';
  timestamp: Date = new Date();
  isSubscriber: boolean = false;
  isVip: boolean = false;
  isModerator: boolean = false;
  isBroadcaster: boolean = false;

  get displayText(): string {
    const timeStr = this.timestamp.toLocaleTimeString('ru-RU', { hour: '2-digit', minute: '2-digit', second: '2-digit' });
    const badge = this.getUserBadge();
    return `[${timeStr}] ${badge}${this.username}: ${this.message}`;
  }

  private getUserBadge(): string {
    if (this.isBroadcaster) return '👑 ';
    if (this.isModerator) return '🛡️ ';
    if (this.isVip) return '💎 ';
    if (this.isSubscriber) return '⭐ ';
    return '';
  }
}

