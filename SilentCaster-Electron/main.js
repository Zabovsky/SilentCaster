const { app, BrowserWindow, ipcMain, shell } = require('electron');
const path = require('path');
const fs = require('fs');
const http = require('http');
const tmi = require('tmi.js');

let mainWindow;
let twitchClient = null;

function isDev() {
  // Проверяем наличие dist папки и файла index.html
  const distPath = path.join(__dirname, 'dist', 'index.html');
  const hasDist = fs.existsSync(distPath);
  
  // Если есть собранное приложение, используем его, иначе dev режим
  return !hasDist || process.env.NODE_ENV === 'development';
}

function createWindow() {
  const preloadPath = path.join(__dirname, 'preload.js');
  const preloadPathResolved = path.resolve(__dirname, 'preload.js');
  console.log('[Main Process] Preload script path (join):', preloadPath);
  console.log('[Main Process] Preload script path (resolve):', preloadPathResolved);
  console.log('[Main Process] Preload script exists (join):', fs.existsSync(preloadPath));
  console.log('[Main Process] Preload script exists (resolve):', fs.existsSync(preloadPathResolved));
  console.log('[Main Process] __dirname:', __dirname);
  console.log('[Main Process] process.cwd():', process.cwd());
  
  // Используем resolve для более надежного пути
  const finalPreloadPath = fs.existsSync(preloadPathResolved) ? preloadPathResolved : preloadPath;
  console.log('[Main Process] Using preload path:', finalPreloadPath);
  
  mainWindow = new BrowserWindow({
    width: 1200,
    height: 800,
    webPreferences: {
      nodeIntegration: false,
      contextIsolation: true,
      preload: finalPreloadPath,
      webSecurity: false, // Для разработки
      allowRunningInsecureContent: true, // Разрешить небезопасный контент
      sandbox: false, // Отключаем sandbox для preload script
      // Оптимизация производительности
      enableWebSQL: false,
      enableRemoteModule: false,
      offscreen: false,
      backgroundThrottling: true
    },
    titleBarStyle: 'default',
    frame: true,
    show: false // Не показывать окно до полной загрузки
  });

  // Обработка ошибок preload script
  mainWindow.webContents.on('preload-error', (event, preloadPath, error) => {
    console.error('[Main Process] ❌ Preload script error:', preloadPath);
    console.error('[Main Process] Error details:', error);
  });

  // Проверка загрузки preload
  mainWindow.webContents.on('did-frame-finish-load', (event, isMainFrame) => {
    if (isMainFrame) {
      console.log('[Main Process] Main frame finished loading');
      // Проверяем, что preload загрузился
      mainWindow.webContents.executeJavaScript(`
        (function() {
          if (window.electronAPI) {
            console.log('[Main Process] ✅ electronAPI is available after frame load');
            return true;
          } else {
            console.error('[Main Process] ❌ electronAPI still not available after frame load');
            return false;
          }
        })();
      `).catch(err => {
        console.error('[Main Process] Error checking electronAPI after frame load:', err);
      });
    }
  });

  // Настраиваем CSP для всех режимов (dev и production)
  mainWindow.webContents.session.webRequest.onHeadersReceived((details, callback) => {
    // Получаем текущие заголовки
    const responseHeaders = { ...details.responseHeaders };
    
    // Удаляем старый CSP, если есть
    delete responseHeaders['content-security-policy'];
    delete responseHeaders['Content-Security-Policy'];
    
    // Устанавливаем новый CSP с разрешениями для Twitch API
    responseHeaders['Content-Security-Policy'] = [
      "default-src 'self' 'unsafe-inline' 'unsafe-eval' data: blob: http://localhost:* ws://localhost:* https://id.twitch.tv https://api.twitch.tv; " +
      "connect-src 'self' 'unsafe-inline' http://localhost:* ws://localhost:* wss://localhost:* https://id.twitch.tv https://api.twitch.tv https://*.twitch.tv; " +
      "script-src 'self' 'unsafe-inline' 'unsafe-eval'; " +
      "style-src 'self' 'unsafe-inline'; " +
      "img-src 'self' data: blob: http://localhost:* https://*.twitch.tv https://static-cdn.jtvnw.net; " +
      "font-src 'self' data:;"
    ];
    
    callback({
      responseHeaders: responseHeaders
    });
  });

  // Обработка ошибок загрузки
  mainWindow.webContents.on('did-fail-load', (event, errorCode, errorDescription) => {
    console.error('Failed to load:', errorCode, errorDescription);
    
    // Если не удалось загрузить, показываем ошибку
    if (isDev()) {
      mainWindow.loadURL('data:text/html;charset=utf-8,' + encodeURIComponent(`
        <!DOCTYPE html>
        <html>
        <head>
          <meta charset="utf-8">
          <title>SilentCaster - Ошибка загрузки</title>
          <style>
            body {
              font-family: Arial, sans-serif;
              padding: 40px;
              background: #f5f5f5;
            }
            .error-container {
              background: white;
              padding: 30px;
              border-radius: 8px;
              max-width: 600px;
              margin: 0 auto;
              box-shadow: 0 2px 10px rgba(0,0,0,0.1);
            }
            h1 { color: #e74c3c; }
            code { background: #f4f4f4; padding: 2px 6px; border-radius: 3px; }
          </style>
        </head>
        <body>
          <div class="error-container">
            <h1>⚠️ Ошибка загрузки</h1>
            <p>Не удалось подключиться к Angular dev server.</p>
            <p><strong>Код ошибки:</strong> <code>${errorCode}</code></p>
            <p><strong>Описание:</strong> ${errorDescription}</p>
            <hr>
            <h3>Решение:</h3>
            <ol>
              <li>Убедитесь, что Angular dev server запущен: <code>npm start</code></li>
              <li>Или соберите приложение: <code>npm run build</code></li>
              <li>Затем запустите Electron: <code>npm run electron</code></li>
            </ol>
          </div>
        </body>
        </html>
      `));
    }
  });

  // Когда контент загружен, показываем окно
  mainWindow.webContents.on('did-finish-load', () => {
    console.log('[Main Process] Контент загружен успешно');
    console.log('[Main Process] Preload script path:', path.join(__dirname, 'preload.js'));
    console.log('[Main Process] __dirname:', __dirname);
    console.log('[Main Process] process.cwd():', process.cwd());
    
    // Проверяем доступность electronAPI с задержкой (чтобы preload успел загрузиться)
    setTimeout(() => {
      mainWindow.webContents.executeJavaScript(`
        (function() {
          console.log('[Main Process] Checking electronAPI in renderer...');
          console.log('[Main Process] window.electronAPI:', window.electronAPI);
          console.log('[Main Process] typeof window.electronAPI:', typeof window.electronAPI);
          console.log('[Main Process] window keys:', Object.keys(window).filter(function(k) { return k.includes('electron'); }));
          if (window.electronAPI) {
            console.log('[Main Process] ✅ electronAPI is available!');
            console.log('[Main Process] electronAPI methods:', Object.keys(window.electronAPI));
            return 'available';
          } else {
            console.error('[Main Process] ❌ electronAPI is NOT available!');
            console.error('[Main Process] This might indicate:');
            console.error('[Main Process] 1. Preload script failed to load');
            console.error('[Main Process] 2. Context isolation issue');
            console.error('[Main Process] 3. contextBridge error');
            return 'not available';
          }
        })();
      `).then(result => {
        console.log('[Main Process] electronAPI check result:', result);
        if (result === 'not available') {
          console.error('[Main Process] ⚠️ WARNING: electronAPI is not available in renderer!');
        }
      }).catch(err => {
        console.error('[Main Process] Error checking electronAPI:', err);
      });
    }, 1000);
    
    mainWindow.show();
    if (isDev()) {
      // Открываем DevTools с небольшой задержкой
      setTimeout(() => {
        mainWindow.webContents.openDevTools();
      }, 500);
    }
  });

  // Обработка ошибок консоли (для отладки)
  mainWindow.webContents.on('console-message', (event, level, message) => {
    if (level === 2) { // error level
      console.error('Renderer error:', message);
    }
  });

  // Загрузка контента
  if (isDev()) {
    // В режиме разработки загружаем Angular dev server
    console.log('Загрузка из dev server: http://localhost:4200');
    
    // Функция для проверки доступности dev server
    const checkDevServer = (retries = 15) => {
      let attempts = 0;
      const check = () => {
        attempts++;
        const req = http.get('http://localhost:4200', (res) => {
          console.log('Dev server доступен, загружаем приложение...');
          req.destroy();
          mainWindow.loadURL('http://localhost:4200').catch(err => {
            console.error('Ошибка загрузки dev server:', err);
            mainWindow.show();
          });
        });
        
        req.on('error', (err) => {
          req.destroy();
          if (attempts < retries) {
            console.log(`Dev server еще не готов, попытка ${attempts}/${retries}...`);
            setTimeout(check, 2000);
          } else {
            console.error('Dev server недоступен после всех попыток. Убедитесь, что Angular dev server запущен (npm start)');
            mainWindow.show();
            // Показываем сообщение об ошибке
            mainWindow.loadURL('data:text/html;charset=utf-8,' + encodeURIComponent(`
              <!DOCTYPE html>
              <html>
              <head>
                <meta charset="utf-8">
                <title>SilentCaster - Ошибка</title>
                <style>
                  body {
                    font-family: Arial, sans-serif;
                    padding: 40px;
                    background: #1a1a1a;
                    color: #fff;
                  }
                  .error-container {
                    background: rgba(40, 40, 40, 0.9);
                    padding: 30px;
                    border-radius: 8px;
                    max-width: 600px;
                    margin: 0 auto;
                  }
                  h1 { color: #e74c3c; }
                  code { background: #2a2a2a; padding: 2px 6px; border-radius: 3px; }
                  .btn {
                    background: #3498db;
                    color: white;
                    border: none;
                    padding: 10px 20px;
                    border-radius: 5px;
                    cursor: pointer;
                    margin-top: 20px;
                  }
                </style>
              </head>
              <body>
                <div class="error-container">
                  <h1>⚠️ Angular Dev Server не найден</h1>
                  <p>Не удалось подключиться к Angular dev server на http://localhost:4200</p>
                  <hr>
                  <h3>Решение:</h3>
                  <ol>
                    <li>Откройте терминал в папке проекта</li>
                    <li>Запустите: <code>npm start</code></li>
                    <li>Дождитесь сообщения "listening on localhost:4200"</li>
                    <li>Перезапустите Electron</li>
                  </ol>
                  <button class="btn" onclick="location.reload()">Перезагрузить</button>
                </div>
              </body>
              </html>
            `));
          }
        });
        
        req.setTimeout(3000, () => {
          req.destroy();
          if (attempts < retries) {
            console.log(`Timeout при проверке dev server, попытка ${attempts}/${retries}...`);
            setTimeout(check, 2000);
          } else {
            console.error('Dev server недоступен после всех попыток');
            mainWindow.show();
          }
        });
      };
      check();
    };
    
    // Начинаем проверку через 2 секунды
    setTimeout(() => {
      checkDevServer();
    }, 2000);
  } else {
    // В продакшене загружаем собранное приложение
    const distPath = path.join(__dirname, 'dist', 'index.html');
    console.log('Загрузка из dist:', distPath);
    mainWindow.loadFile(distPath).catch(err => {
      console.error('Ошибка загрузки dist:', err);
    });
  }

  mainWindow.on('closed', () => {
    mainWindow = null;
  });
}

