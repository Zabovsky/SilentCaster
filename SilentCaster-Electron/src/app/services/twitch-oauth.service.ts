import { Injectable } from '@angular/core';

export interface DeviceFlowInfo {
  device_code: string;
  user_code: string;
  verification_uri: string;
  expires_in: number;
  interval: number;
}

export interface TwitchUserInfo {
  id: string;
  login: string;
  display_name: string;
  email?: string;
  profile_image_url?: string;
  broadcaster_type?: string;
  description?: string;
}

export interface TwitchModeratorInfo {
  user_id: string;
  user_login: string;
  user_name: string;
}

@Injectable({
  providedIn: 'root'
})
export class TwitchOAuthService {
  private readonly defaultClientId = 'hrufoo6euvbe44l7ja70naqkalufxu';
  private readonly defaultClientSecret = 'irh95udt1uhisvp65e3j8ctmd0zgbq';
  private readonly oauthUrl = 'https://id.twitch.tv/oauth2';
  private readonly apiUrl = 'https://api.twitch.tv/helix';
  private last401Logged: boolean = false;

  constructor() {}

  /**
   * Получение URL для авторизации через браузер (Authorization Code Flow)
   */
  getAuthorizationUrl(clientId?: string, redirectUri: string = 'http://localhost:3001/auth/callback', scopes: string = 'channel:read:redemptions user:read:chat user:read:email moderator:manage:banned_users moderator:manage:chat_messages'): string {
    const cId = clientId || this.defaultClientId;
    const state = Math.random().toString(36).substring(2, 15) + Math.random().toString(36).substring(2, 15);
    
    const params = new URLSearchParams({
      client_id: cId,
      redirect_uri: redirectUri,
      response_type: 'code',
      scope: scopes,
      state: state,
      force_verify: 'true'
    });

    return `${this.oauthUrl}/authorize?${params.toString()}`;
  }

