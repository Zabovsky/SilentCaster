const { contextBridge, ipcRenderer } = require('electron');
const Store = require('electron-store');

// Логирование для отладки
console.log('[Preload] Preload script started loading...');
console.log('[Preload] contextBridge available:', typeof contextBridge !== 'undefined');
console.log('[Preload] ipcRenderer available:', typeof ipcRenderer !== 'undefined');

// Инициализация electron-store для настроек
let store;
try {
  store = new Store();
  console.log('[Preload] electron-store initialized');
} catch (error) {
  console.error('[Preload] Error initializing electron-store:', error);
  store = null;
}

console.log('[Preload] Preload script loaded');

// Безопасный API для Angular приложения
try {
  if (!contextBridge) {
    throw new Error('contextBridge is not available');
  }
  
  console.log('[Preload] Exposing electronAPI...');
  contextBridge.exposeInMainWorld('electronAPI', {
  getVersion: () => ipcRenderer.invoke('app-version'),
  getName: () => ipcRenderer.invoke('app-name'),
  
  // Настройки
  loadSettings: () => {
    try {
      return store ? store.get('settings', null) : null;
    } catch (error) {
      console.error('[Preload] Error loading settings:', error);
      return null;
    }
  },
  saveSettings: (settings) => {
    try {
      if (store) {
        store.set('settings', settings);
      }
    } catch (error) {
      console.error('[Preload] Error saving settings:', error);
    }
  },
  
  // Быстрые ответы
  loadResponses: () => {
    try {
      return store ? store.get('responses', []) : [];
    } catch (error) {
      console.error('[Preload] Error loading responses:', error);
      return [];
    }
  },
  saveResponses: (responses) => {
    try {
      if (store) {
        store.set('responses', responses);
      }
    } catch (error) {
      console.error('[Preload] Error saving responses:', error);
    }
  },
  
  // Twitch
  connectTwitch: (options) => ipcRenderer.invoke('twitch-connect', options),
  disconnectTwitch: () => ipcRenderer.invoke('twitch-disconnect'),
  sendMessage: (channel, message) => ipcRenderer.invoke('twitch-send-message', channel, message),
  deleteMessage: (channel, messageId) => ipcRenderer.invoke('twitch-delete-message', channel, messageId),
  timeoutUser: (channel, username, duration, reason) => ipcRenderer.invoke('twitch-timeout', channel, username, duration, reason),
  banUser: (channel, username, reason) => ipcRenderer.invoke('twitch-ban', channel, username, reason),
  clearChat: (channel) => ipcRenderer.invoke('twitch-clear', channel),
  sendWhisper: (username, message) => ipcRenderer.invoke('twitch-whisper', username, message),
  onTwitchMessage: (callback) => {
    ipcRenderer.on('twitch-message', (event, data) => callback(data));
    // Возвращаем функцию для очистки слушателя
    return () => ipcRenderer.removeAllListeners('twitch-message');
  },
  onTwitchStatus: (callback) => {
    ipcRenderer.on('twitch-status', (event, data) => callback(data));
    // Возвращаем функцию для очистки слушателя
    return () => ipcRenderer.removeAllListeners('twitch-status');
  },
  openExternal: (url) => ipcRenderer.invoke('open-external', url),
  
  // OAuth через браузер
  startOAuthFlow: (clientId, redirectUri, scopes) => ipcRenderer.invoke('twitch-oauth-start', clientId, redirectUri, scopes),
  getOAuthCode: () => new Promise((resolve) => {
    ipcRenderer.once('twitch-oauth-code', (event, code) => resolve(code));
  }),
  
  // Банворды Twitch
  getTwitchBannedWords: (channel) => ipcRenderer.invoke('twitch-get-banned-words', channel),
  saveTwitchBannedWords: (channel, words) => ipcRenderer.invoke('twitch-save-banned-words', channel, words),
  addTwitchBannedWord: (channel, word) => ipcRenderer.invoke('twitch-add-banned-word', channel, word),
  removeTwitchBannedWord: (channel, word) => ipcRenderer.invoke('twitch-remove-banned-word', channel, word)
  });
  
  console.log('[Preload] electronAPI exposed successfully');
} catch (error) {
  console.error('[Preload] Error exposing electronAPI:', error);
}

