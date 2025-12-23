import { Component, OnInit, Output, EventEmitter } from '@angular/core';
import { TwitchOAuthService, DeviceFlowInfo, TwitchUserInfo } from '../../services/twitch-oauth.service';
import { SettingsService } from '../../services/settings.service';
import { NotificationService } from '../../services/notification.service';

@Component({
  selector: 'app-twitch-auth',
  templateUrl: './twitch-auth.component.html',
  styleUrls: ['./twitch-auth.component.scss']
})
export class TwitchAuthComponent implements OnInit {
  @Output() authSuccess = new EventEmitter<{ token: string; userInfo: TwitchUserInfo }>();
  @Output() authCancel = new EventEmitter<void>();

  isAuthenticated: boolean = false;
  isAuthenticating: boolean = false;
  deviceFlowInfo: DeviceFlowInfo | null = null;
  userInfo: TwitchUserInfo | null = null;
  currentToken: string = '';
  clientId: string = '';
  clientSecret: string = '';
  useAdvancedMode: boolean = false;

  constructor(
    private oauthService: TwitchOAuthService,
    private settingsService: SettingsService,
    private notificationService: NotificationService
  ) {}

  ngOnInit(): void {
    this.loadAuthStatus();
  }

  private async loadAuthStatus(): Promise<void> {
    const settings = await this.settingsService.getSettings();
    this.currentToken = settings.twitchOAuthToken || '';
    this.clientId = settings.twitchClientId || '';

    if (this.currentToken) {
      // Проверяем валидность токена
      const isValid = await this.oauthService.validateToken(this.currentToken, this.clientId);
      if (isValid) {
        try {
          this.userInfo = await this.oauthService.getUserInfo(this.currentToken, this.clientId);
          this.isAuthenticated = true;
        } catch (error) {
          console.error('Error loading user info:', error);
          this.currentToken = '';
          await this.saveToken('');
        }
      } else {
        this.currentToken = '';
        await this.saveToken('');
      }
    }
  }

  async startAuth(): Promise<void> {
    try {
      this.isAuthenticating = true;
      this.notificationService.info('Открытие браузера для авторизации...');

      const clientId = this.useAdvancedMode ? this.clientId : undefined;
      const clientSecret = this.useAdvancedMode ? this.clientSecret : undefined;
      // Используем порт 3001 для OAuth callback, чтобы не конфликтовать с Angular (4200)
      const redirectUri = 'http://localhost:3001/auth/callback';
      // Добавлены все необходимые scopes для модерации и проверки статуса
      const scopes = 'channel:read:redemptions user:read:chat user:read:email moderator:manage:banned_users moderator:manage:chat_messages moderator:read:moderators channel:moderate';

      console.log('[TwitchAuth] Starting OAuth flow with redirectUri:', redirectUri);
      console.log('[TwitchAuth] ClientId:', clientId || 'default');

      // Используем OAuth через браузер (если доступно в Electron)
      if (window.electronAPI && (window.electronAPI as any).startOAuthFlow) {
        this.notificationService.info('Ожидание авторизации в браузере...');
        
        const result = await (window.electronAPI as any).startOAuthFlow(clientId, redirectUri, scopes);
        
        console.log('[TwitchAuth] OAuth flow result:', result);
        
        if (result.success && result.code) {
          // Обмениваем код на токен
          this.notificationService.info('Получение токена...');
          const token = await this.oauthService.exchangeCodeForToken(
            result.code,
            redirectUri,
            clientId,
            clientSecret
          );

          if (token) {
            this.currentToken = token;
            await this.saveToken(token);

            // Получаем информацию о пользователе
            const userInfo = await this.oauthService.getUserInfo(token, clientId);
            this.userInfo = userInfo;

            this.isAuthenticated = true;
            this.isAuthenticating = false;

            this.notificationService.success(`✅ Успешная авторизация! Пользователь: ${userInfo.display_name}`);
            this.authSuccess.emit({ token, userInfo });
          } else {
            throw new Error('Не удалось получить токен');
          }
        } else {
          const errorMsg = result.error || 'Ошибка авторизации';
          if (errorMsg.includes('redirect_mismatch') || errorMsg.includes('redirect_uri')) {
            throw new Error(`Ошибка redirect URI. Убедитесь, что в настройках Twitch приложения добавлен redirect URI: http://localhost:3001/auth/callback\n\nИнструкция: https://dev.twitch.tv/console/apps → Ваше приложение → OAuth Redirect URLs → Добавить: http://localhost:3001/auth/callback`);
          }
          throw new Error(errorMsg);
        }
      } else {
        // Fallback: используем Device Flow
        this.notificationService.info('Используется Device Flow...');
        
        const deviceFlowInfo = await this.oauthService.requestDeviceCode(clientId, clientSecret, scopes);
        this.deviceFlowInfo = deviceFlowInfo;

        // Открываем браузер с кодом
        const url = `${deviceFlowInfo.verification_uri}?user_code=${encodeURIComponent(deviceFlowInfo.user_code)}`;
        
        if (window.electronAPI && (window.electronAPI as any).openExternal) {
          (window.electronAPI as any).openExternal(url);
        } else {
          window.open(url, '_blank');
        }

        this.notificationService.info('Откройте ссылку в браузере и авторизуйтесь. Ожидание подтверждения...');

        // Начинаем опрос токена
        const token = await this.oauthService.pollDeviceToken(
          deviceFlowInfo.device_code,
          deviceFlowInfo.interval,
          clientId,
          clientSecret
        );

        if (token) {
          this.currentToken = token;
          await this.saveToken(token);

          const userInfo = await this.oauthService.getUserInfo(token, clientId);
          this.userInfo = userInfo;

          this.isAuthenticated = true;
          this.isAuthenticating = false;
          this.deviceFlowInfo = null;

          this.notificationService.success(`✅ Успешная авторизация! Пользователь: ${userInfo.display_name}`);
          this.authSuccess.emit({ token, userInfo });
        } else {
          throw new Error('Не удалось получить токен');
        }
      }
    } catch (error: any) {
      this.isAuthenticating = false;
      this.deviceFlowInfo = null;
      this.notificationService.error(`❌ Ошибка авторизации: ${error.message}`);
      console.error('Auth error:', error);
    }
  }

  async logout(): Promise<void> {
    this.currentToken = '';
    this.userInfo = null;
    this.isAuthenticated = false;
    await this.saveToken('');
    this.notificationService.info('Выход выполнен');
    this.authCancel.emit();
  }

  cancelAuth(): void {
    this.isAuthenticating = false;
    this.deviceFlowInfo = null;
    this.authCancel.emit();
  }

  private async saveToken(token: string): Promise<void> {
    const settings = await this.settingsService.getSettings();
    settings.twitchOAuthToken = token;
    if (this.useAdvancedMode && this.clientId) {
      settings.twitchClientId = this.clientId;
    }
    await this.settingsService.saveSettings(settings);
  }

  copyUserCode(): void {
    if (this.deviceFlowInfo) {
      navigator.clipboard.writeText(this.deviceFlowInfo.user_code).then(() => {
        this.notificationService.success('Код скопирован в буфер обмена');
      });
    }
  }
}

