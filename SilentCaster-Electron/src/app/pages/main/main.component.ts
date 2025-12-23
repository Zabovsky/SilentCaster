import { Component, OnInit, OnDestroy, ViewChild, ElementRef, AfterViewChecked } from '@angular/core';
import { Subscription } from 'rxjs';
import { TwitchService } from '../../services/twitch.service';
import { TtsService } from '../../services/tts.service';
import { SettingsService } from '../../services/settings.service';
import { ResponseService } from '../../services/response.service';
import { ModerationService } from '../../services/moderation.service';
import { AuthService } from '../../services/auth.service';
import { ForbiddenWordsService } from '../../services/forbidden-words.service';
import { TwitchOAuthService } from '../../services/twitch-oauth.service';
import { ChatMessage } from '../../models/chat-message.model';
import { AppSettings } from '../../models/app-settings.model';

@Component({
  selector: 'app-main',
  templateUrl: './main.component.html',
  styleUrls: ['./main.component.scss']
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
  showForbiddenWordsPanel: boolean = false;
  showUserProfileDialog: boolean = false;
  selectedUser: string = '';
  highlightedUsers: Set<string> = new Set();
  showVoiceSettings: boolean = false;
  showTwitchAuth: boolean = false;
  showTwitchBannedWords: boolean = false;
  showSettings: boolean = false;
  isAuthenticated: boolean = false;
  authenticatedUserInfo: any = null;
  
  private subscriptions: Subscription[] = [];

  constructor(
    private twitchService: TwitchService,
    private ttsService: TtsService,
    private settingsService: SettingsService,
    private responseService: ResponseService,
    private moderationService: ModerationService,
    private authService: AuthService,
    private forbiddenWordsService: ForbiddenWordsService,
    private oauthService: TwitchOAuthService
  ) {
    this.settings = this.settingsService.getSettings();
    this.channel = this.settings.lastChannel || '';
  }

  async ngOnInit(): Promise<void> {
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
        this.chatMessages = this.chatMessages.slice(-maxMessages);
      }
    });
    
    // Подписка на сообщения Twitch
    const messageSub = this.twitchService.messageReceived$.subscribe(message => {
      this.onMessageReceived(message);
      // Автоматическая прокрутка при новом сообщении
      if (this.autoScroll) {
        this.shouldScroll = true;
      }
    });
    this.subscriptions.push(messageSub);

    // Подписка на изменения статуса подключения
    const statusSub = this.twitchService.connectionStatusChanged$.subscribe(status => {
      this.connectionStatus = status;
    });
    this.subscriptions.push(statusSub);

    // Подписка на изменения прав доступа
    const permissionsSub = this.authService.permissions$.subscribe(permissions => {
      this.isModerator = permissions.canModerate;
    });
    this.subscriptions.push(permissionsSub);

    // Устанавливаем начальное значение
    this.isModerator = this.authService.isModerator();
  }

  async checkAuthStatus(): Promise<void> {
    const settings = this.settingsService.getSettings();
    if (settings.twitchOAuthToken && settings.twitchClientId) {
      try {
        this.authenticatedUserInfo = await this.oauthService.getUserInfo(settings.twitchOAuthToken, settings.twitchClientId);
        this.isAuthenticated = true;
      } catch (error) {
        console.error('Error checking auth status:', error);
        this.isAuthenticated = false;
        this.authenticatedUserInfo = null;
      }
    } else {
      this.isAuthenticated = false;
      this.authenticatedUserInfo = null;
    }
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
      this.autoScroll = true;
      this.shouldScroll = true;
      
      // Подключаемся (с авторизацией, если есть токен)
      const settings = await this.settingsService.getSettings();
      if (settings.twitchOAuthToken) {
        // Получаем информацию о пользователе для username
        try {
          const userInfo = await this.oauthService.getUserInfo(settings.twitchOAuthToken, settings.twitchClientId);
          // Подключение с авторизацией
          await this.twitchService.connectWithAuth(userInfo.login, settings.twitchOAuthToken, this.channel);
        } catch (error) {
          console.error('Error getting user info, connecting anonymously:', error);
          // Если не удалось получить информацию, подключаемся анонимно
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
    return message.timestamp.toString();
  }

  onMessageDeleted(messageId: string): void {
    this.chatMessages = this.chatMessages.filter(
      msg => msg.timestamp.toString() !== messageId
    );
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

  twitchBannedWords: string[] = [];
  newTwitchBannedWord: string = '';
  isLoadingBannedWords: boolean = false;

  async loadTwitchBannedWords(): Promise<void> {
    try {
      if (!this.channel) {
        alert('Сначала подключитесь к каналу');
        return;
      }

      this.isLoadingBannedWords = true;
      
      if (window.electronAPI && (window.electronAPI as any).getTwitchBannedWords) {
        console.log('[BannedWords] Загрузка банвордов для канала:', this.channel);
        this.twitchBannedWords = await (window.electronAPI as any).getTwitchBannedWords(this.channel);
        console.log('[BannedWords] Загружено банвордов:', this.twitchBannedWords.length);
        
        if (this.twitchBannedWords.length === 0) {
          console.log('[BannedWords] Список банвордов пуст');
        }
      } else {
        // Fallback: загружаем из настроек
        const settings = await this.settingsService.getSettings();
        this.twitchBannedWords = (settings as any).twitchBannedWords || [];
      }
    } catch (error: any) {
      console.error('[BannedWords] Ошибка загрузки:', error);
      alert(`Ошибка загрузки банвордов: ${error.message || 'Неизвестная ошибка'}\n\nУбедитесь, что вы авторизованы через Twitch и являетесь модератором канала.`);
    } finally {
      this.isLoadingBannedWords = false;
    }
  }

  onTwitchBannedWordsPanelToggle(): void {
    if (this.showTwitchBannedWords && this.twitchBannedWords.length === 0 && !this.isLoadingBannedWords) {
      // Автоматически загружаем при первом открытии
      this.loadTwitchBannedWords();
    }
  }

  async addTwitchBannedWord(): Promise<void> {
    if (!this.newTwitchBannedWord.trim()) {
      return;
    }

    if (!this.channel) {
      alert('Сначала подключитесь к каналу');
      return;
    }

    try {
      const word = this.newTwitchBannedWord.trim();
      
      // Добавляем через API, если доступно
      if (window.electronAPI && (window.electronAPI as any).addTwitchBannedWord) {
        const result = await (window.electronAPI as any).addTwitchBannedWord(this.channel, word);
        if (result.success) {
          // Обновляем список
          await this.loadTwitchBannedWords();
          this.newTwitchBannedWord = '';
        } else {
          alert(`Ошибка добавления банворда: ${result.message || 'Неизвестная ошибка'}`);
        }
      } else {
        // Fallback: добавляем локально
        if (!this.twitchBannedWords.includes(word)) {
          this.twitchBannedWords.push(word);
          await this.saveTwitchBannedWords();
          this.newTwitchBannedWord = '';
        }
      }
    } catch (error: any) {
      console.error('[BannedWords] Ошибка добавления:', error);
      alert(`Ошибка добавления банворда: ${error.message || 'Неизвестная ошибка'}`);
    }
  }

  async removeTwitchBannedWord(word: string): Promise<void> {
    if (!this.channel) {
      alert('Сначала подключитесь к каналу');
      return;
    }

    try {
      // Удаляем через API, если доступно
      if (window.electronAPI && (window.electronAPI as any).removeTwitchBannedWord) {
        const result = await (window.electronAPI as any).removeTwitchBannedWord(this.channel, word);
        if (result.success) {
          // Обновляем список
          await this.loadTwitchBannedWords();
        } else {
          alert(`Ошибка удаления банворда: ${result.message || 'Неизвестная ошибка'}`);
        }
      } else {
        // Fallback: удаляем локально
        this.twitchBannedWords = this.twitchBannedWords.filter(w => w !== word);
        await this.saveTwitchBannedWords();
      }
    } catch (error: any) {
      console.error('[BannedWords] Ошибка удаления:', error);
      alert(`Ошибка удаления банворда: ${error.message || 'Неизвестная ошибка'}`);
    }
  }

  async saveTwitchBannedWords(): Promise<void> {
    if (window.electronAPI && (window.electronAPI as any).saveTwitchBannedWords) {
      await (window.electronAPI as any).saveTwitchBannedWords(this.channel, this.twitchBannedWords);
    } else {
      // Fallback: сохраняем в настройки
      const settings = await this.settingsService.getSettings();
      (settings as any).twitchBannedWords = this.twitchBannedWords;
      await this.settingsService.saveSettings(settings);
    }
  }

  async clearChat(): Promise<void> {
    if (confirm('Вы уверены, что хотите очистить весь чат?')) {
      await this.moderationService.clearChat(this.channel);
      this.chatMessages = [];
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

    // Добавляем сообщение в чат (в конец массива для правильной сортировки)
    this.chatMessages.push(message);
    this.shouldScroll = true;
    
    // Ограничиваем количество сообщений (удаляем старые сообщения с начала)
    const maxMessages = this.settings.maxChatMessages || 100;
    if (this.chatMessages.length > maxMessages) {
      this.chatMessages.shift(); // Удаляем самое старое сообщение
    }
    
    // Обновляем список сообщений (для автоматического обновления)
    this.chatMessages = [...this.chatMessages];

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
}

