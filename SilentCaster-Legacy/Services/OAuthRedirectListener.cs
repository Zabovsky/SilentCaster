using System;
using System.Net;
using System.Threading.Tasks;

namespace SilentCaster.Services
{
    public class OAuthRedirectListener : IDisposable
    {
        private readonly HttpListener _listener;
        private readonly string _redirectUrl;
        private bool _isListening;

        public OAuthRedirectListener(string redirectUrl = "http://localhost:3000/")
        {
            _redirectUrl = redirectUrl;
            _listener = new HttpListener();
            _listener.Prefixes.Add(_redirectUrl);
        }

        public async Task<string?> WaitForCodeAsync()
        {
            _isListening = true;
            _listener.Start();
            try
            {
                var context = await _listener.GetContextAsync();
                var request = context.Request;
                var response = context.Response;

                // Парсим code из URL
                var query = request.Url?.Query;
                var code = System.Web.HttpUtility.ParseQueryString(query ?? string.Empty)["code"];

                // Отправляем ответ пользователю
                string responseString = "<html><body>Авторизация завершена, окно можно закрыть.</body></html>";
                byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
                response.ContentLength64 = buffer.Length;
                await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                response.OutputStream.Close();

                return code;
            }
            finally
            {
                _isListening = false;
                _listener.Stop();
            }
        }

        public void Dispose()
        {
            if (_isListening)
                _listener.Stop();
            _listener.Close();
        }
    }
} 