# Исправление зависимостей

## Проблема
Была ошибка несовместимости версий:
```
Error: The current version of "@angular/build" supports Angular versions ^21.0.0,
but detected Angular version 17.3.12 instead.
```

## Решение

### 1. Приведены все версии к Angular 17.3.x
- Все пакеты Angular обновлены до версии `^17.3.0`
- `@angular-devkit/build-angular` установлен в версии `17.3.17`
- Добавлен `typescript` версии `~5.4.0` в devDependencies

### 2. Очистка и переустановка
```bash
# Удалить старые зависимости
Remove-Item -Recurse -Force node_modules,package-lock.json

# Установить с флагом legacy-peer-deps
npm install --legacy-peer-deps
```

### 3. Удален postinstall скрипт
Убран `"postinstall": "ng version"` из package.json, чтобы избежать лишних предупреждений.

## Текущие версии

### Angular
- Angular CLI: 17.3.17
- Angular Core: 17.3.12
- Build Angular: 17.3.17

### Другие зависимости
- TypeScript: ~5.4.0
- Electron: ^28.0.0
- RxJS: ~7.8.0
- Zone.js: ~0.14.0

## Предупреждения

### Node.js версия
Есть предупреждение о версии Node.js (22.16.0 не официально поддерживается Angular 17), но это не критично - приложение должно работать.

### Deprecated API
Предупреждение `util._extend` deprecated - это из старых зависимостей, не влияет на работу приложения.

## Запуск

Теперь приложение должно запускаться без ошибок:
```bash
npm run electron:dev
```

