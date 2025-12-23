# Исправление ошибки "Twitch client not available"

## Проблема
При попытке подключения к Twitch возникала ошибка: "Ошибка подключения: Twitch client not available"

## Причина
Подключение к Twitch не было реализовано в main process Electron. Обработчики IPC возвращали "Not implemented yet".

## Решение

### 1. Реализовано подключение к Twitch в main.js
- Добавлен импорт `tmi.js` в main process
- Реализованы обработчики IPC для подключения/отключения
- Добавлены обработчики для модерации чата (удаление, таймаут, бан, очистка, шепот)
- Настроены события для передачи сообщений и статуса в renderer process

### 2. Обновлен preload.js
- Добавлены методы для работы с Twitch через IPC
- Настроены слушатели событий для сообщений и статуса

### 3. Обновлен TwitchService
- Добавлена поддержка подключения через Electron IPC
- Реализованы методы для модерации чата
- Настроены слушатели событий от main process

### 4. Обновлена модель ChatMessage
- Добавлено поле `isBroadcaster` для определения стримера

### 5. Создан файл типов
- Создан `types/electron-api.d.ts` для типизации Electron API

## Как использовать

### Анонимное подключение (только чтение чата):
```typescript
await twitchService.connectAnonymously('channelname');
```

### Подключение с авторизацией (с возможностью модерации):
```typescript
await twitchService.connect('username', 'oauth_token', 'channelname');
```

### Отключение:
```typescript
await twitchService.disconnect();
```

## Доступные методы модерации

- `sendMessage(channel, message)` - отправить сообщение в чат
- `deleteMessage(channel, messageId)` - удалить сообщение
- `timeoutUser(channel, username, duration, reason)` - таймаут пользователя
- `banUser(channel, username, reason)` - забанить пользователя
- `clearChat(channel)` - очистить чат
- `sendWhisper(username, message)` - отправить приватное сообщение

## Примечания

- Для модерации требуется подключение с OAuth токеном
- Анонимное подключение позволяет только читать чат
- Все события чата передаются через IPC из main process в renderer process

