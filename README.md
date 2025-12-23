# SilentCaster Project Structure

Проект разделен на две версии:

## 📁 Структура проекта

```
SilentCaster/
├── SilentCaster-Legacy/     # Старая версия (WPF .NET)
│   └── Все файлы оригинального проекта
│
└── SilentCaster-Electron/   # Новая версия (Electron + Angular)
    ├── main.js              # Главный процесс Electron
    ├── preload.js           # Preload скрипт
    ├── package.json         # Зависимости и скрипты
    ├── angular.json         # Конфигурация Angular
    └── src/                 # Исходный код Angular приложения
        ├── app/             # Angular компоненты и сервисы
        ├── assets/          # Статические ресурсы
        └── ...
```

## 🎯 SilentCaster-Legacy

Оригинальная версия приложения на WPF (.NET 8.0). Содержит все текущие функции:
- Twitch Chat Integration
- Text-to-Speech
- Voice Profiles
- Quick Responses
- Emotional Reactions
- OBS Integration
- И многое другое

**Для запуска:**
```bash
cd SilentCaster-Legacy
dotnet run
```

## 🚀 SilentCaster-Electron

Новая версия приложения на Electron + Angular. Все функции из Legacy версии будут постепенно перенесены.

**Для запуска:**
```bash
cd SilentCaster-Electron
npm install
npm run electron:dev
```

## 📋 План миграции

Все функции из Legacy версии будут перенесены в новую версию:
- ✅ Базовая структура проекта
- ⏳ Twitch Service
- ⏳ TTS Service
- ⏳ Voice Profiles
- ⏳ Quick Responses
- ⏳ Emotional Reactions
- ⏳ OBS Integration
- ⏳ Settings Management

## 📝 Лицензия

MIT