// Оптимизация производительности GPU для уменьшения ошибок tile memory
// Ограничиваем использование памяти тайлов вместо полного отключения GPU
app.commandLine.appendSwitch('max-old-space-size', '4096');
app.commandLine.appendSwitch('disable-gpu-vsync'); // Отключаем вертикальную синхронизацию для лучшей производительности
app.commandLine.appendSwitch('disable-software-rasterizer'); // Используем аппаратное ускорение

app.whenReady().then(() => {
  createWindow();

  app.on('activate', () => {
    if (BrowserWindow.getAllWindows().length === 0) {
      createWindow();
    }
  });
});

app.on('window-all-closed', () => {
  if (process.platform !== 'darwin') {
    app.quit();
  }
});

// IPC обработчики для связи с Angular приложением
ipcMain.handle('app-version', () => {
  return app.getVersion();
});

ipcMain.handle('app-name', () => {
  return app.getName();
});

// Twitch IPC handlers
ipcMain.handle('twitch-connect', async (event, options) => {
  try {
    console.log('Twitch connect requested:', options);
    
    // Отключаем предыдущее подключение, если есть
    if (twitchClient) {
      try {
        await twitchClient.disconnect();
      } catch (err) {
        console.error('Error disconnecting previous client:', err);
      }
      twitchClient = null;
    }

    const clientOptions = {
      options: { debug: false },
      connection: {
        secure: true,
        reconnect: true,
        maxReconnectAttempts: 5
      },
      channels: [options.channel]
    };

    // Если анонимное подключение
    if (options.anonymous) {
      const randomUsername = `justinfan${Math.floor(Math.random() * 1000000)}`;
      clientOptions.identity = {
        username: randomUsername
      };
    } else if (options.username && options.oauthToken) {
      // Подключение с авторизацией
      clientOptions.identity = {
        username: options.username,
        password: options.oauthToken
      };
    } else {
      throw new Error('Не указаны данные для подключения');
    }

    // Создаем клиент
    twitchClient = new tmi.Client(clientOptions);

    // Настраиваем обработчики событий
    twitchClient.on('message', (channel, tags, message, self) => {
      if (self) return;
      
      // Отправляем сообщение в renderer process
      if (mainWindow && !mainWindow.isDestroyed()) {
        mainWindow.webContents.send('twitch-message', {
          channel,
          username: tags['display-name'] || tags.username || 'Unknown',
          message,
          timestamp: new Date().toISOString(),
          userId: tags['user-id'] || tags.id,
          userColor: tags.color || '#FFFFFF',
          isSubscriber: tags.subscriber === true,
          isVip: tags.badges?.vip === '1',
          isModerator: tags.mod === true || tags.badges?.moderator === '1',
          isBroadcaster: tags.badges?.broadcaster === '1',
          isFollower: tags.following === true || tags.following === '1',
          avatarUrl: tags['profile-image-url'] || null
        });
      }
    });

    twitchClient.on('connected', (addr, port) => {
      console.log(`Connected to Twitch: ${addr}:${port}`);
      if (mainWindow && !mainWindow.isDestroyed()) {
        mainWindow.webContents.send('twitch-status', { status: 'connected', message: 'Подключено' });
      }
    });

    twitchClient.on('disconnected', (reason) => {
      console.log(`Disconnected from Twitch: ${reason}`);
      if (mainWindow && !mainWindow.isDestroyed()) {
        mainWindow.webContents.send('twitch-status', { status: 'disconnected', message: 'Отключено' });
      }
    });

    twitchClient.on('join', (channel, username, self) => {
      if (self) {
        console.log(`Joined channel: ${channel}`);
        if (mainWindow && !mainWindow.isDestroyed()) {
          mainWindow.webContents.send('twitch-status', { status: 'joined', channel, message: `Подключено к ${channel}` });
        }
      }
    });

    twitchClient.on('error', (error) => {
      console.error('Twitch client error:', error);
      if (mainWindow && !mainWindow.isDestroyed()) {
        mainWindow.webContents.send('twitch-status', { status: 'error', message: `Ошибка: ${error.message || error}` });
      }
    });

    // Подключаемся
    await twitchClient.connect();
    
    return { success: true, message: 'Подключено к Twitch' };
  } catch (error) {
    console.error('Error connecting to Twitch:', error);
    twitchClient = null;
    return { success: false, message: error.message || 'Ошибка подключения к Twitch' };
  }
});

