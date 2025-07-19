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
                    // 创建默认响应
                    _responses = new List<QuickResponse>
                    {
                        new QuickResponse
                        {
                            Trigger = "привет",
                            Responses = new List<string>
                            {
                                "Приветики, {username}!",
                                "Рад тебя видеть, {username}!"
                            }
                        },
                        new QuickResponse
                        {
                            Trigger = "*",
                            Responses = new List<string>
                            {
                                "Спасибо, {username}!",
                                "Ты крут, {username}!"
                            }
                        }
                    };
                    SaveResponses();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载响应文件错误: {ex.Message}");
                _responses = new List<QuickResponse>();
            }
        }

        private void SaveResponses()
        {
            try
            {
                var json = JsonConvert.SerializeObject(_responses, Formatting.Indented);
                File.WriteAllText(ResponsesFile, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"保存响应文件错误: {ex.Message}");
            }
        }
    }
} 