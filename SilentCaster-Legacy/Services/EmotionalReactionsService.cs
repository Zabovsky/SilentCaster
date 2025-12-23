using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using SilentCaster.Models;

namespace SilentCaster.Services
{
    public class EmotionalReactionsService
    {
        private const string EmotionalReactionsFile = "EmotionalReactions.json";
        private List<EmotionalReaction> _emotionalReactions;
        private readonly Random _random;
        private bool _globalEnabled = true; // Глобальное отключение эмоций

        public EmotionalReactionsService()
        {
            _emotionalReactions = new List<EmotionalReaction>();
            _random = new Random();
            LoadEmotionalReactions();
        }

        public bool GlobalEnabled
        {
            get => _globalEnabled;
            set
            {
                _globalEnabled = value;
                SaveEmotionalReactions();
            }
        }

        public List<EmotionalReaction> GetAllReactions()
        {
            return _emotionalReactions.ToList();
        }

        public EmotionalReaction? GetReactionForMessage(string message, string? username = null)
        {
            // Проверяем глобальное отключение
            if (!_globalEnabled)
                return null;

            if (string.IsNullOrEmpty(message))
                return null;

            var messageLower = message.ToLower();
            var availableReactions = _emotionalReactions
                .Where(r => r.IsEnabled && r.Triggers.Any())
                .Where(r => r.Triggers.Any(t => messageLower.Contains(t.ToLower())))
                .Where(r => CanUseReaction(r))
                .Where(r => IsUserAllowed(r, username))
                .OrderBy(r => r.Priority)
                .ToList();

            foreach (var reaction in availableReactions)
            {
                if (_random.NextDouble() * 100 <= reaction.UsageChance)
                {
                    reaction.LastUsed = DateTime.Now;
                    return reaction;
                }
            }

            return availableReactions.FirstOrDefault();
        }

        private bool CanUseReaction(EmotionalReaction reaction)
        {
            if (reaction.CooldownSeconds <= 0)
                return true;

            if (reaction.LastUsed == null)
                return true;

            var timeSinceLastUse = DateTime.Now - reaction.LastUsed.Value;
            return timeSinceLastUse.TotalSeconds >= reaction.CooldownSeconds;
        }

        private bool IsUserAllowed(EmotionalReaction reaction, string? username)
        {
            // Если фильтр пользователей не включен, разрешаем всем
            if (!reaction.UseUserFilter)
                return true;

            // Если имя пользователя не указано, разрешаем
            if (string.IsNullOrEmpty(username))
                return true;

            var usernameLower = username.ToLower();

            // Проверяем заблокированных пользователей
            if (reaction.BlockedUsers.Any(u => u.ToLower() == usernameLower))
                return false;

            // Если разрешены все пользователи, кроме заблокированных
            if (reaction.AllowAllUsers)
                return true;

            // Проверяем разрешенных пользователей
            return reaction.AllowedUsers.Any(u => u.ToLower() == usernameLower);
        }

        public void AddReaction(EmotionalReaction reaction)
        {
            if (!_emotionalReactions.Any(r => r.Id == reaction.Id))
            {
                _emotionalReactions.Add(reaction);
                SaveEmotionalReactions();
            }
        }

        public void UpdateReaction(EmotionalReaction reaction)
        {
            var existingIndex = _emotionalReactions.FindIndex(r => r.Id == reaction.Id);
            if (existingIndex >= 0)
            {
                _emotionalReactions[existingIndex] = reaction;
                SaveEmotionalReactions();
            }
        }

        public void RemoveReaction(string id)
        {
            var reaction = _emotionalReactions.FirstOrDefault(r => r.Id == id);
            if (reaction != null)
            {
                _emotionalReactions.Remove(reaction);
                SaveEmotionalReactions();
            }
        }

