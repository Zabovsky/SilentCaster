import { Component, OnInit, OnDestroy } from '@angular/core';
import { SettingsService } from '../../services/settings.service';
import { AuthService } from '../../services/auth.service';
import { TwitchOAuthService } from '../../services/twitch-oauth.service';
import { AppSettings } from '../../models/app-settings.model';
import { NotificationService } from '../../services/notification.service';
import { ForbiddenWordsService } from '../../services/forbidden-words.service';
import { Subscription } from 'rxjs';

@Component({
  selector: 'app-settings',
  templateUrl: './settings.component.html',
  styleUrls: ['./settings.component.scss']
})
export class SettingsComponent implements OnInit, OnDestroy {
  settings: AppSettings;
  isAuthenticated: boolean = false;
  userInfo: any = null;
  showAuthSection: boolean = false;
  
  // Банворды Twitch
  twitchBannedWords: string[] = [];
  newTwitchBannedWord: string = '';
  isLoadingBannedWords: boolean = false;
  currentChannel: string = '';
  
  // Запрещенные слова
  forbiddenWords: string[] = [];
  newForbiddenWord: string = '';
  
  private forbiddenWordsSubscription?: Subscription;

  constructor(
    private settingsService: SettingsService,
    private authService: AuthService,
    private oauthService: TwitchOAuthService,
    private notificationService: NotificationService,
    private forbiddenWordsService: ForbiddenWordsService
  ) {
    this.settings = this.settingsService.getSettings();
    this.currentChannel = this.settings.lastChannel || '';
    // Загружаем банворды из настроек при инициализации
    this.twitchBannedWords = this.settings.twitchBannedWords || [];
    // Загружаем запрещенные слова
    this.forbiddenWords = this.forbiddenWordsService.getForbiddenWords();
  }

  async ngOnInit(): Promise<void> {
    await this.checkAuthStatus();
    // Подписка на изменения настроек
    this.settingsService.settingsChanged$.subscribe((settings: AppSettings) => {
      this.settings = settings;
      // Обновляем список банвордов при изменении настроек
      if (settings.twitchBannedWords) {
        this.twitchBannedWords = settings.twitchBannedWords;
      }
    });
    
    // Загружаем банворды из настроек при инициализации
    if (this.settings.twitchBannedWords && this.settings.twitchBannedWords.length > 0) {
      this.twitchBannedWords = this.settings.twitchBannedWords;
    }
    
    // Подписка на изменения запрещенных слов
    this.forbiddenWordsSubscription = this.forbiddenWordsService.forbiddenWords$.subscribe(words => {
      this.forbiddenWords = words;
    });
  }
  
  ngOnDestroy(): void {
    if (this.forbiddenWordsSubscription) {
      this.forbiddenWordsSubscription.unsubscribe();
    }
  }

  async checkAuthStatus(): Promise<void> {
    const settings = this.settingsService.getSettings();
    if (settings.twitchOAuthToken && settings.twitchClientId) {
      try {
        // Сначала проверяем валидность токена
        const isValid = await this.oauthService.validateToken(settings.twitchOAuthToken);
        if (!isValid) {
          // Токен невалиден, очищаем его
          console.warn('[Settings] Токен невалиден, очищаем настройки авторизации');
          await this.clearInvalidToken();
          return;
        }

        this.userInfo = await this.oauthService.getUserInfo(settings.twitchOAuthToken, settings.twitchClientId);
        this.isAuthenticated = true;
      } catch (error: any) {
        // Если ошибка 401 (Unauthorized), токен истек - очищаем его
        if (error.message && (error.message.includes('401') || error.message.includes('невалиден') || error.message.includes('истек'))) {
          console.warn('[Settings] Токен истек или невалиден, очищаем настройки авторизации');
          await this.clearInvalidToken();
          this.notificationService.warning('Токен авторизации истек. Пожалуйста, авторизуйтесь снова.');
        } else {
          // Другие ошибки логируем только один раз
          console.error('[Settings] Error checking auth status:', error);
        }
        this.isAuthenticated = false;
        this.userInfo = null;
      }
    } else {
      this.isAuthenticated = false;
      this.userInfo = null;
    }
  }

  private async clearInvalidToken(): Promise<void> {
    const currentSettings = this.settingsService.getSettings();
    await this.settingsService.updateSettings({
      ...currentSettings,
      twitchOAuthToken: '',
      twitchClientId: '',
      twitchChannelId: ''
    });
    this.isAuthenticated = false;
    this.userInfo = null;
  }

  saveSettings(): void {
    this.settingsService.saveSettings(this.settings);
    this.notificationService.success('Настройки сохранены');
  }

  async logout(): Promise<void> {
    const currentSettings = this.settingsService.getSettings();
    await this.settingsService.updateSettings({
      ...currentSettings,
      twitchOAuthToken: '',
      twitchClientId: '',
      twitchChannelId: ''
    });
    this.isAuthenticated = false;
    this.userInfo = null;
    this.notificationService.info('Вы вышли из аккаунта Twitch');
  }

  async onAuthSuccess(): Promise<void> {
    // Небольшая задержка для сохранения настроек
    await new Promise(resolve => setTimeout(resolve, 100));
    await this.checkAuthStatus();
    this.showAuthSection = false;
  }

