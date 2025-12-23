# Исправление ошибки "Electron API not available"

## Проблема
При попытке использования Electron API возникает ошибка: "Electron API not available"

## Причины
1. Preload script не загружается
2. Context isolation не настроен правильно
3. Файл типов не подключен в TypeScript
4. Приложение запущено в браузере вместо Electron

## Исправления

### 1. Добавлено логирование в preload.js
- Добавлены console.log для отслеживания загрузки preload script
- Добавлена обработка ошибок при exposeInMainWorld

### 2. Добавлено логирование в main.js
- Проверка существования preload.js файла
- Логирование пути к preload script
- Проверка доступности electronAPI после загрузки контента

### 3. Добавлен импорт типов
- Импорт `electron-api.d.ts` в компонентах и сервисах
- Обновлен tsconfig.app.json для включения всех .d.ts файлов

### 4. Улучшена диагностика в app.component.ts
- Добавлено логирование для проверки доступности electronAPI
- Добавлены предупреждения с возможными причинами

## Как проверить

1. Откройте DevTools в Electron (F12 или через меню)
2. Проверьте консоль на наличие сообщений:
   - `[Preload] Preload script loaded` - preload загружен
   - `[Preload] electronAPI exposed successfully` - API успешно экспортирован
   - `window.electronAPI: [object Object]` - API доступен в renderer

3. Если вы видите ошибки:
   - Проверьте путь к preload.js в консоли main process
   - Убедитесь, что preload.js существует
   - Проверьте, что contextIsolation: true в webPreferences

## Решение проблем

### Проблема: Preload script не загружается
**Решение:**
- Убедитесь, что preload.js находится в корне проекта
- Проверьте путь в main.js: `path.join(__dirname, 'preload.js')`
- В dev режиме `__dirname` указывает на корень проекта

### Проблема: Context isolation не работает
**Решение:**
- Убедитесь, что `contextIsolation: true` в webPreferences
- Убедитесь, что `nodeIntegration: false`
- Не используйте `enableRemoteModule: true` (устарело)

### Проблема: Файл типов не подключается
**Решение:**
- Убедитесь, что `electron-api.d.ts` находится в `src/app/types/`
- Добавьте импорт: `import '../types/electron-api.d';`
- Проверьте, что tsconfig.app.json включает `src/**/*.d.ts`

## Проверка в консоли

Откройте DevTools и выполните:
```javascript
console.log('electronAPI:', window.electronAPI);
console.log('typeof electronAPI:', typeof window.electronAPI);
```

Если `electronAPI` определен, вы должны увидеть объект с методами.

