using System;
using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Forms; // Для Clipboard
using System.Text;

namespace SilentCaster.Services
{
    public class TwitchOAuthService
    {
        // Основной Client ID и зашифрованный Secret (base64 + salt)
        public const string DefaultClientId = "hrufoo6euvbe44l7ja70naqkalufxu";
        private const string EncodedSecret = "aXJoOTV1ZHQxdWhpc3ZwNjVlM2o4Y3RtZDB6Z2JxOk15U2FsdF9TYWZl"; // "irh95udt1uhisvp65e3j8ctmd0zgbq:MySalt_Safe" -> base64
        private const string SecretSalt = "MySalt_Safe";

        private readonly string _clientId;
        private readonly string _clientSecret;
        private readonly string _scopes;

        public TwitchOAuthService(string? clientId = null, string? clientSecret = null, string scopes = "channel:read:redemptions")
        {
            _clientId = clientId ?? DefaultClientId;
            _clientSecret = clientSecret ?? DecodeSecret(EncodedSecret, SecretSalt);
            _scopes = scopes;
        }

        public static string EncodeSecret(string secret, string salt)
        {
            var bytes = Encoding.UTF8.GetBytes(secret + ":" + salt);
            return Convert.ToBase64String(bytes);
        }
        public static string DecodeSecret(string encoded, string salt)
        {
            var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(encoded));
            if (decoded.EndsWith(":" + salt))
                return decoded.Substring(0, decoded.Length - (":" + salt).Length);
            return string.Empty;
        }

        public async Task<DeviceFlowInfo> RequestDeviceCodeAsync()
        {
            using var http = new HttpClient();
            var deviceRequest = new HttpRequestMessage(HttpMethod.Post, "https://id.twitch.tv/oauth2/device")
            {
                Content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("client_id", _clientId),
                    new KeyValuePair<string, string>("scope", _scopes)
                })
            };
            var deviceResponse = await http.SendAsync(deviceRequest);
            var deviceJson = await deviceResponse.Content.ReadAsStringAsync();
            var deviceData = JsonDocument.Parse(deviceJson).RootElement;
            var info = new DeviceFlowInfo
            {
                DeviceCode = deviceData.GetProperty("device_code").GetString() ?? string.Empty,
                UserCode = deviceData.GetProperty("user_code").GetString() ?? string.Empty,
                VerificationUri = deviceData.GetProperty("verification_uri").GetString() ?? string.Empty,
                Interval = deviceData.GetProperty("interval").GetInt32()
            };
            try { System.Windows.Forms.Clipboard.SetText(info.UserCode); } catch { }
            return info;
        }

        public async Task<string?> PollDeviceTokenAsync(string deviceCode, int interval)
        {
            using var http = new HttpClient();
            while (true)
            {
                await Task.Delay(interval * 1000);
                var tokenRequest = new HttpRequestMessage(HttpMethod.Post, "https://id.twitch.tv/oauth2/token")
                {
                    Content = new FormUrlEncodedContent(new[]
                    {
                        new KeyValuePair<string, string>("client_id", _clientId),
                        new KeyValuePair<string, string>("client_secret", _clientSecret),
                        new KeyValuePair<string, string>("device_code", deviceCode),
                        new KeyValuePair<string, string>("grant_type", "urn:ietf:params:oauth:grant-type:device_code")
                    })
                };
                var tokenResponse = await http.SendAsync(tokenRequest);
                var tokenJson = await tokenResponse.Content.ReadAsStringAsync();
                var tokenData = JsonDocument.Parse(tokenJson).RootElement;
                if (tokenData.TryGetProperty("access_token", out var accessTokenProp))
                    return accessTokenProp.GetString();
                if (tokenData.TryGetProperty("error", out var errorProp) && errorProp.GetString() != "authorization_pending")
                    throw new Exception($"Ошибка авторизации: {tokenJson}");
            }
        }
    }

    public class DeviceFlowInfo
    {
        public string DeviceCode { get; set; } = string.Empty;
        public string UserCode { get; set; } = string.Empty;
        public string VerificationUri { get; set; } = string.Empty;
        public int Interval { get; set; }
    }
} 