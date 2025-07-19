using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SilentCaster.Services
{
    public class OBSService : IDisposable
    {
        private ClientWebSocket? _webSocket;
        private readonly object _lockObject = new object();
        private bool _disposed = false;
        private bool _isConnected = false;
        private string _obsUrl = "ws://localhost:4444";
        private string _obsPassword = "";
        private CancellationTokenSource? _cancellationTokenSource;

        public event EventHandler<string>? ConnectionStatusChanged;
        public event EventHandler<string>? ErrorOccurred;

        public bool IsEnabled { get; set; } = false;
        public bool IsConnected => _isConnected;

        public async Task<bool> ConnectAsync(string url = "ws://localhost:4444", string password = "")
        {
            if (!IsEnabled) return false;

            try
            {
                _obsUrl = url;
                _obsPassword = password;

                lock (_lockObject)
                {
                    if (_disposed) return false;
                    
                    _webSocket?.Dispose();
                    _webSocket = new ClientWebSocket();
                    _cancellationTokenSource = new CancellationTokenSource();
                }

                await _webSocket.ConnectAsync(new Uri(_obsUrl), _cancellationTokenSource.Token);
                
                // Аутентификация, если указан пароль
                if (!string.IsNullOrEmpty(_obsPassword))
                {
                    var authResult = await AuthenticateAsync();
                    if (!authResult)
                    {
                        await DisconnectAsync();
                        return false;
                    }
                }

                _isConnected = true;
                ConnectionStatusChanged?.Invoke(this, "Подключено к OBS");
                
                // Запускаем прослушивание сообщений
                _ = Task.Run(ListenForMessagesAsync);
                
                return true;
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, $"Ошибка подключения к OBS: {ex.Message}");
                return false;
            }
        }

        public async Task DisconnectAsync()
        {
            try
            {
                lock (_lockObject)
                {
                    if (_webSocket == null) return;
                    
                    _cancellationTokenSource?.Cancel();
                }

                if (_webSocket.State == WebSocketState.Open)
                {
                    await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Disconnect", CancellationToken.None);
                }
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, $"Ошибка отключения от OBS: {ex.Message}");
            }
            finally
            {
                _isConnected = false;
                ConnectionStatusChanged?.Invoke(this, "Отключено от OBS");
            }
        }

        private async Task<bool> AuthenticateAsync()
        {
            try
            {
                // Получаем challenge от OBS
                var getAuthRequired = await SendRequestAsync("GetAuthRequired");
                if (getAuthRequired == null) return false;

                var authRequired = JObject.Parse(getAuthRequired);
                if (!authRequired["authRequired"]?.Value<bool>() == true)
                {
                    return true; // Аутентификация не требуется
                }

                var challenge = authRequired["challenge"]?.Value<string>();
                var salt = authRequired["salt"]?.Value<string>();

                if (string.IsNullOrEmpty(challenge) || string.IsNullOrEmpty(salt))
                {
                    return false;
                }

                // Генерируем ответ на challenge
                var secret = GenerateSecret(_obsPassword, salt);
                var authResponse = GenerateAuthResponse(secret, challenge);

                var authRequest = new
                {
                    requestType = "Authenticate",
                    auth = authResponse
                };

                var authResult = await SendRequestAsync(JsonConvert.SerializeObject(authRequest));
                if (authResult == null) return false;

                var authResponseObj = JObject.Parse(authResult);
                return authResponseObj["status"]?.Value<string>() == "ok";
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, $"Ошибка аутентификации OBS: {ex.Message}");
                return false;
            }
        }

        private string GenerateSecret(string password, string salt)
        {
            // Простая реализация генерации секрета
            // В реальном приложении нужно использовать SHA256
            return password + salt;
        }

        private string GenerateAuthResponse(string secret, string challenge)
        {
            // Простая реализация генерации ответа
            // В реальном приложении нужно использовать SHA256
            return secret + challenge;
        }

        public async Task<bool> SetTextAsync(string sourceName, string text)
        {
            if (!IsEnabled || !_isConnected) return false;

            try
            {
                var request = new
                {
                    requestType = "SetTextGDIPlusProperties",
                    sourceName = sourceName,
                    text = text
                };

                var response = await SendRequestAsync(JsonConvert.SerializeObject(request));
                if (response == null) return false;

                var responseObj = JObject.Parse(response);
                return responseObj["status"]?.Value<string>() == "ok";
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, $"Ошибка установки текста в OBS: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> ShowSourceAsync(string sourceName)
        {
            if (!IsEnabled || !_isConnected) return false;

            try
            {
                var request = new
                {
                    requestType = "SetSceneItemProperties",
                    sceneName = await GetCurrentSceneAsync(),
                    item = new { name = sourceName },
                    visible = true
                };

                var response = await SendRequestAsync(JsonConvert.SerializeObject(request));
                if (response == null) return false;

                var responseObj = JObject.Parse(response);
                return responseObj["status"]?.Value<string>() == "ok";
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, $"Ошибка показа источника в OBS: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> HideSourceAsync(string sourceName)
        {
            if (!IsEnabled || !_isConnected) return false;

            try
            {
                var request = new
                {
                    requestType = "SetSceneItemProperties",
                    sceneName = await GetCurrentSceneAsync(),
                    item = new { name = sourceName },
                    visible = false
                };

                var response = await SendRequestAsync(JsonConvert.SerializeObject(request));
                if (response == null) return false;

                var responseObj = JObject.Parse(response);
                return responseObj["status"]?.Value<string>() == "ok";
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, $"Ошибка скрытия источника в OBS: {ex.Message}");
                return false;
            }
        }

        private async Task<string?> GetCurrentSceneAsync()
        {
            try
            {
                var response = await SendRequestAsync("GetCurrentScene");
                if (response == null) return null;

                var responseObj = JObject.Parse(response);
                return responseObj["name"]?.Value<string>();
            }
            catch
            {
                return null;
            }
        }

        private async Task<string?> SendRequestAsync(string request)
        {
            if (_webSocket?.State != WebSocketState.Open) return null;

            try
            {
                var buffer = Encoding.UTF8.GetBytes(request);
                await _webSocket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, _cancellationTokenSource?.Token ?? CancellationToken.None);

                // Ждем ответ
                var responseBuffer = new byte[4096];
                var result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(responseBuffer), _cancellationTokenSource?.Token ?? CancellationToken.None);
                
                if (result.MessageType == WebSocketMessageType.Text)
                {
                    return Encoding.UTF8.GetString(responseBuffer, 0, result.Count);
                }
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, $"Ошибка отправки запроса в OBS: {ex.Message}");
            }

            return null;
        }

        private async Task ListenForMessagesAsync()
        {
            var buffer = new byte[4096];

            try
            {
                while (_webSocket?.State == WebSocketState.Open)
                {
                    var result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), _cancellationTokenSource?.Token ?? CancellationToken.None);
                    
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        break;
                    }
                    
                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        // Обрабатываем входящие сообщения от OBS
                        System.Diagnostics.Debug.WriteLine($"OBS сообщение: {message}");
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, $"Ошибка прослушивания OBS: {ex.Message}");
            }
            finally
            {
                _isConnected = false;
                ConnectionStatusChanged?.Invoke(this, "Соединение с OBS потеряно");
            }
        }

        public void Dispose()
        {
            lock (_lockObject)
            {
                if (_disposed) return;
                
                _disposed = true;
                _cancellationTokenSource?.Cancel();
                _webSocket?.Dispose();
                _cancellationTokenSource?.Dispose();
            }
        }
    }
} 