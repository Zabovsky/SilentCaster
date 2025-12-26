import { Component, OnInit } from '@angular/core';
import { ThemeService } from './services/theme.service';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.scss']
})
export class AppComponent implements OnInit {
  title = 'SilentCaster 2.0';
  appVersion: string = '2.0.0';
  isElectron: boolean = false;

  constructor(private themeService: ThemeService) {
    // Инициализируем тему сразу при создании компонента
    // ThemeService уже загрузит сохраненную тему в конструкторе
  }

  ngOnInit(): void {
    console.log('AppComponent initialized');
    console.log('window.electronAPI:', window.electronAPI);
    console.log('typeof window.electronAPI:', typeof window.electronAPI);
    console.log('User Agent:', navigator.userAgent);
    console.log('Is Electron:', navigator.userAgent.includes('Electron'));
    
    // Проверка доступности Electron API
    if (window.electronAPI) {
      this.isElectron = true;
      console.log('✅ Electron API is available');
      window.electronAPI.getVersion().then((version: string) => {
        this.appVersion = version;
        console.log('App version:', version);
      }).catch(err => {
        console.error('Error getting version:', err);
      });
    } else {
      console.error('❌ Electron API not available');
      console.error('Current environment:', {
        userAgent: navigator.userAgent,
        isElectron: navigator.userAgent.includes('Electron'),
        location: window.location.href,
        protocol: window.location.protocol
      });
      
      // Показываем предупреждение только если это не браузер
      if (!navigator.userAgent.includes('Electron')) {
        console.warn('⚠️ Приложение запущено в браузере, а не в Electron!');
        console.warn('Для работы приложения необходимо запустить через Electron:');
        console.warn('1. npm run electron:dev (автоматический запуск)');
        console.warn('2. Или: npm start (терминал 1) + npm run electron (терминал 2)');
      } else {
        console.error('⚠️ Electron обнаружен, но electronAPI недоступен!');
        console.error('Возможные причины:');
        console.error('1. Preload script не загрузился');
        console.error('2. Context isolation не работает');
        console.error('3. contextBridge ошибка');
      }
    }
  }
}

// Типы определены в types/electron-api.d.ts

