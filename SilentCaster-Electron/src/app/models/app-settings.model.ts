import { VoiceSettings } from './voice-settings.model';

export interface ModerationSettings {
  forbiddenWordsEnabled: boolean;
  forbiddenWords: string[];
  caseSensitive: boolean;
  spamDetectionEnabled: boolean;
  capsDetectionEnabled: boolean;
  messageLengthCheckEnabled: boolean;
  maxMessageLength: number;
  autoDeleteHighSeverity: boolean;
}

export interface AppSettings {
  alwaysOnTop: boolean;
  useMultipleVoices: boolean;
  lastUsername: string;
  lastChannel: string;
  windowLeft: number;
  windowTop: number;
  windowWidth: number;
  windowHeight: number;
  windowMaximized: boolean;
  voiceSettings: VoiceSettings;
  enableChatVoice: boolean;
  chatTriggerSymbol: string;
  voiceOnlyForSubscribers: boolean;
  voiceOnlyForVips: boolean;
  voiceOnlyForModerators: boolean;
  voiceOnlyForFollowers: boolean;
  ttsMode: string; // 'reward' | 'trigger' | 'all'
  eventSubtitlesEnabled: boolean;
  eventSoundsEnabled: boolean;
  maxChatMessages: number;
  twitchOAuthToken: string;
  twitchChannelId: string;
  twitchRewardId: string;
  twitchClientId: string;
  twitchBannedWords?: string[];
  moderationSettings: ModerationSettings;
}

export class AppSettingsImpl implements AppSettings {
  alwaysOnTop: boolean = true;
  useMultipleVoices: boolean = false;
  lastUsername: string = '';
  lastChannel: string = '';
  windowLeft: number = 100;
  windowTop: number = 100;
  windowWidth: number = 800;
  windowHeight: number = 600;
  windowMaximized: boolean = false;
  voiceSettings: VoiceSettings = {
    selectedVoice: '',
    rate: 0,
    volume: 100,
    voiceProfiles: [],
    useMultipleVoices: false,
    currentVoiceIndex: 0
  };
  enableChatVoice: boolean = true;
  chatTriggerSymbol: string = '!!!';
  voiceOnlyForSubscribers: boolean = false;
  voiceOnlyForVips: boolean = false;
  voiceOnlyForModerators: boolean = false;
  voiceOnlyForFollowers: boolean = false;
  ttsMode: string = 'reward';
  eventSubtitlesEnabled: boolean = false;
  eventSoundsEnabled: boolean = false;
  maxChatMessages: number = 100;
  twitchOAuthToken: string = '';
  twitchChannelId: string = '';
  twitchRewardId: string = '';
  twitchClientId: string = '';
  twitchBannedWords: string[] = [];
  moderationSettings: ModerationSettings = {
    forbiddenWordsEnabled: true,
    forbiddenWords: [],
    caseSensitive: false,
    spamDetectionEnabled: true,
    capsDetectionEnabled: true,
    messageLengthCheckEnabled: true,
    maxMessageLength: 200,
    autoDeleteHighSeverity: true
  };
}