  /**
   * Обмен кода авторизации на токен
   */
  async exchangeCodeForToken(code: string, redirectUri: string = 'http://localhost:3001/auth/callback', clientId?: string, clientSecret?: string): Promise<string> {
    const cId = clientId || this.defaultClientId;
    const cSecret = clientSecret || this.defaultClientSecret;

    const response = await fetch(`${this.oauthUrl}/token`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/x-www-form-urlencoded'
      },
      body: new URLSearchParams({
        client_id: cId,
        client_secret: cSecret,
        code: code,
        grant_type: 'authorization_code',
        redirect_uri: redirectUri
      })
    });

    if (!response.ok) {
      const error = await response.text();
      throw new Error(`Ошибка обмена кода на токен: ${error}`);
    }

    const data = await response.json();
    return data.access_token;
  }

  /**
   * Запрос Device Code для авторизации (старый метод, оставлен для совместимости)
   */
  async requestDeviceCode(clientId?: string, clientSecret?: string, scopes: string = 'channel:read:redemptions user:read:chat user:read:email moderator:manage:banned_users moderator:manage:chat_messages'): Promise<DeviceFlowInfo> {
    const cId = clientId || this.defaultClientId;
    const cSecret = clientSecret || this.defaultClientSecret;

    const response = await fetch(`${this.oauthUrl}/device`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/x-www-form-urlencoded'
      },
      body: new URLSearchParams({
        client_id: cId,
        client_secret: cSecret,
        scopes: scopes
      })
    });

    if (!response.ok) {
      const error = await response.text();
      throw new Error(`Ошибка запроса Device Code: ${error}`);
    }

    const data = await response.json();
    return {
      device_code: data.device_code,
      user_code: data.user_code,
      verification_uri: data.verification_uri,
      expires_in: data.expires_in,
      interval: data.interval
    };
  }

  /**
   * Опрос токена авторизации
   */
  async pollDeviceToken(deviceCode: string, interval: number, clientId?: string, clientSecret?: string): Promise<string | null> {
    const cId = clientId || this.defaultClientId;
    const cSecret = clientSecret || this.defaultClientSecret;

    return new Promise((resolve, reject) => {
      const poll = async () => {
        try {
          const response = await fetch(`${this.oauthUrl}/token`, {
            method: 'POST',
            headers: {
              'Content-Type': 'application/x-www-form-urlencoded'
            },
            body: new URLSearchParams({
              client_id: cId,
              client_secret: cSecret,
              device_code: deviceCode,
              grant_type: 'urn:ietf:params:oauth:grant-type:device_code'
            })
          });

          const data = await response.json();

          if (data.access_token) {
            resolve(data.access_token);
            return;
          }

          if (data.error) {
            if (data.error === 'authorization_pending') {
              // Продолжаем опрос
              setTimeout(poll, interval * 1000);
            } else if (data.error === 'slow_down') {
              // Увеличиваем интервал
              setTimeout(poll, (interval + 5) * 1000);
            } else {
              reject(new Error(`Ошибка авторизации: ${data.error_description || data.error}`));
            }
          } else {
            // Продолжаем опрос
            setTimeout(poll, interval * 1000);
          }
        } catch (error: any) {
          reject(error);
        }
      };

      // Начинаем опрос
      setTimeout(poll, interval * 1000);
    });
  }

  /**
   * Получение информации о пользователе
   */
  async getUserInfo(accessToken: string, clientId?: string): Promise<TwitchUserInfo> {
    const cId = clientId || this.defaultClientId;

    if (!accessToken) {
      throw new Error('Токен доступа не предоставлен');
    }

    if (!cId) {
      throw new Error('Client ID не предоставлен');
    }

    const response = await fetch(`${this.apiUrl}/users`, {
      method: 'GET',
      headers: {
        'Authorization': `Bearer ${accessToken}`,
        'Client-Id': cId
      }
    });

    if (response.status === 401) {
      const errorText = await response.text();
      // Логируем только один раз, чтобы не засорять консоль
      if (!this.last401Logged) {
        console.warn('[getUserInfo] 401 Unauthorized - токен истек или невалиден');
        this.last401Logged = true;
        // Сбрасываем флаг через 5 секунд, чтобы можно было залогировать снова при следующей попытке
        setTimeout(() => { this.last401Logged = false; }, 5000);
      }
      throw new Error('Токен невалиден или истек. Пожалуйста, авторизуйтесь снова.');
    }

    if (!response.ok) {
      const errorText = await response.text();
      console.error(`[getUserInfo] HTTP ${response.status}:`, errorText);
      throw new Error(`Ошибка получения информации о пользователе: ${response.statusText}`);
    }

    const data = await response.json();
    if (data.data && data.data.length > 0) {
      return data.data[0];
    }

    throw new Error('Информация о пользователе не найдена');
  }

  /**
   * Проверка, является ли пользователь модератором канала
   */
  async isModerator(accessToken: string, channelId: string, userId: string, clientId?: string): Promise<boolean> {
    const cId = clientId || this.defaultClientId;

    try {
      if (!accessToken || !cId) {
        console.warn('[isModerator] Missing accessToken or clientId');
        return false;
      }

      const response = await fetch(`${this.apiUrl}/moderation/moderators?broadcaster_id=${channelId}&user_id=${userId}`, {
        method: 'GET',
        headers: {
          'Authorization': `Bearer ${accessToken}`,
          'Client-Id': cId
        }
      });

      if (response.status === 401) {
        console.error('[isModerator] 401 Unauthorized - токен невалиден или истек');
        const errorText = await response.text();
        console.error('[isModerator] Error response:', errorText);
        return false;
      }

      if (response.status === 403) {
        // Нет прав для проверки модераторов
        console.warn('[isModerator] 403 Forbidden - нет прав для проверки модераторов');
        return false;
      }

      if (!response.ok) {
        console.error(`[isModerator] HTTP ${response.status}: ${response.statusText}`);
        return false;
      }

      const data = await response.json();
      return data.data && data.data.length > 0;
    } catch (error) {
      console.error('Error checking moderator status:', error);
      return false;
    }
  }

  /**
   * Проверка, является ли пользователь стримером канала
   */
  async isBroadcaster(accessToken: string, channelId: string, userId: string, clientId?: string): Promise<boolean> {
    const cId = clientId || this.defaultClientId;

    try {
      if (!accessToken || !cId) {
        console.warn('[isBroadcaster] Missing accessToken or clientId');
        return false;
      }

      // Простая проверка: если channelId === userId, то это стример
      if (channelId === userId) {
        return true;
      }

      const response = await fetch(`${this.apiUrl}/channels?broadcaster_id=${channelId}`, {
        method: 'GET',
        headers: {
          'Authorization': `Bearer ${accessToken}`,
          'Client-Id': cId
        }
      });

      if (response.status === 401) {
        const errorText = await response.text();
        console.error('[isBroadcaster] 401 Unauthorized:', errorText);
        return false;
      }

      if (!response.ok) {
        const errorText = await response.text();
        console.error(`[isBroadcaster] HTTP ${response.status}:`, errorText);
        return false;
      }

      const data = await response.json();
      if (data.data && data.data.length > 0) {
        return data.data[0].broadcaster_id === userId;
      }

      return false;
    } catch (error) {
      console.error('Error checking broadcaster status:', error);
      return false;
    }
  }

  /**
   * Получение ID канала по имени
   */
  async getChannelId(accessToken: string, channelName: string, clientId?: string): Promise<string | null> {
    const cId = clientId || this.defaultClientId;

    try {
      if (!accessToken || !cId) {
        console.warn('[getChannelId] Missing accessToken or clientId');
        return null;
      }

      const response = await fetch(`${this.apiUrl}/users?login=${channelName}`, {
        method: 'GET',
        headers: {
          'Authorization': `Bearer ${accessToken}`,
          'Client-Id': cId
        }
      });

      if (response.status === 401) {
        const errorText = await response.text();
        console.error('[getChannelId] 401 Unauthorized:', errorText);
        return null;
      }

      if (!response.ok) {
        const errorText = await response.text();
        console.error(`[getChannelId] HTTP ${response.status}:`, errorText);
        return null;
      }

      const data = await response.json();
      if (data.data && data.data.length > 0) {
        return data.data[0].id;
      }

      return null;
    } catch (error) {
      console.error('Error getting channel ID:', error);
      return null;
    }
  }

  /**
   * Валидация токена
   */
  async validateToken(accessToken: string, clientId?: string): Promise<boolean> {
    const cId = clientId || this.defaultClientId;

    try {
      const response = await fetch(`${this.oauthUrl}/validate`, {
        method: 'GET',
        headers: {
          'Authorization': `Bearer ${accessToken}`
        }
      });

      // Если токен невалиден (401), не логируем ошибку, просто возвращаем false
      if (response.status === 401) {
        return false;
      }

      return response.ok;
    } catch (error) {
      // Логируем только сетевые ошибки, не ошибки валидации токена
      if (!this.last401Logged) {
        console.warn('[validateToken] Ошибка проверки токена:', error);
      }
      return false;
    }
  }
}

