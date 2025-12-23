export interface VoiceProfile {
  name: string;
  voiceName: string;
  rate: number; // -10 to 10
  volume: number; // 0 to 100
  isEnabled: boolean;
  description: string;
  useForChatMessages: boolean;
  useForQuickResponses: boolean;
  useForManualMessages: boolean;
  priority: number;
  usageChance: number; // 0 to 100
}

export class VoiceProfileImpl implements VoiceProfile {
  name: string = '';
  voiceName: string = '';
  rate: number = 0;
  volume: number = 100;
  isEnabled: boolean = true;
  description: string = '';
  useForChatMessages: boolean = true;
  useForQuickResponses: boolean = true;
  useForManualMessages: boolean = true;
  priority: number = 1;
  usageChance: number = 100;
}

