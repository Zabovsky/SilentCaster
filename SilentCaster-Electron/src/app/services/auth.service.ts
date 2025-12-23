import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';
import { UserRole, UserPermissions, PermissionManager } from '../models/user-role.model';
import { SettingsService } from './settings.service';
import { TwitchOAuthService } from './twitch-oauth.service';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private currentRoleSubject = new BehaviorSubject<UserRole>(UserRole.VIEWER);
  public currentRole$: Observable<UserRole> = this.currentRoleSubject.asObservable();

  private permissionsSubject = new BehaviorSubject<UserPermissions>(
    PermissionManager.getPermissions(UserRole.VIEWER)
  );
  public permissions$: Observable<UserPermissions> = this.permissionsSubject.asObservable();

  private currentChannel: string = '';
  private currentUserId: string = '';

  constructor(
    private settingsService: SettingsService,
    private oauthService: TwitchOAuthService
  ) {
    // Загружаем роль из настроек или определяем по умолчанию
    this.loadUserRole();
  }

  private async loadUserRole(): Promise<void> {
    const settings = await this.settingsService.getSettings();
    
    // Если нет токена, устанавливаем роль VIEWER
    if (!settings.twitchOAuthToken) {
      this.setRole(UserRole.VIEWER);
      return;
    }

    // Проверяем валидность токена
    const isValid = await this.oauthService.validateToken(settings.twitchOAuthToken, settings.twitchClientId);
    if (!isValid) {
      this.setRole(UserRole.VIEWER);
      return;
    }

    // Роль будет обновлена при подключении к каналу
    this.setRole(UserRole.VIEWER);
  }

  /**
   * Обновляет роль пользователя на основе прав в канале
   */
  async updateRoleForChannel(channel: string): Promise<void> {
    const settings = await this.settingsService.getSettings();
    
    if (!settings.twitchOAuthToken) {
      this.setRole(UserRole.VIEWER);
      return;
    }

    try {
      // Проверяем наличие токена
      if (!settings.twitchOAuthToken) {
        console.warn('[AuthService] Нет токена OAuth, устанавливаем роль VIEWER');
        this.setRole(UserRole.VIEWER);
        return;
      }

      if (!settings.twitchClientId) {
        console.warn('[AuthService] Нет Client ID, устанавливаем роль VIEWER');
        this.setRole(UserRole.VIEWER);
        return;
      }

      // Получаем информацию о пользователе
      const userInfo = await this.oauthService.getUserInfo(
        settings.twitchOAuthToken,
        settings.twitchClientId
      );
      
      if (!userInfo || !userInfo.id) {
        console.error('[AuthService] Не удалось получить информацию о пользователе');
        this.setRole(UserRole.VIEWER);
        return;
      }
      
      this.currentUserId = userInfo.id;
      this.currentChannel = channel;

      // Получаем ID канала
      const channelId = await this.oauthService.getChannelId(
        settings.twitchOAuthToken,
        channel,
        settings.twitchClientId
      );

      if (!channelId) {
        this.setRole(UserRole.VIEWER);
        return;
      }

      // Проверяем, является ли пользователь стримером
      const isBroadcaster = await this.oauthService.isBroadcaster(
        settings.twitchOAuthToken,
        channelId,
        userInfo.id,
        settings.twitchClientId
      );

      if (isBroadcaster) {
        this.setRole(UserRole.BROADCASTER);
        return;
      }

      // Проверяем, является ли пользователь модератором
      const isModerator = await this.oauthService.isModerator(
        settings.twitchOAuthToken,
        channelId,
        userInfo.id,
        settings.twitchClientId
      );

      if (isModerator) {
        this.setRole(UserRole.MODERATOR);
        return;
      }

      // По умолчанию VIEWER
      this.setRole(UserRole.VIEWER);
    } catch (error) {
      console.error('Error updating role for channel:', error);
      this.setRole(UserRole.VIEWER);
    }
  }

  setRole(role: UserRole): void {
    this.currentRoleSubject.next(role);
    const permissions = PermissionManager.getPermissions(role);
    this.permissionsSubject.next(permissions);
    
    // Сохраняем в настройках
    // TODO: Сохранять роль пользователя
  }

  getCurrentRole(): UserRole {
    return this.currentRoleSubject.value;
  }

  getPermissions(): UserPermissions {
    return this.permissionsSubject.value;
  }

  hasPermission(permission: keyof UserPermissions): boolean {
    const permissions = this.getPermissions();
    return permissions[permission];
  }

  isModerator(): boolean {
    return this.hasPermission('canModerate');
  }

  isAdmin(): boolean {
    const role = this.getCurrentRole();
    return role === UserRole.ADMIN || role === UserRole.BROADCASTER;
  }

  // Определение роли на основе данных Twitch
  determineRoleFromTwitch(tags: any): UserRole {
    if (tags.badges?.broadcaster === '1') {
      return UserRole.BROADCASTER;
    }
    if (tags.badges?.moderator === '1' || tags.mod === true) {
      return UserRole.MODERATOR;
    }
    if (tags.badges?.vip === '1') {
      return UserRole.VIP;
    }
    if (tags.subscriber === true || tags.badges?.subscriber) {
      return UserRole.SUBSCRIBER;
    }
    return UserRole.VIEWER;
  }
}