  async loadTwitchBannedWords(): Promise<void> {
    try {
      if (!this.currentChannel) {
        this.notificationService.warning('Сначала укажите канал в настройках подключения');
        return;
      }

      if (!this.isAuthenticated) {
        this.notificationService.warning('Требуется авторизация через Twitch');
        return;
      }

      this.isLoadingBannedWords = true;
      
      if (window.electronAPI && (window.electronAPI as any).getTwitchBannedWords) {
        console.log('[Settings] Загрузка банвордов для канала:', this.currentChannel);
        this.twitchBannedWords = await (window.electronAPI as any).getTwitchBannedWords(this.currentChannel);
        console.log('[Settings] Загружено банвордов:', this.twitchBannedWords.length);
        
        // Сохраняем в настройки
        this.settings.twitchBannedWords = this.twitchBannedWords;
        this.settingsService.saveSettings(this.settings);
        
        if (this.twitchBannedWords.length === 0) {
          this.notificationService.info('Список банвордов Twitch пуст');
        } else {
          this.notificationService.success(`Загружено ${this.twitchBannedWords.length} банвордов из Twitch`);
        }
      } else {
        // Fallback: загружаем из настроек
        this.twitchBannedWords = this.settings.twitchBannedWords || [];
        this.notificationService.info('Загружено из локальных настроек');
      }
    } catch (error: any) {
      console.error('[Settings] Ошибка загрузки банвордов:', error);
      this.notificationService.error(`Ошибка загрузки: ${error.message || 'Неизвестная ошибка'}`);
    } finally {
      this.isLoadingBannedWords = false;
    }
  }

  async addTwitchBannedWord(): Promise<void> {
    if (!this.newTwitchBannedWord.trim()) {
      return;
    }

    if (!this.currentChannel) {
      this.notificationService.warning('Сначала укажите канал в настройках подключения');
      return;
    }

    if (!this.isAuthenticated) {
      this.notificationService.warning('Требуется авторизация через Twitch');
      return;
    }

    try {
      const word = this.newTwitchBannedWord.trim();
      
      // Добавляем через API, если доступно
      if (window.electronAPI && (window.electronAPI as any).addTwitchBannedWord) {
        const result = await (window.electronAPI as any).addTwitchBannedWord(this.currentChannel, word);
        if (result && result.success !== false) {
          // Обновляем список
          await this.loadTwitchBannedWords();
          this.newTwitchBannedWord = '';
          this.notificationService.success(`Банворд "${word}" добавлен в Twitch`);
        } else {
          this.notificationService.error('Не удалось добавить банворд');
        }
      } else {
        // Fallback: добавляем локально
        if (!this.twitchBannedWords.includes(word)) {
          this.twitchBannedWords.push(word);
          this.settings.twitchBannedWords = this.twitchBannedWords;
          this.settingsService.saveSettings(this.settings);
          this.newTwitchBannedWord = '';
          this.notificationService.success(`Банворд "${word}" добавлен локально`);
        } else {
          this.notificationService.warning('Этот банворд уже существует');
        }
      }
    } catch (error: any) {
      console.error('[Settings] Ошибка добавления банворда:', error);
      this.notificationService.error(`Ошибка: ${error.message || 'Неизвестная ошибка'}`);
    }
  }

  async removeTwitchBannedWord(word: string): Promise<void> {
    if (!this.currentChannel) {
      this.notificationService.warning('Сначала укажите канал в настройках подключения');
      return;
    }

    if (!this.isAuthenticated) {
      this.notificationService.warning('Требуется авторизация через Twitch');
      return;
    }

    try {
      // Удаляем через API, если доступно
      if (window.electronAPI && (window.electronAPI as any).removeTwitchBannedWord) {
        const result = await (window.electronAPI as any).removeTwitchBannedWord(this.currentChannel, word);
        if (result && result.success !== false) {
          // Обновляем список
          await this.loadTwitchBannedWords();
          this.notificationService.success(`Банворд "${word}" удален из Twitch`);
        } else {
          this.notificationService.error('Не удалось удалить банворд');
        }
      } else {
        // Fallback: удаляем локально
        this.twitchBannedWords = this.twitchBannedWords.filter(w => w !== word);
        this.settings.twitchBannedWords = this.twitchBannedWords;
        this.settingsService.saveSettings(this.settings);
        this.notificationService.success(`Банворд "${word}" удален локально`);
      }
    } catch (error: any) {
      console.error('[Settings] Ошибка удаления банворда:', error);
      this.notificationService.error(`Ошибка: ${error.message || 'Неизвестная ошибка'}`);
    }
  }

  async addForbiddenWord(): Promise<void> {
    if (!this.newForbiddenWord.trim()) {
      return;
    }

    try {
      const word = this.newForbiddenWord.trim();
      await this.forbiddenWordsService.addForbiddenWord(word);
      this.newForbiddenWord = '';
      this.notificationService.success(`Запрещенное слово "${word}" добавлено`);
    } catch (error: any) {
      console.error('[Settings] Ошибка добавления запрещенного слова:', error);
      this.notificationService.error(`Ошибка: ${error.message || 'Неизвестная ошибка'}`);
    }
  }

  async removeForbiddenWord(word: string): Promise<void> {
    try {
      await this.forbiddenWordsService.removeForbiddenWord(word);
      this.notificationService.success(`Запрещенное слово "${word}" удалено`);
    } catch (error: any) {
      console.error('[Settings] Ошибка удаления запрещенного слова:', error);
      this.notificationService.error(`Ошибка: ${error.message || 'Неизвестная ошибка'}`);
    }
  }
}

