# Решение проблем

## Проблема: CSP ошибки и 404 при загрузке

### Симптомы
```
Connecting to 'http://localhost:4200/.well-known/appspecific/com.chrome.devtools.json' violates CSP
Cannot GET /
```

### Решение

1. **Проверьте, что Angular dev server запущен:**
   ```bash
   npm start
   ```
   Должен быть доступен на http://localhost:4200

2. **Запустите Electron после запуска dev server:**
   ```bash
   # В отдельном терминале
   npm run electron
   ```

3. **Или используйте автоматический запуск:**
   ```bash
   npm run electron:dev
   ```
   Этот скрипт автоматически запустит оба процесса.

### Исправления в коде

1. **baseHref для dev режима:**
   - В `src/index.html`: `<base href="/">` (для dev)
   - В `angular.json` production: `"baseHref": "./"` (для production)

2. **CSP настройки в Electron:**
   - Добавлены разрешения для localhost
   - Разрешены unsafe-inline и unsafe-eval для разработки

3. **Задержка загрузки:**
   - Добавлена задержка 1 секунда перед загрузкой URL
   - Это дает время dev server запуститься

## Другие проблемы

### Проблема: Белый экран

**Решение:**
1. Проверьте консоль Electron (DevTools)
2. Убедитесь, что Angular dev server запущен
3. Проверьте, что порт 4200 не занят другим процессом

### Проблема: Модули не найдены

**Решение:**
```bash
# Переустановите зависимости
rm -rf node_modules package-lock.json
npm install --legacy-peer-deps
```

### Проблема: TypeScript ошибки

**Решение:**
Убедитесь, что TypeScript версии 5.4.x:
```bash
npm list typescript
```

Если версия неправильная:
```bash
npm install --save-dev typescript@~5.4.2 --legacy-peer-deps
```

## Порядок запуска

### Правильный порядок:
1. Запустите `npm start` (Angular dev server)
2. Дождитесь сообщения "Angular Live Development Server is listening on localhost:4200"
3. В другом терминале запустите `npm run electron`

### Или используйте автоматический запуск:
```bash
npm run electron:dev
```

Это запустит оба процесса автоматически с правильной последовательностью.

