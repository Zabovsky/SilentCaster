import { VoiceProfile } from './voice-profile.model';

export interface VoiceSettings {
  selectedVoice: string;
  rate: number; // -10 to 10
  volume: number; // 0 to 100
  voiceProfiles: VoiceProfile[];
  useMultipleVoices: boolean;
  currentVoiceIndex: number;
}

export class VoiceSettingsImpl implements VoiceSettings {
  selectedVoice: string = '';
  rate: number = 0;
  volume: number = 100;
  voiceProfiles: VoiceProfile[] = [];
  useMultipleVoices: boolean = false;
  currentVoiceIndex: number = 0;
}

