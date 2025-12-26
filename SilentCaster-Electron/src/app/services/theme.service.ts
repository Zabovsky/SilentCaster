import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';

export type Theme = 'light' | 'dark' | 'peach';

@Injectable({
  providedIn: 'root'
})
export class ThemeService {
  private currentThemeSubject = new BehaviorSubject<Theme>('dark');
  public currentTheme$: Observable<Theme> = this.currentThemeSubject.asObservable();

  constructor() {
    // Загружаем сохраненную тему или используем темную по умолчанию
    const savedTheme = localStorage.getItem('app-theme') as Theme;
    const themeToSet = (savedTheme && ['light', 'dark', 'peach'].includes(savedTheme)) ? savedTheme : 'dark';
    
    // Применяем тему сразу при создании сервиса
    if (typeof document !== 'undefined') {
      document.documentElement.setAttribute('data-theme', themeToSet);
    }
    
    // Устанавливаем тему в subject
    this.currentThemeSubject.next(themeToSet);
  }

  getCurrentTheme(): Theme {
    return this.currentThemeSubject.value;
  }

  setTheme(theme: Theme, save: boolean = true): void {
    this.currentThemeSubject.next(theme);
    // Применяем тему к document.documentElement
    if (typeof document !== 'undefined') {
      document.documentElement.setAttribute('data-theme', theme);
    }
    if (save) {
      localStorage.setItem('app-theme', theme);
    }
  }

  toggleTheme(): void {
    const themes: Theme[] = ['light', 'dark', 'peach'];
    const currentIndex = themes.indexOf(this.currentThemeSubject.value);
    const nextIndex = (currentIndex + 1) % themes.length;
    this.setTheme(themes[nextIndex]);
  }
}