ipcMain.handle('twitch-disconnect', async () => {
  try {
    console.log('Twitch disconnect requested');
    
    if (twitchClient) {
      await twitchClient.disconnect();
      twitchClient = null;
      
      if (mainWindow && !mainWindow.isDestroyed()) {
        mainWindow.webContents.send('twitch-status', { status: 'disconnected', message: 'Отключено' });
      }
      
      return { success: true, message: 'Отключено от Twitch' };
    }
    
    return { success: true, message: 'Уже отключено' };
  } catch (error) {
    console.error('Error disconnecting from Twitch:', error);
    twitchClient = null;
    return { success: false, message: error.message || 'Ошибка отключения' };
  }
});

// Функция для проверки подключения
function isTwitchConnected() {
  if (!twitchClient) return false;
  try {
    const state = twitchClient.readyState();
    return state === 'OPEN' || state === 'CONNECTING';
  } catch (error) {
    // Если readyState() не доступен, проверяем наличие клиента
    return twitchClient !== null;
  }
}

// Обработчики для отправки сообщений и модерации
ipcMain.handle('twitch-send-message', async (event, channel, message) => {
  try {
    if (!isTwitchConnected()) {
      throw new Error('Не подключено к Twitch');
    }
    await twitchClient.say(channel, message);
    return { success: true };
  } catch (error) {
    console.error('Error sending message:', error);
    return { success: false, message: error.message };
  }
});

