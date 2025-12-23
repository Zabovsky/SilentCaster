import { Injectable } from '@angular/core';
import { Subject, Observable } from 'rxjs';
import { AppSettings, AppSettingsImpl } from '../models/app-settings.model';

@Injectable({
  providedIn: 'root'
})
export class SettingsService {
  private readonly SETTINGS_KEY = 'app_settings';
  private settings: AppSettings = new AppSettingsImpl();
  private settingsSubject = new Subject<AppSettings>();
  public settingsChanged$: Observable<AppSettings> = this.settingsSubject.asObservable();

  constructor() {
    this.loadSettings();
  }

  loadSettings(): AppSettings {
    try {
      if (window.electronAPI && (window.electronAPI as any).loadSettings) {
        const saved = (window.electronAPI as any).loadSettings();
        if (saved) {
          this.settings = { ...new AppSettingsImpl(), ...saved };
          return this.settings;
        }
      }

      // Fallback to localStorage
      const saved = localStorage.getItem(this.SETTINGS_KEY);
      if (saved) {
        this.settings = { ...new AppSettingsImpl(), ...JSON.parse(saved) };
      }
    } catch (error) {
      console.error('Error loading settings:', error);
      this.settings = new AppSettingsImpl();
    }
    return this.settings;
  }

  saveSettings(settings?: AppSettings): void {
    const settingsToSave = settings || this.settings;
    this.settings = settingsToSave;

    try {
      if (window.electronAPI && (window.electronAPI as any).saveSettings) {
        (window.electronAPI as any).saveSettings(settingsToSave);
      } else {
        // Fallback to localStorage
        localStorage.setItem(this.SETTINGS_KEY, JSON.stringify(settingsToSave));
      }
      this.settingsSubject.next(this.settings);
    } catch (error) {
      console.error('Error saving settings:', error);
    }
  }

  getSettings(): AppSettings {
    return { ...this.settings };
  }

  updateSettings(updates: Partial<AppSettings>): void {
    this.settings = { ...this.settings, ...updates };
    this.saveSettings();
    this.settingsSubject.next(this.settings);
  }
}

