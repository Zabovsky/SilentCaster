import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';
import { SettingsService } from './settings.service';

@Injectable({
  providedIn: 'root'
})
export class ForbiddenWordsService {
  private forbiddenWordsSubject = new BehaviorSubject<string[]>([]);
  public forbiddenWords$: Observable<string[]> = this.forbiddenWordsSubject.asObservable();
  
  private caseSensitiveSubject = new BehaviorSubject<boolean>(false);
  public caseSensitive$: Observable<boolean> = this.caseSensitiveSubject.asObservable();
  
  private enabledSubject = new BehaviorSubject<boolean>(true);
  public enabled$: Observable<boolean> = this.enabledSubject.asObservable();

  constructor(private settingsService: SettingsService) {
    this.loadForbiddenWords();
  }

  private async loadForbiddenWords(): Promise<void> {
    const settings = this.settingsService.getSettings();
    const moderationSettings = (settings as any).moderationSettings || {};
    
    this.forbiddenWordsSubject.next(moderationSettings.forbiddenWords || []);
    this.caseSensitiveSubject.next(moderationSettings.caseSensitive || false);
    this.enabledSubject.next(moderationSettings.forbiddenWordsEnabled !== false);
  }

  getForbiddenWords(): string[] {
    return [...this.forbiddenWordsSubject.value];
  }

  async addForbiddenWord(word: string): Promise<void> {
    const words = this.getForbiddenWords();
    if (!words.includes(word)) {
      words.push(word);
      await this.saveForbiddenWords(words);
    }
  }

  async removeForbiddenWord(word: string): Promise<void> {
    const words = this.getForbiddenWords().filter(w => w !== word);
    await this.saveForbiddenWords(words);
  }

  async updateForbiddenWords(words: string[]): Promise<void> {
    await this.saveForbiddenWords(words);
  }

  async setCaseSensitive(caseSensitive: boolean): Promise<void> {
    this.caseSensitiveSubject.next(caseSensitive);
    await this.saveSettings();
  }

  async setEnabled(enabled: boolean): Promise<void> {
    this.enabledSubject.next(enabled);
    await this.saveSettings();
  }

  isCaseSensitive(): boolean {
    return this.caseSensitiveSubject.value;
  }

  isEnabled(): boolean {
    return this.enabledSubject.value;
  }

  checkMessage(message: string, twitchBannedWords: string[] = []): { hasViolation: boolean; foundWords: string[] } {
    if (!this.isEnabled()) {
      return { hasViolation: false, foundWords: [] };
    }

    // Объединяем локальные запретные слова и банворды Twitch
    const words = [...this.getForbiddenWords(), ...twitchBannedWords];
    const caseSensitive = this.isCaseSensitive();
    const foundWords: string[] = [];

    const messageToCheck = caseSensitive ? message : message.toLowerCase();

    for (const word of words) {
      const wordToCheck = caseSensitive ? word : word.toLowerCase();
      if (messageToCheck.includes(wordToCheck)) {
        foundWords.push(word);
      }
    }

    return {
      hasViolation: foundWords.length > 0,
      foundWords
    };
  }

  private async saveForbiddenWords(words: string[]): Promise<void> {
    this.forbiddenWordsSubject.next(words);
    await this.saveSettings();
  }

  private async saveSettings(): Promise<void> {
    const settings = this.settingsService.getSettings();
    const moderationSettings = (settings as any).moderationSettings || {};
    
    moderationSettings.forbiddenWords = this.getForbiddenWords();
    moderationSettings.caseSensitive = this.isCaseSensitive();
    moderationSettings.forbiddenWordsEnabled = this.isEnabled();
    
    (settings as any).moderationSettings = moderationSettings;
    this.settingsService.updateSettings(settings);
  }
}