ipcMain.handle('twitch-delete-message', async (event, channel, messageId) => {
  try {
    if (!isTwitchConnected()) {
      throw new Error('Не подключено к Twitch');
    }
    await twitchClient.deletemessage(channel, messageId);
    return { success: true };
  } catch (error) {
    console.error('Error deleting message:', error);
    return { success: false, message: error.message };
  }
});

ipcMain.handle('twitch-timeout', async (event, channel, username, duration, reason) => {
  try {
    if (!isTwitchConnected()) {
      throw new Error('Не подключено к Twitch');
    }
    await twitchClient.timeout(channel, username, duration, reason);
    return { success: true };
  } catch (error) {
    console.error('Error timing out user:', error);
    return { success: false, message: error.message };
  }
});

ipcMain.handle('twitch-ban', async (event, channel, username, reason) => {
  try {
    if (!isTwitchConnected()) {
      throw new Error('Не подключено к Twitch');
    }
    await twitchClient.ban(channel, username, reason);
    return { success: true };
  } catch (error) {
    console.error('Error banning user:', error);
    return { success: false, message: error.message };
  }
});

ipcMain.handle('twitch-clear', async (event, channel) => {
  try {
    if (!isTwitchConnected()) {
      throw new Error('Не подключено к Twitch');
    }
    await twitchClient.clear(channel);
    return { success: true };
  } catch (error) {
    console.error('Error clearing chat:', error);
    return { success: false, message: error.message };
  }
});

