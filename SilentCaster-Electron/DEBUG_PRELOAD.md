# Диагностика проблемы с preload script

## Проблема
Electron API недоступен, хотя Electron обнаружен. Это означает, что preload script не загружается или contextBridge не работает.

## Шаги диагностики

### 1. Проверьте консоль main process (терминал, где запущен Electron)

Должны быть сообщения:
```
Preload script path: E:\Prodject\SilentCaster\SilentCaster-Electron\preload.js
Preload script exists: true
```

Если `Preload script exists: false` - файл не найден по указанному пути.

### 2. Проверьте консоль renderer process (DevTools в Electron)

Должны быть сообщения:
```
[Preload] Preload script started loading...
[Preload] contextBridge available: true
[Preload] ipcRenderer available: true
[Preload] electron-store initialized
[Preload] Preload script loaded
[Preload] Exposing electronAPI...
[Preload] electronAPI exposed successfully
```

Если этих сообщений нет - preload script не выполняется.

### 3. Проверьте ошибки preload

В консоли renderer process (DevTools) должны быть сообщения об ошибках, если они есть:
```
[Preload] Error initializing electron-store: ...
[Preload] Error exposing electronAPI: ...
```

### 4. Возможные причины

1. **Preload script не загружается:**
   - Проверьте путь к preload.js в консоли main process
   - Убедитесь, что файл существует
   - В dev режиме `__dirname` должен указывать на корень проекта

2. **Context isolation не работает:**
   - Убедитесь, что `contextIsolation: true` в webPreferences
   - Убедитесь, что `nodeIntegration: false`

3. **contextBridge ошибка:**
   - Проверьте консоль на наличие ошибок
   - Убедитесь, что все зависимости preload.js доступны

4. **Sandbox режим:**
   - Если используется sandbox, preload должен быть в отдельном процессе
   - Попробуйте установить `sandbox: false` в webPreferences

## Решение

Если preload не загружается, попробуйте:

1. **Проверьте путь:**
   ```javascript
   // В main.js должно быть:
   const preloadPath = path.join(__dirname, 'preload.js');
   console.log('Preload path:', preloadPath);
   console.log('Exists:', fs.existsSync(preloadPath));
   ```

2. **Используйте абсолютный путь:**
   ```javascript
   const preloadPath = path.resolve(__dirname, 'preload.js');
   ```

3. **Проверьте права доступа:**
   - Убедитесь, что файл preload.js доступен для чтения

4. **Перезапустите Electron:**
   - Закройте все окна Electron
   - Запустите заново: `npm run electron:dev`

## Проверка в консоли

В DevTools выполните:
```javascript
// Проверка доступности preload
console.log('electronAPI:', window.electronAPI);
console.log('typeof:', typeof window.electronAPI);

// Проверка window
console.log('Window keys:', Object.keys(window).filter(k => k.includes('electron')));
```

Если `electronAPI` undefined, но Electron обнаружен - проблема в preload script.

