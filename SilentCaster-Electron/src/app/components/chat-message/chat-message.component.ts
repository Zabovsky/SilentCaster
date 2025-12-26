import { Component, Input, Output, EventEmitter, ChangeDetectionStrategy, ChangeDetectorRef } from '@angular/core';
import { ChatMessage } from '../../models/chat-message.model';
import { ModerationService } from '../../services/moderation.service';

@Component({
  selector: 'app-chat-message',
  templateUrl: './chat-message.component.html',
  styleUrls: ['./chat-message.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ChatMessageComponent {
  @Input() message!: ChatMessage;
  @Input() isModerator: boolean = false;
  @Input() channel: string = '';
  private _activeModerationMenu: string | null = null;
  
  @Input() 
  set activeModerationMenu(value: string | null) {
    this._activeModerationMenu = value;
    this.showModerationMenu = value === this.message.timestamp.toString();
    this.cdr.markForCheck();
  }
  
  get activeModerationMenu(): string | null {
    return this._activeModerationMenu;
  }
  @Output() messageDeleted = new EventEmitter<string>();
  @Output() quickTimeout = new EventEmitter<{username: string; duration: number}>();
  @Output() quickBan = new EventEmitter<string>();
  @Output() moderationMenuToggled = new EventEmitter<string | null>();
  @Output() showUserProfile = new EventEmitter<string>();
  @Output() toggleUserHighlight = new EventEmitter<string>();
  
  showModerationMenu: boolean = false;
  showTimeoutDialog: boolean = false;
  timeoutDuration: number = 600; // 10 minutes default
  timeoutReason: string = '';

  constructor(
    private moderationService: ModerationService,
    private cdr: ChangeDetectorRef
  ) {}

  toggleModerationMenu(event: Event): void {
    event.stopPropagation();
    if (this.isModerator) {
      const messageId = this.message.timestamp.toString();
      if (this.activeModerationMenu === messageId) {
        // Закрываем меню, если оно уже открыто
        this.showModerationMenu = false;
        this.moderationMenuToggled.emit(null);
      } else {
        // Открываем меню и закрываем другие
        this.showModerationMenu = true;
        this.moderationMenuToggled.emit(messageId);
      }
      this.cdr.markForCheck();
    }
  }

  onDoubleClick(): void {
    this.showUserProfile.emit(this.message.username);
  }

  toggleHighlight(): void {
    this.toggleUserHighlight.emit(this.message.username);
  }

  async deleteMessage(): Promise<void> {
    try {
      const result = await this.moderationService.deleteMessage(
        this.channel,
        this.message.timestamp.toString(),
        this.message.username
      );
      
      if (result.success) {
        this.messageDeleted.emit(this.message.timestamp.toString());
        this.showModerationMenu = false;
      }
    } catch (error) {
      console.error('Error deleting message:', error);
    }
  }

  async timeoutUser(): Promise<void> {
    try {
      const result = await this.moderationService.timeoutUser(
        this.channel,
        this.message.username,
        this.timeoutDuration,
        this.timeoutReason || undefined
      );
      
      if (result.success) {
        this.showTimeoutDialog = false;
        this.showModerationMenu = false;
        this.timeoutReason = '';
      }
    } catch (error) {
      console.error('Error timing out user:', error);
    }
  }

  async banUser(): Promise<void> {
    try {
      const result = await this.moderationService.banUser(
        this.channel,
        this.message.username,
        this.timeoutReason || undefined
      );
      
      if (result.success) {
        this.showModerationMenu = false;
        this.timeoutReason = '';
      }
    } catch (error) {
      console.error('Error banning user:', error);
    }
  }

  openTimeoutDialog(): void {
    this.showTimeoutDialog = true;
    this.timeoutDuration = 600;
    this.timeoutReason = '';
  }

  closeTimeoutDialog(): void {
    this.showTimeoutDialog = false;
    this.timeoutReason = '';
  }

  // Быстрые действия
  quickDelete(): void {
    this.deleteMessage();
  }

  quickTimeout1min(): void {
    this.timeoutDuration = 60;
    this.timeoutUser();
  }

  quickTimeout5min(): void {
    this.timeoutDuration = 300;
    this.timeoutUser();
  }

  quickTimeout10min(): void {
    this.timeoutDuration = 600;
    this.timeoutUser();
  }

  quickBanUser(): void {
    this.banUser();
  }

  get violationReason(): string {
    if (this.message.violation?.hasViolation && 
        this.message.violation?.reason && 
        this.message.violation.reason.length > 0) {
      return this.message.violation.reason[0];
    }
    return '';
  }

  get hasViolation(): boolean {
    return !!(this.message.violation?.hasViolation && 
              this.message.violation?.reason && 
              this.message.violation.reason.length > 0);
  }
}

