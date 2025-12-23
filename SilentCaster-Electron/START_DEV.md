# Инструкция по запуску в режиме разработки

## Проблема: "Cannot GET /" при запуске

Если вы видите ошибку "Cannot GET /" при запуске `npm run electron:dev`, это означает, что Angular dev server не успел запуститься до того, как Electron попытался подключиться.

## Решения

### Вариант 1: Ручной запуск (рекомендуется)

1. **Откройте первый терминал** и запустите Angular dev server:
   ```bash
   cd SilentCaster-Electron
   npm start
   ```

2. **Дождитесь сообщения:**
   ```
   ** Angular Live Development Server is listening on localhost:4200, open your browser on http://localhost:4200/ **
   ```

3. **Откройте второй терминал** и запустите Electron:
   ```bash
   cd SilentCaster-Electron
   npm run electron
   ```

### Вариант 2: Использование bat-файла (Windows)

Запустите файл `start-dev.bat`:
```bash
cd SilentCaster-Electron
start-dev.bat
```

Этот скрипт автоматически:
- Запустит Angular dev server в отдельном окне
- Подождет 15 секунд
- Запустит Electron

### Вариант 3: Улучшенный скрипт npm

Попробуйте использовать улучшенный скрипт:
```bash
npm run electron:dev
```

Если это не работает, используйте ручной запуск (Вариант 1).

## Проверка работы Angular dev server

Чтобы проверить, что Angular dev server работает:

1. Запустите `npm start`
2. Откройте браузер и перейдите на http://localhost:4200
3. Если вы видите приложение - dev server работает правильно
4. Если вы видите "Cannot GET /" - есть проблема с конфигурацией Angular

## Возможные проблемы

### Проблема: Порт 4200 занят

**Решение:**
```bash
# Найдите процесс, использующий порт 4200
netstat -ano | findstr :4200

# Завершите процесс (замените PID на реальный ID процесса)
taskkill /PID <PID> /F
```

### Проблема: Angular не компилируется

**Решение:**
```bash
# Очистите кэш и переустановите зависимости
rm -rf node_modules package-lock.json
npm install --legacy-peer-deps

# Попробуйте собрать проект
npm run build
```

### Проблема: Electron не подключается к dev server

**Решение:**
1. Убедитесь, что Angular dev server запущен и доступен на http://localhost:4200
2. Проверьте, что в `main.js` правильно настроен URL для dev режима
3. Проверьте консоль Electron (DevTools) на наличие ошибок

## Логи и отладка

Для отладки проверьте:
- Консоль терминала, где запущен `npm start` - там должны быть логи Angular
- Консоль Electron (DevTools) - там должны быть ошибки, если они есть
- Консоль терминала, где запущен Electron - там могут быть логи Electron

