import { Component, OnInit } from '@angular/core';
import { SettingsService } from '../../services/settings.service';
import { AuthService } from '../../services/auth.service';
import { TwitchOAuthService } from '../../services/twitch-oauth.service';
import { AppSettings } from '../../models/app-settings.model';
import { NotificationService } from '../../services/notification.service';

@Component({
  selector: 'app-settings',
  templateUrl: './settings.component.html',
  styleUrls: ['./settings.component.scss']
})
export class SettingsComponent implements OnInit {
  settings: AppSettings;
  isAuthenticated: boolean = false;
  userInfo: any = null;
  showAuthSection: boolean = false;

  constructor(
    private settingsService: SettingsService,
    private authService: AuthService,
    private oauthService: TwitchOAuthService,
    private notificationService: NotificationService
  ) {
    this.settings = this.settingsService.getSettings();
  }

  async ngOnInit(): Promise<void> {
    await this.checkAuthStatus();
    // Подписка на изменения настроек
    this.settingsService.settingsChanged$.subscribe((settings: AppSettings) => {
      this.settings = settings;
    });
  }

  async checkAuthStatus(): Promise<void> {
    const settings = this.settingsService.getSettings();
    if (settings.twitchOAuthToken && settings.twitchClientId) {
      try {
        this.userInfo = await this.oauthService.getUserInfo(settings.twitchOAuthToken, settings.twitchClientId);
        this.isAuthenticated = true;
      } catch (error) {
        console.error('Error checking auth status:', error);
        this.isAuthenticated = false;
        this.userInfo = null;
      }
    } else {
      this.isAuthenticated = false;
      this.userInfo = null;
    }
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

  onAuthSuccess(): void {
    this.checkAuthStatus();
    this.showAuthSection = false;
  }
}

