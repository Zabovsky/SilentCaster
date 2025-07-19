using System;
using System.Threading.Tasks;
using TwitchLib.Client;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Clients;
using TwitchLib.Communication.Models;
using SilentCaster.Models;
using ChatMessageModel = SilentCaster.Models.ChatMessage;

namespace SilentCaster.Services
{
    public class TwitchService
    {
        private TwitchClient? _client;
        public event EventHandler<ChatMessageModel>? MessageReceived;
        public event EventHandler<string>? ConnectionStatusChanged;

        public async Task ConnectAsync(string username, string oauthToken, string channel)
        {
            try
            {
                ConnectionStatusChanged?.Invoke(this, "Подключение...");

                var credentials = new ConnectionCredentials(username, oauthToken);
                var clientOptions = new ClientOptions();

                var webSocketClient = new WebSocketClient(clientOptions);
                _client = new TwitchClient(webSocketClient);
                _client.Initialize(credentials, channel);

                SetupClientEvents();

                await Task.Run(() => _client.Connect());
            }
            catch (Exception ex)
            {
                ConnectionStatusChanged?.Invoke(this, $"Ошибка подключения: {ex.Message}");
            }
        }

        public async Task ConnectAnonymouslyAsync(string channel)
        {
            try
            {
                ConnectionStatusChanged?.Invoke(this, "Подключение...");

                // Используем случайное имя для анонимного подключения
                var randomUsername = $"justinfan{new Random().Next(100000, 999999)}";
                var credentials = new ConnectionCredentials(randomUsername, "SCHMOCKTOKEN");
                var clientOptions = new ClientOptions();

                var webSocketClient = new WebSocketClient(clientOptions);
                _client = new TwitchClient(webSocketClient);
                _client.Initialize(credentials, channel);

                SetupClientEvents();

                await Task.Run(() => _client.Connect());
            }
            catch (Exception ex)
            {
                ConnectionStatusChanged?.Invoke(this, $"Ошибка подключения: {ex.Message}");
                throw;
            }
        }

        private void SetupClientEvents()
        {
            if (_client == null) return;

            _client.OnMessageReceived += (sender, e) =>
            {
                var chatMessage = new ChatMessageModel
                {
                    Username = e.ChatMessage.Username,
                    Message = e.ChatMessage.Message,
                    Timestamp = DateTime.Now
                };
                MessageReceived?.Invoke(this, chatMessage);
            };

            _client.OnConnected += (sender, e) =>
            {
                ConnectionStatusChanged?.Invoke(this, "Подключено");
            };

            _client.OnDisconnected += (sender, e) =>
            {
                ConnectionStatusChanged?.Invoke(this, "Отключено");
            };
        }

        public void Disconnect()
        {
            _client?.Disconnect();
        }

        public bool IsConnected => _client?.IsConnected ?? false;
    }
} 