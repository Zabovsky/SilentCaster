import { Injectable } from '@angular/core';
import { VoiceProfile } from '../models/voice-profile.model';
import { VoiceSettings } from '../models/voice-settings.model';

@Injectable({
  providedIn: 'root'
})
export class TtsService {
  private synth: SpeechSynthesis | null = null;
  private currentVoice: SpeechSynthesisVoice | null = null;
  private voiceSettings: VoiceSettings | null = null;

  constructor() {
    if ('speechSynthesis' in window) {
      this.synth = window.speechSynthesis;
    }
  }

  getAvailableVoices(): SpeechSynthesisVoice[] {
    if (!this.synth) return [];
    return this.synth.getVoices();
  }

  getAvailableVoiceNames(): string[] {
    return this.getAvailableVoices().map(voice => voice.name);
  }

  selectVoice(voiceName: string): void {
    if (!this.synth) return;

    const voices = this.getAvailableVoices();
    const voice = voices.find(v => v.name === voiceName);
    if (voice) {
      this.currentVoice = voice;
    }
  }

  setRate(rate: number): void {
    // Web Speech API rate: 0.1 to 10, where 1 is normal
    // Legacy rate: -10 to 10, where 0 is normal
    // Convert: legacyRate = (webRate - 1) * 10
    // Reverse: webRate = (legacyRate / 10) + 1
    this.voiceSettings = this.voiceSettings || { rate, volume: 100, selectedVoice: '', voiceProfiles: [], useMultipleVoices: false, currentVoiceIndex: 0 };
    this.voiceSettings.rate = rate;
  }

  setVolume(volume: number): void {
    // Web Speech API volume: 0 to 1
    // Legacy volume: 0 to 100
    this.voiceSettings = this.voiceSettings || { rate: 0, volume, selectedVoice: '', voiceProfiles: [], useMultipleVoices: false, currentVoiceIndex: 0 };
    this.voiceSettings.volume = volume;
  }

  updateSettings(settings: VoiceSettings): void {
    this.voiceSettings = settings;
    if (settings.selectedVoice) {
      this.selectVoice(settings.selectedVoice);
    }
  }

  async speak(text: string, username?: string, messageType: string = 'chat'): Promise<void> {
    if (!this.synth || !text) return;

    // Заменяем плейсхолдеры
    const processedText = text.replace(/{username}/g, username || 'пользователь');

    return new Promise((resolve, reject) => {
      try {
        // Выбираем профиль голоса если используется
        const selectedProfile = this.selectVoiceProfile(messageType);
        
        const utterance = new SpeechSynthesisUtterance(processedText);
        
        // Применяем настройки голоса
        if (selectedProfile) {
          utterance.voice = this.getAvailableVoices().find(v => v.name === selectedProfile.voiceName) || null;
          utterance.rate = this.convertRate(selectedProfile.rate);
          utterance.volume = selectedProfile.volume / 100;
        } else if (this.voiceSettings) {
          utterance.voice = this.currentVoice;
          utterance.rate = this.convertRate(this.voiceSettings.rate);
          utterance.volume = this.voiceSettings.volume / 100;
        } else {
          utterance.voice = this.currentVoice;
          utterance.rate = 1;
          utterance.volume = 1;
        }

        utterance.onend = () => resolve();
        utterance.onerror = (error) => {
          console.error('TTS Error:', error);
          reject(error);
        };

        this.synth!.speak(utterance);
      } catch (error) {
        console.error('Error speaking:', error);
        reject(error);
      }
    });
  }

  async speakTest(): Promise<void> {
    await this.speak('Это тестовое сообщение для проверки голоса.');
  }

  private convertRate(legacyRate: number): number {
    // Legacy: -10 to 10, where 0 is normal
    // Web Speech API: 0.1 to 10, where 1 is normal
    return (legacyRate / 10) + 1;
  }

  private selectVoiceProfile(messageType: string): VoiceProfile | null {
    if (!this.voiceSettings || !this.voiceSettings.useMultipleVoices) {
      return null;
    }

    const availableProfiles = this.voiceSettings.voiceProfiles
      .filter(p => p.isEnabled)
      .filter(p => {
        // Если ни одна галочка не стоит — профиль подходит для всех типов
        if (!p.useForChatMessages && !p.useForQuickResponses && !p.useForManualMessages) {
          return true;
        }
        // Иначе — профиль подходит только для выбранных типов
        return (
          (messageType === 'chat' && p.useForChatMessages) ||
          (messageType === 'quick' && p.useForQuickResponses) ||
          (messageType === 'manual' && p.useForManualMessages)
        );
      });

    if (availableProfiles.length === 0) {
      return null;
    }

    // Сортируем по приоритету
    availableProfiles.sort((a, b) => a.priority - b.priority);

    // Выбираем профиль с учетом шанса использования
    for (const profile of availableProfiles) {
      if (Math.random() * 100 <= profile.usageChance) {
        return profile;
      }
    }

    // Если ни один профиль не выбран по шансу, возвращаем первый с наивысшим приоритетом
    return availableProfiles[0];
  }

  stop(): void {
    if (this.synth) {
      this.synth.cancel();
    }
  }
}