ipcMain.handle('twitch-whisper', async (event, username, message) => {
  try {
    if (!isTwitchConnected()) {
      throw new Error('Не подключено к Twitch');
    }
    await twitchClient.whisper(username, message);
    return { success: true };
  } catch (error) {
    console.error('Error sending whisper:', error);
    return { success: false, message: error.message };
  }
});

// Открытие внешних ссылок
ipcMain.handle('open-external', (event, url) => {
  shell.openExternal(url);
});

// Вспомогательные функции для работы с Twitch API
async function getChannelId(accessToken, channelName, clientId) {
  try {
    const https = require('https');
    return await new Promise((resolve, reject) => {
      const options = {
        hostname: 'api.twitch.tv',
        path: `/helix/users?login=${channelName}`,
        method: 'GET',
        headers: {
          'Authorization': `Bearer ${accessToken}`,
          'Client-Id': clientId || 'hrufoo6euvbe44l7ja70naqkalufxu'
        }
      };

      const req = https.request(options, (res) => {
        let data = '';
        res.on('data', (chunk) => {
          data += chunk;
        });
        res.on('end', () => {
          try {
            const json = JSON.parse(data);
            if (json.data && json.data.length > 0) {
              resolve(json.data[0].id);
            } else {
              resolve(null);
            }
          } catch (error) {
            reject(error);
          }
        });
      });

      req.on('error', (error) => {
        reject(error);
      });

      req.end();
    });
  } catch (error) {
    console.error('Error getting channel ID:', error);
    return null;
  }
}

async function getUserInfo(accessToken, clientId) {
  try {
    const https = require('https');
    return await new Promise((resolve, reject) => {
      const options = {
        hostname: 'api.twitch.tv',
        path: '/helix/users',
        method: 'GET',
        headers: {
          'Authorization': `Bearer ${accessToken}`,
          'Client-Id': clientId || 'hrufoo6euvbe44l7ja70naqkalufxu'
        }
      };

      const req = https.request(options, (res) => {
        let data = '';
        res.on('data', (chunk) => {
          data += chunk;
        });
        res.on('end', () => {
          try {
            const json = JSON.parse(data);
            if (json.data && json.data.length > 0) {
              resolve(json.data[0]);
            } else {
              resolve(null);
            }
          } catch (error) {
            reject(error);
          }
        });
      });

      req.on('error', (error) => {
        reject(error);
      });

      req.end();
    });
  } catch (error) {
    console.error('Error getting user info:', error);
    return null;
  }
}

// Банворды Twitch
ipcMain.handle('twitch-get-banned-words', async (event, channel) => {
  try {
    const Store = require('electron-store');
    const store = new Store();
    const settings = store.get('settings', {});
    
    if (!settings.twitchOAuthToken) {
      throw new Error('Требуется авторизация Twitch');
    }

    if (!channel) {
      throw new Error('Канал не указан');
    }

    // Получаем ID канала
    const channelId = await getChannelId(settings.twitchOAuthToken, channel, settings.twitchClientId);
    if (!channelId) {
      throw new Error('Не удалось получить ID канала');
    }

    // Получаем ID пользователя (модератора)
    const userInfo = await getUserInfo(settings.twitchOAuthToken, settings.twitchClientId);
    if (!userInfo || !userInfo.id) {
      throw new Error('Не удалось получить информацию о пользователе');
    }

    // Загружаем банворды из Twitch API
    const https = require('https');
    const bannedWords = await new Promise((resolve, reject) => {
      const options = {
        hostname: 'api.twitch.tv',
        path: `/helix/moderation/banned_words?broadcaster_id=${channelId}&moderator_id=${userInfo.id}`,
        method: 'GET',
        headers: {
          'Authorization': `Bearer ${settings.twitchOAuthToken}`,
          'Client-Id': settings.twitchClientId || 'hrufoo6euvbe44l7ja70naqkalufxu'
        }
      };

      const req = https.request(options, (res) => {
        let data = '';
        res.on('data', (chunk) => {
          data += chunk;
        });
        res.on('end', () => {
          try {
            const json = JSON.parse(data);
            if (json.data) {
              const words = json.data.map(item => item.word || item.text || '').filter(w => w);
              resolve(words);
            } else {
              resolve([]);
            }
          } catch (error) {
            console.error('Error parsing banned words response:', error);
            reject(error);
          }
        });
      });

      req.on('error', (error) => {
        console.error('Error fetching banned words:', error);
        reject(error);
      });

      req.end();
    });

    // Сохраняем локально
    const key = `twitch_banned_words_${channel}`;
    store.set(key, bannedWords);

    return bannedWords;
  } catch (error) {
    console.error('Error getting Twitch banned words:', error);
    // Fallback: загружаем из локального хранилища
    try {
      const Store = require('electron-store');
      const store = new Store();
      const key = `twitch_banned_words_${channel}`;
      return store.get(key, []);
    } catch (err) {
      console.error('Error loading from store:', err);
      return [];
    }
  }
});

