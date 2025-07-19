# Интеграция с TTS Voice Wizard

## 🎯 Обзор

SilentCaster теперь поддерживает интеграцию с внешними TTS движками, включая **TTS Voice Wizard** (ttsvoicewizard.com). Это позволяет использовать высококачественные голоса и дополнительные возможности синтеза речи.

## 📋 Поддерживаемые TTS движки

- **TTS Voice Wizard** - основной поддерживаемый движок
- **eSpeak** - открытый TTS движок
- **RHVoice** - русскоязычный TTS движок
- **Пользовательские движки** - любые другие TTS программы

## 🚀 Настройка TTS Voice Wizard

### 1. Установка TTS Voice Wizard

1. Скачайте TTS Voice Wizard с официального сайта: https://ttsvoicewizard.com
2. Установите программу в папку по умолчанию: `C:\Program Files\TTS Voice Wizard\`
3. Убедитесь, что файл `TTSVoiceWizard.exe` доступен

### 2. Настройка в SilentCaster

1. Откройте **Настройки** в главном окне
2. Нажмите кнопку **"Внешние TTS движки"**
3. Поставьте галочку **"Использовать внешние TTS движки"**
4. Выберите **"TTS Voice Wizard"** как провайдер по умолчанию

### 3. Настройка провайдера

#### Основные параметры:
- **Имя**: `TTS Voice Wizard`
- **Тип**: `TTS Voice Wizard`
- **Путь к exe**: `C:\Program Files\TTS Voice Wizard\TTSVoiceWizard.exe`
- **Аргументы**: `-voice "{voice}" -rate {rate} -volume {volume} -text "{text}"`

#### Добавление голосов:
1. Нажмите **"➕ Добавить голос"**
2. Укажите **Имя голоса** (например: "Microsoft Anna")
3. Укажите **ID голоса** (например: "Microsoft Anna")
4. Выберите **Язык** (например: "ru-RU")
5. Настройте **Скорость** и **Громкость**

## 🎛️ Примеры конфигурации

### TTS Voice Wizard с Microsoft голосами

```json
{
  "Name": "TTS Voice Wizard",
  "Type": "ttsvoicewizard",
  "ExecutablePath": "C:\\Program Files\\TTS Voice Wizard\\TTSVoiceWizard.exe",
  "Arguments": "-voice \"{voice}\" -rate {rate} -volume {volume} -text \"{text}\"",
  "Voices": [
    {
      "Name": "Microsoft Anna",
      "VoiceId": "Microsoft Anna",
      "Language": "en-US"
    },
    {
      "Name": "Microsoft Irina",
      "VoiceId": "Microsoft Irina", 
      "Language": "ru-RU"
    },
    {
      "Name": "Microsoft Pavel",
      "VoiceId": "Microsoft Pavel",
      "Language": "ru-RU"
    }
  ]
}
```

### eSpeak конфигурация

```json
{
  "Name": "eSpeak",
  "Type": "espeak",
  "ExecutablePath": "C:\\Program Files\\eSpeak\\command_line\\espeak.exe",
  "Arguments": "-v {voice} -s {rate} -a {volume} \"{text}\"",
  "Voices": [
    {
      "Name": "Russian",
      "VoiceId": "ru",
      "Language": "ru-RU"
    },
    {
      "Name": "English",
      "VoiceId": "en",
      "Language": "en-US"
    }
  ]
}
```

## 🔧 Параметры командной строки

### TTS Voice Wizard
- `{voice}` - ID голоса
- `{rate}` - скорость речи (-10 до 10)
- `{volume}` - громкость (0-100)
- `{text}` - текст для озвучивания

### eSpeak
- `{voice}` - код языка (ru, en, etc.)
- `{rate}` - скорость (80-450)
- `{volume}` - громкость (0-200)
- `{text}` - текст для озвучивания

## 🎯 Использование в профилях голосов

1. Откройте **"Управление голосовыми профилями"**
2. Создайте новый профиль
3. В выпадающем списке голосов выберите внешний голос (например: "TTS Voice Wizard - Microsoft Anna")
4. Настройте параметры взаимодействия
5. Активируйте профиль в настройках

## 🐛 Устранение неполадок

### Проблема: "Файл не найден"
**Решение**: Проверьте путь к исполняемому файлу в настройках провайдера

### Проблема: "Ошибка при выполнении теста"
**Решение**: 
1. Проверьте аргументы командной строки
2. Убедитесь, что TTS Voice Wizard установлен корректно
3. Попробуйте запустить TTS Voice Wizard вручную

### Проблема: "Голос не работает"
**Решение**:
1. Проверьте ID голоса в настройках
2. Убедитесь, что голос установлен в системе
3. Протестируйте голос в самом TTS Voice Wizard

## 📁 Файлы конфигурации

Конфигурация внешних TTS движков сохраняется в файле:
```
[Папка приложения]/external_tts_config.json
```

## 🔄 Обновление голосов

После установки новых голосов в TTS Voice Wizard:
1. Откройте настройки внешних TTS
2. Выберите провайдер TTS Voice Wizard
3. Добавьте новые голоса в список
4. Сохраните настройки

## 💡 Советы

1. **Тестируйте голоса** перед использованием в профилях
2. **Используйте разные голоса** для разных типов сообщений
3. **Настройте приоритеты** для разнообразия озвучивания
4. **Регулярно обновляйте** TTS Voice Wizard для новых возможностей

## 🌐 Дополнительные ресурсы

- [Официальный сайт TTS Voice Wizard](https://ttsvoicewizard.com)
- [Документация TTS Voice Wizard](https://ttsvoicewizard.com/docs)
- [Форум поддержки](https://ttsvoicewizard.com/forum) 