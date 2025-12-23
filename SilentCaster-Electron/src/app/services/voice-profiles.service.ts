import { Injectable } from '@angular/core';
import { VoiceProfile, VoiceProfileImpl } from '../models/voice-profile.model';
import { VoiceSettings } from '../models/voice-settings.model';
import { SettingsService } from './settings.service';

@Injectable({
  providedIn: 'root'
})
export class VoiceProfilesService {
  constructor(private settingsService: SettingsService) {}

  getVoiceProfiles(): VoiceProfile[] {
    const settings = this.settingsService.getSettings();
    return settings.voiceSettings.voiceProfiles || [];
  }

  addVoiceProfile(profile: VoiceProfile): void {
    const settings = this.settingsService.getSettings();
    if (!settings.voiceSettings.voiceProfiles) {
      settings.voiceSettings.voiceProfiles = [];
    }
    settings.voiceSettings.voiceProfiles.push(profile);
    this.settingsService.updateSettings({ voiceSettings: settings.voiceSettings });
  }

  updateVoiceProfile(index: number, profile: VoiceProfile): void {
    const settings = this.settingsService.getSettings();
    if (settings.voiceSettings.voiceProfiles && 
        index >= 0 && index < settings.voiceSettings.voiceProfiles.length) {
      settings.voiceSettings.voiceProfiles[index] = profile;
      this.settingsService.updateSettings({ voiceSettings: settings.voiceSettings });
    }
  }

  removeVoiceProfile(index: number): void {
    const settings = this.settingsService.getSettings();
    if (settings.voiceSettings.voiceProfiles && 
        index >= 0 && index < settings.voiceSettings.voiceProfiles.length) {
      settings.voiceSettings.voiceProfiles.splice(index, 1);
      this.settingsService.updateSettings({ voiceSettings: settings.voiceSettings });
    }
  }

  createVoiceProfile(
    name: string,
    voiceName: string,
    rate: number = 0,
    volume: number = 100,
    description: string = ''
  ): VoiceProfile {
    return {
      ...new VoiceProfileImpl(),
      name,
      voiceName,
      rate,
      volume,
      description,
      isEnabled: true,
      useForChatMessages: true,
      useForQuickResponses: true,
      useForManualMessages: true,
      priority: 1,
      usageChance: 100
    };
  }

  getVoiceSettings(): VoiceSettings {
    return this.settingsService.getSettings().voiceSettings;
  }

  updateVoiceSettings(voiceSettings: VoiceSettings): void {
    this.settingsService.updateSettings({ voiceSettings });
  }
}