ipcMain.handle('twitch-save-banned-words', async (event, channel, words) => {
  try {
    const Store = require('electron-store');
    const store = new Store();
    const key = `twitch_banned_words_${channel}`;
    store.set(key, words);
    return { success: true };
  } catch (error) {
    console.error('Error saving banned words:', error);
    return { success: false, message: error.message };
  }
});

ipcMain.handle('twitch-add-banned-word', async (event, channel, word) => {
  try {
    const Store = require('electron-store');
    const store = new Store();
    const settings = store.get('settings', {});
    
    if (!settings.twitchOAuthToken) {
      throw new Error('Требуется авторизация Twitch');
    }

    // Получаем ID канала
    const channelId = await getChannelId(settings.twitchOAuthToken, channel, settings.twitchClientId);
    if (!channelId) {
      throw new Error('Не удалось получить ID канала');
    }

    // Добавляем банворд через Twitch API
    const https = require('https');
    await new Promise((resolve, reject) => {
      const postData = JSON.stringify({
        text: word
      });

      const options = {
        hostname: 'api.twitch.tv',
        path: `/helix/moderation/banned_words?broadcaster_id=${channelId}`,
        method: 'POST',
        headers: {
          'Authorization': `Bearer ${settings.twitchOAuthToken}`,
          'Client-Id': settings.twitchClientId || 'hrufoo6euvbe44l7ja70naqkalufxu',
          'Content-Type': 'application/json',
          'Content-Length': Buffer.byteLength(postData)
        }
      };

      const req = https.request(options, (res) => {
        let data = '';
        res.on('data', (chunk) => {
          data += chunk;
        });
        res.on('end', () => {
          if (res.statusCode >= 200 && res.statusCode < 300) {
            resolve();
          } else {
            reject(new Error(`HTTP ${res.statusCode}: ${data}`));
          }
        });
      });

      req.on('error', (error) => {
        reject(error);
      });

      req.write(postData);
      req.end();
    });

    // Обновляем локальный список
    const key = `twitch_banned_words_${channel}`;
    const currentWords = store.get(key, []);
    if (!currentWords.includes(word)) {
      currentWords.push(word);
      store.set(key, currentWords);
    }

    return { success: true };
  } catch (error) {
    console.error('Error adding banned word:', error);
    return { success: false, message: error.message };
  }
});

ipcMain.handle('twitch-remove-banned-word', async (event, channel, word) => {
  try {
    const Store = require('electron-store');
    const store = new Store();
    const settings = store.get('settings', {});
    
    if (!settings.twitchOAuthToken) {
      throw new Error('Требуется авторизация Twitch');
    }

    // Получаем ID канала
    const channelId = await getChannelId(settings.twitchOAuthToken, channel, settings.twitchClientId);
    if (!channelId) {
      throw new Error('Не удалось получить ID канала');
    }

    // Удаляем банворд через Twitch API
    const https = require('https');
    await new Promise((resolve, reject) => {
      const options = {
        hostname: 'api.twitch.tv',
        path: `/helix/moderation/banned_words?broadcaster_id=${channelId}&word=${encodeURIComponent(word)}`,
        method: 'DELETE',
        headers: {
          'Authorization': `Bearer ${settings.twitchOAuthToken}`,
          'Client-Id': settings.twitchClientId || 'hrufoo6euvbe44l7ja70naqkalufxu'
        }
      };

      const req = https.request(options, (res) => {
        let data = '';
        res.on('data', (chunk) => {
          data += chunk;
        });
        res.on('end', () => {
          if (res.statusCode >= 200 && res.statusCode < 300) {
            resolve();
          } else {
            reject(new Error(`HTTP ${res.statusCode}: ${data}`));
          }
        });
      });

      req.on('error', (error) => {
        reject(error);
      });

      req.end();
    });

    // Обновляем локальный список
    const key = `twitch_banned_words_${channel}`;
    const currentWords = store.get(key, []);
    const updatedWords = currentWords.filter(w => w !== word);
    store.set(key, updatedWords);

    return { success: true };
  } catch (error) {
    console.error('Error removing banned word:', error);
    return { success: false, message: error.message };
  }
});

// OAuth через браузер
let oauthServer = null;
let oauthState = null;
let oauthResolve = null;