        private void LoadEmotionalReactions()
        {
            try
            {
                if (File.Exists(EmotionalReactionsFile))
                {
                    var json = File.ReadAllText(EmotionalReactionsFile);
                    var data = JsonConvert.DeserializeObject<EmotionalReactionsData>(json);
                    
                    if (data != null)
                    {
                        _emotionalReactions = data.Reactions ?? new List<EmotionalReaction>();
                        _globalEnabled = data.GlobalEnabled;
                    }
                    else
                    {
                        _emotionalReactions = new List<EmotionalReaction>();
                        _globalEnabled = true;
                    }
                }
                else
                {
                    // Создаем предустановленные эмоциональные реакции
                    _emotionalReactions = new List<EmotionalReaction>
                    {
                        new EmotionalReaction
                        {
                            Name = "Смех",
                            Description = "Реакция на смешные сообщения",
                            Emotion = EmotionType.Laugh,
                            Triggers = new List<string> { "хаха", "лол", "кек", "смешно", "забавно", "прикол", "анекдот", "шутка", "юмор", "весело" },
                            Responses = new List<string> { "Хахаха!", "Смешно!", "Лол!", "Кек!", "Забавно!", "Хорошая шутка!", "Весело!" },
                            Priority = 1,
                            UsageChance = 80.0,
                            CooldownSeconds = 30
                        },
                        new EmotionalReaction
                        {
                            Name = "Удивление",
                            Description = "Реакция на удивительные сообщения",
                            Emotion = EmotionType.Surprised,
                            Triggers = new List<string> { "вау", "ого", "ух ты", "невероятно", "потрясающе", "удивительно", "впечатляет" },
                            Responses = new List<string> { "Вау!", "Ого!", "Ух ты!", "Невероятно!", "Потрясающе!", "Впечатляет!" },
                            Priority = 2,
                            UsageChance = 70.0,
                            CooldownSeconds = 45
                        },
                        new EmotionalReaction
                        {
                            Name = "Любовь",
                            Description = "Реакция на милые и любящие сообщения",
                            Emotion = EmotionType.Love,
                            Triggers = new List<string> { "люблю", "милый", "красивый", "прекрасный", "обожаю", "симпатичный", "мило" },
                            Responses = new List<string> { "Люблю вас всех!", "Мило!", "Красиво!", "Прекрасно!", "Обожаю!", "Симпатично!" },
                            Priority = 3,
                            UsageChance = 60.0,
                            CooldownSeconds = 60
                        },
                        new EmotionalReaction
                        {
                            Name = "Гордость",
                            Description = "Реакция на достижения и успехи",
                            Emotion = EmotionType.Proud,
                            Triggers = new List<string> { "молодец", "отлично", "супер", "великолепно", "превосходно", "блестяще", "победа" },
                            Responses = new List<string> { "Молодец!", "Отлично!", "Супер!", "Великолепно!", "Превосходно!", "Блестяще!" },
                            Priority = 4,
                            UsageChance = 75.0,
                            CooldownSeconds = 40
                        },
                        new EmotionalReaction
                        {
                            Name = "Возбуждение",
                            Description = "Реакция на захватывающие моменты",
                            Emotion = EmotionType.Excited,
                            Triggers = new List<string> { "круто", "эпично", "потрясающе", "захватывающе", "невероятно", "фантастически" },
                            Responses = new List<string> { "Круто!", "Эпично!", "Потрясающе!", "Захватывающе!", "Невероятно!", "Фантастически!" },
                            Priority = 5,
                            UsageChance = 65.0,
                            CooldownSeconds = 50
                        }
                    };
                    _globalEnabled = true;
                    SaveEmotionalReactions();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки эмоциональных реакций: {ex.Message}");
                _emotionalReactions = new List<EmotionalReaction>();
                _globalEnabled = true;
            }
        }

        private void SaveEmotionalReactions()
        {
            try
            {
                var data = new EmotionalReactionsData
                {
                    Reactions = _emotionalReactions,
                    GlobalEnabled = _globalEnabled
                };
                
                var json = JsonConvert.SerializeObject(data, Formatting.Indented);
                File.WriteAllText(EmotionalReactionsFile, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка сохранения эмоциональных реакций: {ex.Message}");
            }
        }
    }

    // Класс для сохранения данных эмоциональных реакций
    public class EmotionalReactionsData
    {
        public List<EmotionalReaction> Reactions { get; set; } = new List<EmotionalReaction>();
        public bool GlobalEnabled { get; set; } = true;
    }
} 