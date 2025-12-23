// Типы для Electron API
// Этот файл автоматически подхватывается TypeScript
// Не нужно явно импортировать его

export interface ElectronAPI {
  // App info
  getVersion: () => Promise<string>;
  getName: () => Promise<string>;
  
  // Settings
  loadSettings: () => any;
  saveSettings: (settings: any) => void;
  
  // Responses
  loadResponses: () => any[];
  saveResponses: (responses: any[]) => void;
  
  // Twitch
  connectTwitch: (options: {
    channel: string;
    anonymous?: boolean;
    username?: string;
    oauthToken?: string;
  }) => Promise<{ success: boolean; message?: string }>;
  
  disconnectTwitch: () => Promise<{ success: boolean; message?: string }>;
  
  sendMessage: (channel: string, message: string) => Promise<{ success: boolean; message?: string }>;
  deleteMessage: (channel: string, messageId: string) => Promise<{ success: boolean; message?: string }>;
  timeoutUser: (channel: string, username: string, duration: number, reason?: string) => Promise<{ success: boolean; message?: string }>;
  banUser: (channel: string, username: string, reason?: string) => Promise<{ success: boolean; message?: string }>;
  clearChat: (channel: string) => Promise<{ success: boolean; message?: string }>;
  sendWhisper: (username: string, message: string) => Promise<{ success: boolean; message?: string }>;
  
  // Event listeners
  onTwitchMessage: (callback: (data: any) => void) => (() => void) | void;
  onTwitchStatus: (callback: (data: any) => void) => (() => void) | void;
}

declare global {
  interface Window {
    electronAPI?: ElectronAPI;
  }
}