ipcMain.handle('twitch-oauth-start', async (event, clientId, redirectUri, scopes) => {
  return new Promise((resolve, reject) => {
    try {
      // Генерируем state для безопасности
      oauthState = Math.random().toString(36).substring(2, 15) + Math.random().toString(36).substring(2, 15);
      
      // Парсим redirect URI
      const url = new URL(redirectUri);
      // Используем порт 3001 для OAuth callback, чтобы не конфликтовать с Angular (4200)
      // ВАЖНО: Если порт не указан в URL, используем 3001
      let port = url.port;
      if (!port || port === '') {
        port = '3001';
      }
      const hostname = url.hostname || 'localhost';
      
      console.log('[OAuth] Received redirectUri:', redirectUri);
      console.log('[OAuth] Parsed URL:', { port, hostname, pathname: url.pathname });
      
      // Создаем локальный сервер для получения callback
      oauthServer = http.createServer((req, res) => {
        const requestUrl = new URL(req.url, `http://${req.headers.host}`);
        
        console.log(`[OAuth] Received request: ${req.method} ${req.url}`);
        console.log(`[OAuth] Pathname: ${requestUrl.pathname}`);
        console.log(`[OAuth] Query: ${requestUrl.search}`);
        
        // Обрабатываем как /auth/callback, так и корневой путь (для совместимости)
        if (requestUrl.pathname === '/auth/callback' || requestUrl.pathname === '/') {
          const code = requestUrl.searchParams.get('code');
          const state = requestUrl.searchParams.get('state');
          const error = requestUrl.searchParams.get('error');
          
          console.log(`[OAuth] Processing callback: code=${code ? 'present' : 'missing'}, state=${state}, error=${error || 'none'}`);
          
          // Отправляем HTML ответ
          res.writeHead(200, { 'Content-Type': 'text/html; charset=utf-8' });
          
          if (error) {
            console.error(`[OAuth] Error from Twitch: ${error}`);
            if (error === 'redirect_mismatch') {
              console.error(`[OAuth] ⚠️ ОШИБКА: redirect_mismatch - проверьте настройки Twitch приложения!`);
              console.error(`[OAuth] Redirect URI должен быть ТОЧНО: http://localhost:3001/auth/callback`);
            }
            res.end(`
              <!DOCTYPE html>
              <html>
              <head>
                <title>Ошибка авторизации</title>
                <style>
                  body {
                    font-family: Arial, sans-serif;
                    display: flex;
                    justify-content: center;
                    align-items: center;
                    height: 100vh;
                    margin: 0;
                    background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
                    color: white;
                  }
                  .container {
                    text-align: center;
                    padding: 2rem;
                    background: rgba(255, 255, 255, 0.1);
                    border-radius: 16px;
                    backdrop-filter: blur(10px);
                  }
                  h1 { margin-top: 0; }
                </style>
              </head>
              <body>
                <div class="container">
                  <h1>❌ Ошибка авторизации</h1>
                  <div style="background: rgba(255, 0, 0, 0.2); border: 1px solid rgba(255, 0, 0, 0.3); border-radius: 8px; padding: 1rem; margin: 1rem 0;">
                    <p><strong>Ошибка:</strong> ${error}</p>
                    ${requestUrl.searchParams.get('error_description') ? `<p><strong>Описание:</strong> ${requestUrl.searchParams.get('error_description')}</p>` : ''}
                  </div>
                  ${error === 'redirect_mismatch' ? `
                    <div style="background: rgba(255, 255, 255, 0.1); border-radius: 8px; padding: 1rem; margin: 1rem 0; text-align: left;">
                      <h3 style="margin-top: 0;">⚠️ Как исправить:</h3>
                      <ol style="margin: 0.5rem 0; padding-left: 1.5rem;">
                        <li>Откройте <a href="https://dev.twitch.tv/console/apps" target="_blank" style="color: #fff; text-decoration: underline;">dev.twitch.tv/console/apps</a></li>
                        <li>Выберите ваше приложение</li>
                        <li>Найдите раздел "OAuth Redirect URLs"</li>
                        <li>Удалите все старые redirect URI</li>
                        <li>Добавьте новый redirect URI:</li>
                      </ol>
                      <code style="background: rgba(0, 0, 0, 0.3); padding: 0.5rem 1rem; border-radius: 4px; display: block; margin: 1rem 0; font-family: 'Courier New', monospace;">http://localhost:3001/auth/callback</code>
                      <p style="margin-top: 1rem; font-size: 0.9rem;">
                        <strong>Важно:</strong> URI должен быть ТОЧНО таким, включая путь <code style="background: rgba(0, 0, 0, 0.3); padding: 0.2rem 0.5rem; border-radius: 4px;">/auth/callback</code>
                      </p>
                    </div>
                  ` : ''}
                  <p>Вы можете закрыть это окно и попробовать снова после исправления настроек.</p>
                </div>
              </body>
              </html>
            `);
            
            if (oauthResolve) {
              oauthResolve({ success: false, error: error });
              oauthResolve = null;
            }
          } else if (code && state === oauthState) {
            res.end(`
              <!DOCTYPE html>
              <html>
              <head>
                <title>Успешная авторизация</title>
                <style>
                  body {
                    font-family: Arial, sans-serif;
                    display: flex;
                    justify-content: center;
                    align-items: center;
                    height: 100vh;
                    margin: 0;
                    background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
                    color: white;
                  }
                  .container {
                    text-align: center;
                    padding: 2rem;
                    background: rgba(255, 255, 255, 0.1);
                    border-radius: 16px;
                    backdrop-filter: blur(10px);
                  }
                  h1 { margin-top: 0; }
                  .checkmark {
                    font-size: 4rem;
                    margin-bottom: 1rem;
                  }
                </style>
              </head>
              <body>
                <div class="container">
                  <div class="checkmark">✅</div>
                  <h1>Авторизация успешна!</h1>
                  <p>Вы можете закрыть это окно и вернуться в приложение.</p>
                </div>
              </body>
              </html>
            `);
            
            if (oauthResolve) {
              oauthResolve({ success: true, code: code });
              oauthResolve = null;
            }
          } else {
            res.end(`
              <!DOCTYPE html>
              <html>
              <head>
                <title>Ошибка</title>
                <style>
                  body {
                    font-family: Arial, sans-serif;
                    display: flex;
                    justify-content: center;
                    align-items: center;
                    height: 100vh;
                    margin: 0;
                    background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
                    color: white;
                  }
                  .container {
                    text-align: center;
                    padding: 2rem;
                    background: rgba(255, 255, 255, 0.1);
                    border-radius: 16px;
                    backdrop-filter: blur(10px);
                  }
                </style>
              </head>
              <body>
                <div class="container">
                  <h1>Ошибка</h1>
                  <p>Неверный код авторизации или state.</p>
                </div>
              </body>
              </html>
            `);
            
            if (oauthResolve) {
              oauthResolve({ success: false, error: 'Invalid code or state' });
              oauthResolve = null;
            }
          }
          
          // Закрываем сервер через 2 секунды
          setTimeout(() => {
            if (oauthServer) {
              oauthServer.close();
              oauthServer = null;
            }
          }, 2000);
        } else {
          res.writeHead(404);
          res.end('Not Found');
        }
      });
      
      oauthServer.listen(port, hostname, () => {
        console.log(`[OAuth] Callback server listening on ${hostname}:${port}`);
        console.log(`[OAuth] Redirect URI: ${redirectUri}`);
        console.log(`[OAuth] Parsed port: ${port}, hostname: ${hostname}`);
        
        // Проверяем, что порт правильный
        if (port !== '3001' && port !== 3001) {
          console.warn(`[OAuth] ⚠️ ВНИМАНИЕ: Используется порт ${port}, но должен быть 3001!`);
          console.warn(`[OAuth] Убедитесь, что в настройках Twitch приложения добавлен redirect URI: http://localhost:3001/auth/callback`);
        }
        
        // Генерируем URL для авторизации
        const authUrl = `https://id.twitch.tv/oauth2/authorize?` +
          `client_id=${clientId || 'hrufoo6euvbe44l7ja70naqkalufxu'}&` +
          `redirect_uri=${encodeURIComponent(redirectUri)}&` +
          `response_type=code&` +
          `scope=${encodeURIComponent(scopes)}&` +
          `state=${oauthState}&` +
          `force_verify=true`;
        
        console.log(`[OAuth] Authorization URL: ${authUrl}`);
        
        // Открываем браузер
        shell.openExternal(authUrl);
        
        // Сохраняем resolve для callback
        oauthResolve = resolve;
      });
      
      oauthServer.on('error', (error) => {
        console.error('OAuth server error:', error);
        if (oauthResolve) {
          oauthResolve({ success: false, error: error.message });
          oauthResolve = null;
        }
        if (oauthServer) {
          oauthServer.close();
          oauthServer = null;
        }
      });
      
      // Таймаут через 5 минут
      setTimeout(() => {
        if (oauthServer) {
          oauthServer.close();
          oauthServer = null;
        }
        if (oauthResolve) {
          oauthResolve({ success: false, error: 'Timeout' });
          oauthResolve = null;
        }
      }, 5 * 60 * 1000);
      
    } catch (error) {
      reject(error);
    }
  });
});

