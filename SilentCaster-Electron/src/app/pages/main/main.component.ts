import { Component, OnInit, OnDestroy, ViewChild, ElementRef, AfterViewChecked, ChangeDetectionStrategy, ChangeDetectorRef } from '@angular/core';
import { Subscription } from 'rxjs';
import { TwitchService } from '../../services/twitch.service';
import { TtsService } from '../../services/tts.service';
import { SettingsService } from '../../services/settings.service';
import { ResponseService } from '../../services/response.service';
import { ModerationService } from '../../services/moderation.service';
import { AuthService } from '../../services/auth.service';
import { ForbiddenWordsService } from '../../services/forbidden-words.service';
import { TwitchOAuthService } from '../../services/twitch-oauth.service';
import { ThemeService, Theme } from '../../services/theme.service';
import { ChatMessage } from '../../models/chat-message.model';
import { AppSettings } from '../../models/app-settings.model';

@Component({
  selector: 'app-main',
  templateUrl: './main.component.html',
  styleUrls: ['./main.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class MainComponent implements OnInit, OnDestroy, AfterViewChecked {
  @ViewChild('chatContainer') chatContainer!: ElementRef;
  
  channel: string = '';
  connectionStatus: string = 'Отключено';
  chatMessages: ChatMessage[] = [];
  settings: AppSettings;
  isModerator: boolean = false;
  autoScroll: boolean = true;
  private shouldScroll: boolean = false;
  activeModerationMenu: string | null = null;
  showUserProfileDialog: boolean = false;
  selectedUser: string = '';
  highlightedUsers: Set<string> = new Set();
  showVoiceSettings: boolean = false;
  showTwitchAuth: boolean = false;
  showSettings: boolean = false;
  isAuthenticated: boolean = false;
  authenticatedUserInfo: any = null;
  currentTheme: Theme = 'dark';
  chatSortOrder: 'time-asc' | 'time-desc' | 'user-asc' | 'user-desc' = 'time-asc';
  sortedChatMessages: ChatMessage[] = [];
  
  private subscriptions: Subscription[] = [];

  constructor(
    private twitchService: TwitchService,
    private ttsService: TtsService,
    private settingsService: SettingsService,
    private responseService: ResponseService,
    private moderationService: ModerationService,
    private authService: AuthService,
    private forbiddenWordsService: ForbiddenWordsService,
    private oauthService: TwitchOAuthService,
    public themeService: ThemeService,
    private cdr: ChangeDetectorRef
  ) {
    this.settings = this.settingsService.getSettings();
    this.channel = this.settings.lastChannel || '';
    this.currentTheme = this.themeService.getCurrentTheme();
    this.sortedChatMessages = [];
  }

  async ngOnInit(): Promise<void> {
    // Подписка на изменения темы
    this.subscriptions.push(
      this.themeService.currentTheme$.subscribe((theme: Theme) => {
        this.currentTheme = theme;
        this.cdr.markForCheck();
      })
    );
    
    // Инициализация TTS - загружаем голоса
    this.initializeTTS();
    
    // Проверяем статус авторизации
    await this.checkAuthStatus();
    
    // Подписка на изменения настроек
    this.settingsService.settingsChanged$.subscribe(settings => {
      this.settings = settings;
      // Обновляем лимит сообщений при изменении настроек
      const maxMessages = this.settings.maxChatMessages || 100;
      if (this.chatMessages.length > maxMessages) {
        // Сортируем по времени и берем последние N сообщений
        const sortedByTime = this.chatMessages.slice().sort((a, b) => a.timestamp.getTime() - b.timestamp.getTime());
        const recentMessages = sortedByTime.slice(-maxMessages);
        this.chatMessages = recentMessages;
        this.sortChatMessages();
        this.cdr.markForCheck();
      }
    });
    
    // Подписка на сообщения Twitch
    const messageSub = this.twitchService.messageReceived$.subscribe(message => {
      this.onMessageReceived(message);
      // Автоматическая прокрутка при новом сообщении
      if (this.autoScroll) {
        this.shouldScroll = true;
      }
      this.cdr.markForCheck();
    });
    this.subscriptions.push(messageSub);

    // Подписка на изменения статуса подключения
    const statusSub = this.twitchService.connectionStatusChanged$.subscribe(status => {
      this.connectionStatus = status;
      this.cdr.markForCheck();
    });
    this.subscriptions.push(statusSub);

    // Подписка на изменения прав доступа
    const permissionsSub = this.authService.permissions$.subscribe(permissions => {
      this.isModerator = permissions.canModerate;
      this.cdr.markForCheck();
    });
    this.subscriptions.push(permissionsSub);

    // Устанавливаем начальное значение
    this.isModerator = this.authService.isModerator();
    
    // Инициализируем сортировку
    this.sortChatMessages();
  }

  async checkAuthStatus(): Promise<void> {
    const settings = this.settingsService.getSettings();
    if (settings.twitchOAuthToken && settings.twitchClientId) {
      try {
        // Сначала проверяем валидность токена
        const isValid = await this.oauthService.validateToken(settings.twitchOAuthToken);
        if (!isValid) {
          // Токен невалиден, очищаем его
          console.warn('[MainComponent] Токен невалиден, очищаем настройки авторизации');
          await this.clearInvalidToken();
          return;
        }

        this.authenticatedUserInfo = await this.oauthService.getUserInfo(settings.twitchOAuthToken, settings.twitchClientId);
        this.isAuthenticated = true;
      } catch (error: any) {
        // Если ошибка 401 (Unauthorized), токен истек - очищаем его
        if (error.message && error.message.includes('401') || error.message.includes('невалиден') || error.message.includes('истек')) {
          console.warn('[MainComponent] Токен истек или невалиден, очищаем настройки авторизации');
          await this.clearInvalidToken();
        } else {
          // Другие ошибки логируем только один раз
          console.error('[MainComponent] Error checking auth status:', error);
        }
        this.isAuthenticated = false;
        this.authenticatedUserInfo = null;
      }
    } else {
      this.isAuthenticated = false;
      this.authenticatedUserInfo = null;
    }
  }

  private async clearInvalidToken(): Promise<void> {
    const settings = this.settingsService.getSettings();
    await this.settingsService.updateSettings({
      ...settings,
      twitchOAuthToken: '',
      twitchClientId: '',
      twitchChannelId: ''
    });
    this.isAuthenticated = false;
    this.authenticatedUserInfo = null;
  }

  onAuthSuccess(): void {
    this.checkAuthStatus();
  }

  ngOnDestroy(): void {
    this.subscriptions.forEach(sub => sub.unsubscribe());
  }

  async connect(): Promise<void> {
    if (!this.channel.trim()) {
      alert('Введите название канала');
      return;
    }

    // Проверка доступности Electron API перед подключением
    if (!window.electronAPI) {
      const isElectron = navigator.userAgent.includes('Electron');
      let errorMsg = 'Electron API недоступен.\n\n';
      
      if (!isElectron) {
        errorMsg += '⚠️ Приложение запущено в браузере, а не в Electron!\n\n';
        errorMsg += 'Для работы приложения:\n';
        errorMsg += '1. Закройте браузер\n';
        errorMsg += '2. Запустите: npm run electron:dev\n';
        errorMsg += 'Или: npm start (терминал 1) + npm run electron (терминал 2)';
      } else {
        errorMsg += 'Electron обнаружен, но API недоступен.\n';
        errorMsg += 'Проверьте консоль DevTools для подробностей.';
      }
      
      alert(errorMsg);
      console.error('[MainComponent] Electron API check failed:');
      console.error('[MainComponent] window.electronAPI:', window.electronAPI);
      console.error('[MainComponent] typeof window.electronAPI:', typeof window.electronAPI);
      console.error('[MainComponent] Is Electron:', isElectron);
      console.error('[MainComponent] User Agent:', navigator.userAgent);
      return;
    }

    try {
      // Сохраняем канал в настройках
      this.settingsService.updateSettings({ lastChannel: this.channel });
      
      // Обновляем роль пользователя для канала
      await this.authService.updateRoleForChannel(this.channel);
      
      // Сбрасываем чат и включаем автоскролл при подключении
      this.chatMessages = [];
      this.sortedChatMessages = [];
      this.autoScroll = true;
      this.shouldScroll = true;
      this.cdr.markForCheck();
      
      // Подключаемся (с авторизацией, если есть токен)
      const settings = await this.settingsService.getSettings();
      if (settings.twitchOAuthToken && settings.twitchClientId) {
        // Получаем информацию о пользователе для username
        try {
          const userInfo = await this.oauthService.getUserInfo(settings.twitchOAuthToken, settings.twitchClientId);
          if (!userInfo || !userInfo.login) {
            throw new Error('Не удалось получить информацию о пользователе');
          }
          // Подключение с авторизацией
          await this.twitchService.connectWithAuth(userInfo.login, settings.twitchOAuthToken, this.channel);
        } catch (error: any) {
          console.error('Error getting user info, connecting anonymously:', error);
          console.error('Error details:', {
            message: error.message,
            stack: error.stack,
            token: settings.twitchOAuthToken ? 'present' : 'missing',
            clientId: settings.twitchClientId ? 'present' : 'missing'
          });
          // Если не удалось получить информацию, подключаемся анонимно
          console.log('Falling back to anonymous connection');
          await this.twitchService.connectAnonymously(this.channel);
        }
      } else {
        // Анонимное подключение (только просмотр)
        await this.twitchService.connectAnonymously(this.channel);
      }
    } catch (error: any) {
      console.error('Error connecting:', error);
      alert(`Ошибка подключения: ${error.message}`);
    }
  }

  async disconnect(): Promise<void> {
    try {
      await this.twitchService.disconnect();
    } catch (error: any) {
      console.error('Error disconnecting:', error);
    }
  }

  ngAfterViewChecked(): void {
    if (this.shouldScroll && this.autoScroll) {
      this.scrollToBottom();
      this.shouldScroll = false;
    }
  }

  trackByTimestamp(index: number, message: ChatMessage): string {
    return `${message.timestamp.getTime()}-${message.username}`;
  }

  sortChatMessages(): void {
    if (this.chatMessages.length === 0) {
      this.sortedChatMessages = [];
      this.cdr.markForCheck();
      return;
    }
    
    // Используем более эффективную сортировку
    const messages = this.chatMessages.slice(); // Более эффективно чем [...]
    
    switch (this.chatSortOrder) {
      case 'time-asc':
        messages.sort((a, b) => a.timestamp.getTime() - b.timestamp.getTime());
        break;
      case 'time-desc':
        messages.sort((a, b) => b.timestamp.getTime() - a.timestamp.getTime());
        break;
      case 'user-asc':
        messages.sort((a, b) => {
          const nameCompare = a.username.localeCompare(b.username, 'ru');
          if (nameCompare !== 0) return nameCompare;
          return a.timestamp.getTime() - b.timestamp.getTime();
        });
        break;
      case 'user-desc':
        messages.sort((a, b) => {
          const nameCompare = b.username.localeCompare(a.username, 'ru');
          if (nameCompare !== 0) return nameCompare;
          return a.timestamp.getTime() - b.timestamp.getTime();
        });
        break;
    }
    
    this.sortedChatMessages = messages;
    this.cdr.markForCheck();
  }

  onSortOrderChange(): void {
    this.sortChatMessages();
  }

  onMessageDeleted(messageId: string): void {
    this.chatMessages = this.chatMessages.filter(
      msg => msg.timestamp.toString() !== messageId
    );
    this.sortChatMessages();
    this.cdr.markForCheck();
  }

  onModerationMenuToggled(messageId: string | null): void {
    this.activeModerationMenu = messageId;
  }

  showUserProfile(username: string): void {
    this.selectedUser = username;
    this.showUserProfileDialog = true;
  }

  closeUserProfile(): void {
    this.showUserProfileDialog = false;
    this.selectedUser = '';
  }

  toggleUserHighlight(username: string): void {
    if (this.highlightedUsers.has(username)) {
      this.highlightedUsers.delete(username);
    } else {
      this.highlightedUsers.add(username);
    }
    // Обновляем сообщения
    this.chatMessages.forEach(msg => {
      if (msg.username === username) {
        msg.isHighlighted = this.highlightedUsers.has(username);
      }
    });
  }

  saveVoiceSettings(): void {
    // Обновляем настройки в сервисе
    this.settingsService.updateSettings(this.settings);
    // Обновляем настройки TTS
    this.ttsService.updateSettings(this.settings.voiceSettings);
    console.log('[Voice] Настройки озвучки сохранены:', {
      enableChatVoice: this.settings.enableChatVoice,
      chatTriggerSymbol: this.settings.chatTriggerSymbol,
      voiceOnlyForSubscribers: this.settings.voiceOnlyForSubscribers,
      voiceOnlyForVips: this.settings.voiceOnlyForVips,
      voiceOnlyForModerators: this.settings.voiceOnlyForModerators,
      voiceOnlyForFollowers: this.settings.voiceOnlyForFollowers
    });
  }


  async clearChat(): Promise<void> {
    if (confirm('Вы уверены, что хотите очистить весь чат?')) {
      await this.moderationService.clearChat(this.channel);
      this.chatMessages = [];
      this.sortedChatMessages = [];
      this.cdr.markForCheck();
    }
  }

  toggleAutoScroll(): void {
    this.autoScroll = !this.autoScroll;
    if (this.autoScroll) {
      this.scrollToBottom();
    }
  }

  private scrollToBottom(): void {
    if (this.chatContainer && this.autoScroll) {
      try {
        const element = this.chatContainer.nativeElement;
        // Используем requestAnimationFrame для плавной прокрутки
        requestAnimationFrame(() => {
          element.scrollTop = element.scrollHeight;
        });
      } catch (error) {
        console.error('Error scrolling to bottom:', error);
      }
    }
  }

  onChatScroll(): void {
    // Проверяем, находится ли пользователь внизу чата
    if (this.chatContainer) {
      const element = this.chatContainer.nativeElement;
      const threshold = 50; // 50px запас
      const isAtBottom = element.scrollHeight - element.scrollTop <= element.clientHeight + threshold;
      this.autoScroll = isAtBottom;
    }
  }

  private onMessageReceived(message: ChatMessage): void {
    // Проверяем, отмечен ли пользователь
    if (this.highlightedUsers.has(message.username)) {
      message.isHighlighted = true;
    }
    
    // Проверяем сообщение на запрещенные слова (включая банворды Twitch)
    const twitchBannedWords = this.settings.twitchBannedWords || [];
    const forbiddenCheck = this.forbiddenWordsService.checkMessage(message.message, twitchBannedWords);
    
    // Проверяем сообщение на другие нарушения
    const checkResult = this.moderationService.checkMessage(message, this.forbiddenWordsService.getForbiddenWords());
    
    // Добавляем информацию о нарушениях в сообщение
    if (forbiddenCheck.hasViolation || checkResult.isViolation) {
      message.violation = {
        hasViolation: true,
        severity: forbiddenCheck.hasViolation ? 'high' : checkResult.severity,
        reason: [
          ...(forbiddenCheck.hasViolation ? [`Запрещенные слова: ${forbiddenCheck.foundWords.join(', ')}`] : []),
          ...(checkResult.reason ? [checkResult.reason] : [])
        ],
        forbiddenWords: forbiddenCheck.foundWords
      };
      
      // Если нарушение высокого уровня, автоматически удаляем
      if (message.violation.severity === 'high') {
        this.moderationService.deleteMessage(
          this.channel,
          message.timestamp.toString(),
          message.username
        ).then(() => {
          // Сообщение будет удалено через onMessageDeleted
        });
        return;
      }
    }

    // Добавляем сообщение в чат
    this.chatMessages.push(message);
    this.shouldScroll = true;
    
    // Ограничиваем количество сообщений (удаляем старые сообщения)
    const maxMessages = this.settings.maxChatMessages || 100;
    if (this.chatMessages.length > maxMessages) {
      // Удаляем самое старое сообщение (первое в отсортированном списке по времени)
      const sortedByTime = this.chatMessages.slice().sort((a, b) => a.timestamp.getTime() - b.timestamp.getTime());
      const oldestMessage = sortedByTime[0];
      const oldestIndex = this.chatMessages.findIndex(msg => 
        msg.timestamp.getTime() === oldestMessage.timestamp.getTime() && msg.username === oldestMessage.username
      );
      if (oldestIndex !== -1) {
        this.chatMessages.splice(oldestIndex, 1);
      }
    }
    
    // Обновляем отсортированный список
    this.sortChatMessages();

    // Проверяем, нужно ли озвучивать сообщение
    if (this.shouldVoiceMessage(message)) {
      this.voiceMessage(message);
    }

    // Проверяем быстрые ответы
    this.checkQuickResponses(message);
  }

  private shouldVoiceMessage(message: ChatMessage): boolean {
    // Обновляем настройки перед проверкой (на случай если они изменились)
    const currentSettings = this.settingsService.getSettings();
    
    if (!currentSettings.enableChatVoice) {
      console.log('[Voice] Озвучка отключена');
      return false;
    }

    // Проверка на триггер символ (только если символ установлен и не пустой)
    if (currentSettings.chatTriggerSymbol && currentSettings.chatTriggerSymbol.trim() !== '') {
      if (!message.message.includes(currentSettings.chatTriggerSymbol)) {
        console.log('[Voice] Сообщение не содержит триггер символ:', currentSettings.chatTriggerSymbol);
        return false;
      }
    }

    // Проверка прав доступа (если хотя бы одна галочка установлена)
    const hasAnyFilter = currentSettings.voiceOnlyForSubscribers || 
                         currentSettings.voiceOnlyForVips || 
                         currentSettings.voiceOnlyForModerators || 
                         currentSettings.voiceOnlyForFollowers;

    if (hasAnyFilter) {
      // Если установлены фильтры, проверяем соответствие
      if (currentSettings.voiceOnlyForSubscribers && !message.isSubscriber) {
        console.log('[Voice] Сообщение не от подписчика');
        return false;
      }
      if (currentSettings.voiceOnlyForVips && !message.isVip) {
        console.log('[Voice] Сообщение не от VIP');
        return false;
      }
      if (currentSettings.voiceOnlyForModerators && !message.isModerator) {
        console.log('[Voice] Сообщение не от модератора');
        return false;
      }
      if (currentSettings.voiceOnlyForFollowers && !message.isFollower) {
        console.log('[Voice] Сообщение не от фолловера');
        return false;
      }
    }

    console.log('[Voice] Сообщение будет озвучено:', message.message);
    return true;
  }

  private async voiceMessage(message: ChatMessage): Promise<void> {
    try {
      await this.ttsService.speak(message.message, message.username, 'chat');
    } catch (error) {
      console.error('Error voicing message:', error);
    }
  }

  private checkQuickResponses(message: ChatMessage): void {
    // Получаем ответы для сообщения
    const responses = this.responseService.getResponsesForMessage(message.message);
    
    if (responses.length > 0) {
      // Выбираем случайный ответ
      const randomResponse = responses[Math.floor(Math.random() * responses.length)];
      
      // Заменяем плейсхолдеры
      const processedResponse = randomResponse.replace(/{username}/g, message.username);
      
      // Озвучиваем ответ
      this.ttsService.speak(processedResponse, message.username, 'quick').catch(err => {
        console.error('Error voicing response:', err);
      });
    }
  }

  async testVoice(): Promise<void> {
    await this.ttsService.speakTest();
  }

  private initializeTTS(): void {
    // Инициализируем TTS сервис - загружаем доступные голоса
    try {
      // Обновляем настройки TTS из сохраненных настроек
      this.ttsService.updateSettings(this.settings.voiceSettings);
      
      // Ждем загрузки голосов (Web Speech API может загружать голоса асинхронно)
      if ('speechSynthesis' in window) {
        const voices = window.speechSynthesis.getVoices();
        if (voices.length === 0) {
          // Если голоса еще не загружены, ждем события 'voiceschanged'
          window.speechSynthesis.onvoiceschanged = () => {
            console.log('[TTS] Голоса загружены:', window.speechSynthesis.getVoices().length);
            this.ttsService.updateSettings(this.settings.voiceSettings);
          };
        } else {
          console.log('[TTS] Голоса уже загружены:', voices.length);
        }
      }
    } catch (error) {
      console.error('[TTS] Ошибка инициализации TTS:', error);
    }
  }

  get isConnected(): boolean {
    return this.twitchService.getIsConnected();
  }

  async onAuthSuccessFromTwitchAuth(event: { token: string; userInfo: any }): Promise<void> {
    this.showTwitchAuth = false;
    // Обновляем статус авторизации
    await this.checkAuthStatus();
    // Обновляем роль пользователя
    if (this.channel) {
      await this.authService.updateRoleForChannel(this.channel);
    }
    // Если подключены, переподключаемся с новым токеном
    if (this.isConnected) {
      await this.disconnect();
      await this.connect();
    }
  }

  onThemeChange(theme: Theme): void {
    this.themeService.setTheme(theme);
    this.currentTheme = theme;
  }
}

