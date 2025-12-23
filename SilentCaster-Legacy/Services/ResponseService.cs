using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using SilentCaster.Models;

namespace SilentCaster.Services
{
    public class ResponseService
    {
        private const string ResponsesFile = "Responses.json";
        private List<QuickResponse> _responses;

        public ResponseService()
        {
            _responses = new List<QuickResponse>();
            LoadResponses();
        }

        public List<QuickResponse> GetAllResponses()
        {
            return _responses.ToList();
        }

        public List<string> GetResponsesForMessage(string message)
        {
            var responses = new List<string>();

            // 查找匹配的触发器
            foreach (var response in _responses)
            {
                if (response.Trigger == "*" || 
                    message.ToLower().Contains(response.Trigger.ToLower()))
                {
                    responses.AddRange(response.Responses);
                }
            }

            return responses.Distinct().ToList();
        }

        public List<string> GetPersonalResponsesForMessage(string message)
        {
            var responses = new List<string>();

            // Ищем только персональные ответы (IsQuickResponse == false)
            foreach (var response in _responses.Where(r => !r.IsQuickResponse))
            {
                if (response.Trigger == "*" || 
                    message.ToLower().Contains(response.Trigger.ToLower()))
                {
                    responses.AddRange(response.Responses);
                }
            }

            return responses.Distinct().ToList();
        }

        public void AddResponse(QuickResponse response)
        {
            _responses.Add(response);
            SaveResponses();
        }

        public void RemoveResponse(int index)
        {
            if (index >= 0 && index < _responses.Count)
            {
                _responses.RemoveAt(index);
                SaveResponses();
            }
        }

        public void ClearResponses()
        {
            _responses.Clear();
            SaveResponses();
        }

        public List<QuickResponse> GetQuickResponses()
        {
            return _responses.Where(r => r.IsQuickResponse).ToList();
        }

        public List<QuickResponse> GetPersonalResponses()
        {
            return _responses.Where(r => !r.IsQuickResponse).ToList();
        }

        public void UpdateAllResponses(List<QuickResponse> newResponses)
        {
            _responses.Clear();
            _responses.AddRange(newResponses);
            SaveResponses();
        }

        private void LoadResponses()
        {
            try
            {
                if (File.Exists(ResponsesFile))
                {
                    var json = File.ReadAllText(ResponsesFile);
                    _responses = JsonConvert.DeserializeObject<List<QuickResponse>>(json) ?? new List<QuickResponse>();
                }
                else
                {
                    // Создаем примеры быстрых и персональных ответов
                    _responses = new List<QuickResponse>
                    {
                        new QuickResponse { Trigger = "привет", Responses = new List<string>{"Привет всем!", "Приветики!"}, IsQuickResponse = true },
                        new QuickResponse { Trigger = "спасибо", Responses = new List<string>{"Пожалуйста!", "Рад помочь!"}, IsQuickResponse = true },
                        new QuickResponse { Trigger = "follow", Responses = new List<string>{"Спасибо за фоллоу, {username}!"}, IsQuickResponse = false },
                        new QuickResponse { Trigger = "привет, {username}", Responses = new List<string>{"Привет, {username}!"}, IsQuickResponse = false }
                    };
                    SaveResponses();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки ответов: {ex.Message}");
                _responses = new List<QuickResponse>();
            }
        }

        public void SaveResponses()
        {
            try
            {
                var json = JsonConvert.SerializeObject(_responses, Formatting.Indented);
                File.WriteAllText(ResponsesFile, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка сохранения ответов: {ex.Message}");
            }
        }
    }
} 