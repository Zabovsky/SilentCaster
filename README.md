# 🎯 SilentCaster - Помощник для стримеров

**SilentCaster** - это бесплатное приложение для стримеров, которое помогает озвучивать сообщения чата с помощью синтеза речи. Идеально подходит для стримеров, которые хотят оставаться "тихими" во время стримов, но при этом слышать сообщения зрителей.

## ✨ Основные возможности

### 🎤 **Синтез речи**
- **Встроенные голоса Windows** - использование системных голосов
- **Внешние TTS движки** - поддержка TTS Voice Wizard, eSpeak, RHVoice, Balabolka
- **Множественные голосовые профили** - разные голоса для разных типов сообщений
- **Настройка параметров** - скорость, громкость, приоритеты

### 🔊 **Настройки аудио устройств** *(НОВОЕ!)*
- **Выбор устройства вывода** - направление звука на конкретные устройства
- **Автоматическое обнаружение** - все доступные аудио устройства
- **Тестирование устройств** - проверка доступности и воспроизведение звука
- **Сохранение настроек** - запоминание выбора между запусками

### 💬 **Управление чатом**
- **Подключение к Twitch** - анонимное подключение к любому каналу
- **Быстрые ответы** - автоматические ответы на сообщения чата
- **Категории ответов** - организация по типам (приветствия, шутки, реакции)
- **Приоритеты и шансы** - настройка частоты использования ответов

### ⚡ **Быстрые действия**
- **Ручные сообщения** - озвучивание собственных сообщений
- **Тестирование голоса** - проверка настроек синтеза речи
- **Управление профилями** - создание и настройка голосовых профилей

## 🚀 Быстрый старт

### 1. **Установка**
```bash
# Клонирование репозитория
git clone https://github.com/Zabovsky/SilentCaster.git
cd SilentCaster

# Сборка проекта
dotnet build

# Запуск
dotnet run
```

### 2. **Подключение к Twitch**
1. Введите название канала для мониторинга
2. Нажмите "🔗 Подключиться"
3. Приложение автоматически подключится к чату

### 3. **Настройка голоса**
1. Перейдите в "Настройки" → "Настройки голоса"
2. Выберите голос и настройте параметры
3. Протестируйте голос кнопкой "🎵 Тест голоса"

### 4. **Настройка аудио устройства**
1. Перейдите в "Настройки" → "🔊 Настройки аудио устройств"
2. Выберите устройство вывода звука
3. Протестируйте устройство
4. Сохраните настройки

## 🎛️ Детальные настройки

### **Голосовые профили**
- Создавайте разные профили для разных типов сообщений
- Настраивайте приоритеты и шансы использования
- Управляйте применением к чату/быстрым ответам/ручным сообщениям

### **Быстрые ответы**
- Триггеры: ключевые слова или `*` для всех сообщений
- Поддержка плейсхолдеров: `{username}` заменяется именем пользователя
- Категории: общие, приветствия, прощания, шутки, реакции

### **Внешние TTS движки**
- **TTS Voice Wizard** - профессиональный TTS движок
- **eSpeak** - открытый TTS движок
- **RHVoice** - русскоязычный TTS
- **Balabolka** - популярный TTS движок
- **Пользовательские движки** - поддержка любых TTS программ

### **Аудио устройства**
- **Устройство по умолчанию** - системное устройство Windows
- **Внешние наушники** - USB/Bluetooth наушники
- **Аудио карты** - внешние и внутренние звуковые карты
- **Виртуальные устройства** - виртуальные аудио кабели

## 📋 Системные требования

- **ОС:** Windows 10/11
- **.NET:** .NET 8.0 Runtime
- **Память:** 100 MB RAM
- **Сеть:** Интернет-соединение для подключения к Twitch

## 🔧 Технические детали

### **Используемые технологии:**
- **.NET 8.0** - основная платформа
- **WPF** - пользовательский интерфейс
- **TwitchLib** - подключение к Twitch API
- **System.Speech** - синтез речи Windows
- **NAudio** - работа с аудио устройствами
- **Newtonsoft.Json** - сериализация данных

### **Архитектура:**
- **MVVM паттерн** - разделение логики и интерфейса
- **Сервис-ориентированная архитектура** - модульная структура
- **Событийная модель** - асинхронная обработка сообщений

## 📁 Структура проекта

```
SilentCaster/
├── Services/                 # Сервисы приложения
│   ├── AudioDeviceService.cs    # Управление аудио устройствами
│   ├── SpeechService.cs         # Синтез речи
│   ├── TwitchService.cs         # Подключение к Twitch
│   ├── ResponseService.cs       # Управление быстрыми ответами
│   └── SettingsService.cs       # Настройки приложения
├── Models/                   # Модели данных
│   ├── VoiceSettings.cs         # Настройки голоса
│   └── ChatMessage.cs           # Модель сообщения чата
├── Windows/                  # Окна приложения
│   ├── MainWindow.xaml          # Главное окно
│   ├── SettingsWindow.xaml      # Настройки быстрых ответов
│   ├── VoiceProfilesWindow.xaml # Управление голосовыми профилями
│   ├── ExternalTTSWindow.xaml   # Внешние TTS движки
│   └── AudioDeviceSettingsWindow.xaml # Настройки аудио устройств
└── Documentation/            # Документация
    ├── Audio_Device_Settings_Guide.md
    ├── External_TTS_Guide.md
    └── Voice_Profiles_Guide.md
```

## 🎯 Примеры использования

### **Сценарий 1: Стрим с наушниками**
1. Подключите наушники к компьютеру
2. Откройте настройки аудио устройств
3. Выберите ваши наушники
4. Настройте голосовые профили
5. Теперь озвучка чата будет идти только в наушники

### **Сценарий 2: Разделение аудио для записи**
1. Установите виртуальный аудио кабель
2. Настройте SilentCaster на виртуальное устройство
3. Настройте OBS для записи с виртуального устройства
4. Озвучка чата не попадет в запись стрима

### **Сценарий 3: Множественные голоса**
1. Создайте несколько голосовых профилей
2. Настройте разные голоса для разных типов сообщений
3. Включите "Использовать множественные голоса"
4. Приложение будет автоматически выбирать голоса

## 🤝 Вклад в проект

Мы приветствуем вклад в развитие проекта! Вот как вы можете помочь:

### **Сообщения об ошибках**
- Используйте [Issues](../../issues) для сообщений об ошибках
- Опишите проблему подробно с шагами воспроизведения
- Приложите логи ошибок, если возможно

### **Предложения функций**
- Создайте [Issue](../../issues) с тегом "enhancement"
- Опишите желаемую функциональность
- Объясните, как это улучшит пользовательский опыт

### **Код**
1. Форкните репозиторий
2. Создайте ветку для новой функции
3. Внесите изменения
4. Создайте Pull Request

## 📄 Лицензия

Этот проект распространяется под лицензией MIT. См. файл [LICENSE](LICENSE) для подробностей.

## 🙏 Благодарности

- **TwitchLib** - библиотека для работы с Twitch API
- **NAudio** - библиотека для работы с аудио
- **TTS Voice Wizard** - внешний TTS движок
- **Сообщество стримеров** - за идеи и обратную связь

## 📞 Поддержка

- **GitHub Issues** - для технических проблем
- **Документация** - подробные руководства в папке Documentation
- **Wiki** - дополнительная информация и FAQ

---

**🎵 Наслаждайтесь качественным стримингом с SilentCaster!**

*Сделано с ❤️ для стримеров* 
