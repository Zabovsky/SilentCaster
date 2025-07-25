# 🔊 Руководство по настройкам аудио устройств

## ✨ Новая функциональность

SilentCaster теперь поддерживает выбор устройства вывода звука с возможностью сохранения выбора! Это позволяет направлять озвучку сообщений чата на конкретные аудио устройства.

## 🎯 Основные возможности

### **🔊 Выбор аудио устройства**
- ✅ **Автоматическое обнаружение** - все доступные устройства вывода
- ✅ **Устройство по умолчанию** - системное устройство по умолчанию
- ✅ **Пользовательские устройства** - конкретные аудио карты и наушники
- ✅ **Сохранение выбора** - настройки запоминаются между запусками

### **🎵 Тестирование устройств**
- ✅ **Проверка доступности** - тест подключения устройства
- ✅ **Тестовый звук** - воспроизведение синусоиды 440 Hz
- ✅ **Настройка громкости** - регулировка громкости теста
- ✅ **Визуальная обратная связь** - статус доступности устройства

### **⚙️ Современный интерфейс**
- ✅ **Двухпанельный дизайн** - слева устройства, справа настройки
- ✅ **Информативные карточки** - название, каналы, частота дискретизации
- ✅ **Цветовая индикация** - зеленый/красный статус устройства
- ✅ **Удобное управление** - кнопки обновления и тестирования

## 🚀 Как использовать

### **1. Открытие настроек аудио устройств**
1. Главное окно → **Настройки** → **"🔊 Настройки аудио устройств"**

### **2. Выбор устройства**
1. В списке слева выберите нужное устройство
2. Устройство по умолчанию всегда доступно
3. Пользовательские устройства показывают статус доступности

### **3. Тестирование устройства**
1. Выберите устройство в списке
2. Нажмите **"🎵 Тест устройства"** для проверки доступности
3. Нажмите **"🎵 Тест звука"** для воспроизведения тестового звука
4. Настройте громкость теста с помощью слайдера

### **4. Применение настроек**
1. Нажмите **"💾 Применить"** для применения к текущей сессии
2. Нажмите **"💾 Сохранить"** для сохранения навсегда
3. Нажмите **"❌ Отмена"** для отмены изменений

## 🎛️ Поддерживаемые устройства

### **Типы устройств:**
- **Устройство по умолчанию** - системное устройство Windows
- **Встроенные динамики** - динамики ноутбука/монитора
- **Внешние наушники** - USB/Bluetooth наушники
- **Аудио карты** - внешние и внутренние звуковые карты
- **Виртуальные устройства** - виртуальные аудио кабели

### **Форматы аудио:**
- **Частота дискретизации:** 44.1 kHz (стандартная)
- **Битность:** 16-bit
- **Каналы:** Моно/Стерео (автоматическое определение)

## 🔧 Технические детали

### **Используемые технологии:**
- **NAudio** - библиотека для работы с аудио устройствами
- **Windows Core Audio** - системный API для аудио
- **JSON конфигурация** - сохранение настроек в файл

### **Файл конфигурации:**
```json
{
  "SelectedDeviceId": "default",
  "UseCustomDevice": false
}
```

### **Расположение файла:**
- `audio_device_config.json` - в папке с приложением

## 🎯 Примеры использования

### **Сценарий 1: Стрим с наушниками**
1. Подключите наушники к компьютеру
2. Откройте настройки аудио устройств
3. Выберите ваши наушники в списке
4. Протестируйте звук
5. Сохраните настройки
6. Теперь озвучка чата будет идти только в наушники

### **Сценарий 2: Разделение аудио**
1. Настройте виртуальный аудио кабель
2. Выберите виртуальное устройство в SilentCaster
3. Настройте OBS для записи с виртуального устройства
4. Теперь озвучка чата не попадет в запись стрима

### **Сценарий 3: Множественные устройства**
1. Подключите несколько аудио устройств
2. Протестируйте каждое устройство
3. Выберите наиболее подходящее
4. Сохраните настройки

## 🔍 Устранение неполадок

### **Проблема: Устройство не отображается**
**Решение:**
1. Убедитесь, что устройство подключено
2. Проверьте драйверы устройства
3. Нажмите **"🔄 Обновить список"**
4. Перезапустите приложение

### **Проблема: Нет звука при тестировании**
**Решение:**
1. Проверьте громкость устройства в Windows
2. Убедитесь, что устройство не отключено
3. Проверьте настройки приложений в Windows
4. Попробуйте другое устройство

### **Проблема: Настройки не сохраняются**
**Решение:**
1. Убедитесь, что у приложения есть права на запись
2. Проверьте свободное место на диске
3. Перезапустите приложение от имени администратора

## 📋 Советы по использованию

### **Для стримеров:**
- Используйте отдельные наушники для озвучки чата
- Настройте виртуальный аудио кабель для разделения звука
- Регулярно тестируйте устройства перед стримом

### **Для зрителей:**
- Выберите устройство с лучшим качеством звука
- Настройте комфортную громкость теста
- Используйте устройство по умолчанию для простоты

### **Для разработчиков:**
- Интеграция с NAudio для расширенной функциональности
- Поддержка ASIO устройств для профессионального аудио
- Возможность добавления эквалайзера и эффектов

## 🚀 Планы развития

### **Будущие возможности:**
- **Эквалайзер** - настройка частотных характеристик
- **Эффекты** - реверберация, эхо, компрессия
- **ASIO поддержка** - профессиональные аудио интерфейсы
- **Автоматическое переключение** - смена устройств по расписанию
- **Профили устройств** - сохранение настроек для разных сценариев

## 📞 Поддержка

Если у вас возникли проблемы с настройками аудио устройств:

1. **Проверьте документацию** - эта страница содержит основные решения
2. **Тестируйте устройства** - используйте встроенные тесты
3. **Обновите драйверы** - актуальные драйверы решают большинство проблем
4. **Обратитесь в поддержку** - опишите проблему с деталями

---

**🎵 Наслаждайтесь качественным звуком в SilentCaster!** 