using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using SilentCaster.Models;

namespace SilentCaster.Services
{
    public class StreamEventsService
    {
        private const string StreamEventsFile = "StreamEvents.json";
        private List<StreamEvent> _streamEvents;
        private readonly Random _random;

        public StreamEventsService()
        {
            _streamEvents = new List<StreamEvent>();
            _random = new Random();
            LoadStreamEvents();
        }

        public List<StreamEvent> GetAllEvents()
        {
            return _streamEvents.ToList();
        }

        public StreamEvent? GetEventForDonation(decimal amount, string username)
        {
            var availableEvents = _streamEvents
                .Where(e => e.IsEnabled && e.Type == EventType.Donation)
                .Where(e => !e.MinAmount.HasValue || amount >= e.MinAmount.Value)
                .Where(e => !e.MaxAmount.HasValue || amount <= e.MaxAmount.Value)
                .OrderBy(e => e.Priority)
                .ToList();

            foreach (var streamEvent in availableEvents)
            {
                if (_random.NextDouble() * 100 <= streamEvent.UsageChance)
                {
                    return streamEvent;
                }
            }

            return availableEvents.FirstOrDefault();
        }

        public StreamEvent? GetEventForSubscription(EventType type, int months = 1, string username = "")
        {
            var availableEvents = _streamEvents
                .Where(e => e.IsEnabled && e.Type == type)
                .Where(e => type != EventType.Resubscription || !e.SubscriberMonths.HasValue || months >= e.SubscriberMonths.Value)
                .OrderBy(e => e.Priority)
                .ToList();

            foreach (var streamEvent in availableEvents)
            {
                if (_random.NextDouble() * 100 <= streamEvent.UsageChance)
                {
                    return streamEvent;
                }
            }

            return availableEvents.FirstOrDefault();
        }

        public StreamEvent? GetEventForRaid(int viewerCount, string raiderName)
        {
            var availableEvents = _streamEvents
                .Where(e => e.IsEnabled && e.Type == EventType.Raid)
                .Where(e => !e.RaidViewers.HasValue || viewerCount >= e.RaidViewers.Value)
                .OrderBy(e => e.Priority)
                .ToList();

            foreach (var streamEvent in availableEvents)
            {
                if (_random.NextDouble() * 100 <= streamEvent.UsageChance)
                {
                    return streamEvent;
                }
            }

            return availableEvents.FirstOrDefault();
        }

        public void AddEvent(StreamEvent streamEvent)
        {
            if (!_streamEvents.Any(e => e.Id == streamEvent.Id))
            {
                _streamEvents.Add(streamEvent);
                SaveStreamEvents();
            }
        }

        public void UpdateEvent(StreamEvent streamEvent)
        {
            var existingIndex = _streamEvents.FindIndex(e => e.Id == streamEvent.Id);
            if (existingIndex >= 0)
            {
                _streamEvents[existingIndex] = streamEvent;
                SaveStreamEvents();
            }
        }

        public void RemoveEvent(string id)
        {
            var streamEvent = _streamEvents.FirstOrDefault(e => e.Id == id);
            if (streamEvent != null)
            {
                _streamEvents.Remove(streamEvent);
                SaveStreamEvents();
            }
        }

        private void LoadStreamEvents()
        {
            try
            {
                if (File.Exists(StreamEventsFile))
                {
                    var json = File.ReadAllText(StreamEventsFile);
                    _streamEvents = JsonConvert.DeserializeObject<List<StreamEvent>>(json) ?? new List<StreamEvent>();
                }
                else
                {
                    // Создаем предустановленные реакции на события
                    _streamEvents = new List<StreamEvent>
                    {
                        // Донаты
                        new StreamEvent
                        {
                            Name = "Маленький донат",
                            Description = "Реакция на донаты до 5$",
                            Type = EventType.Donation,
                            MinAmount = 0.01m,
                            MaxAmount = 5.00m,
                            Responses = new List<string> 
                            { 
                                "Спасибо за донат, {username}!", 
                                "Ого, донат от {username}!", 
                                "Спасибо, {username}!", 
                                "Круто, {username}!" 
                            },
                            Priority = 1,
                            UsageChance = 100.0
                        },
                        new StreamEvent
                        {
                            Name = "Средний донат",
                            Description = "Реакция на донаты 5-20$",
                            Type = EventType.Donation,
                            MinAmount = 5.01m,
                            MaxAmount = 20.00m,
                            Responses = new List<string> 
                            { 
                                "Вау, {username}! Спасибо за такой донат!", 
                                "Невероятно, {username}! Спасибо!", 
                                "Ого, {username}! Ты потрясающий!", 
                                "Спасибо за щедрость, {username}!" 
                            },
                            Priority = 2,
                            UsageChance = 100.0
                        },
                        new StreamEvent
                        {
                            Name = "Большой донат",
                            Description = "Реакция на донаты от 20$",
                            Type = EventType.Donation,
                            MinAmount = 20.01m,
                            Responses = new List<string> 
                            { 
                                "БОЖЕ МОЙ, {username}! ТЫ НЕВЕРОЯТНЫЙ!", 
                                "ВАУ! {username}, ты просто потрясающий!", 
                                "Не могу поверить! Спасибо, {username}!", 
                                "Ты лучший, {username}! Спасибо огромное!" 
                            },
                            Priority = 3,
                            UsageChance = 100.0
                        },

                        // Подписки
                        new StreamEvent
                        {
                            Name = "Новая подписка",
                            Description = "Реакция на новые подписки",
                            Type = EventType.Subscription,
                            Responses = new List<string> 
                            { 
                                "Добро пожаловать в семью, {username}!", 
                                "Спасибо за подписку, {username}!", 
                                "Новый подписчик! Привет, {username}!", 
                                "Рад видеть тебя, {username}!" 
                            },
                            Priority = 1,
                            UsageChance = 100.0
                        },
                        new StreamEvent
                        {
                            Name = "Продление подписки",
                            Description = "Реакция на продление подписки",
                            Type = EventType.Resubscription,
                            Responses = new List<string> 
                            { 
                                "Спасибо за продление, {username}!", 
                                "Рад видеть тебя снова, {username}!", 
                                "Ты вернулся! Привет, {username}!", 
                                "Спасибо за лояльность, {username}!" 
                            },
                            Priority = 2,
                            UsageChance = 100.0
                        },
                        new StreamEvent
                        {
                            Name = "Подарочная подписка",
                            Description = "Реакция на подарочные подписки",
                            Type = EventType.GiftSubscription,
                            Responses = new List<string> 
                            { 
                                "Спасибо за подарок, {username}!", 
                                "Как мило! Спасибо, {username}!", 
                                "Ты даришь подписки! Спасибо, {username}!", 
                                "Ого, подарок! Спасибо, {username}!" 
                            },
                            Priority = 3,
                            UsageChance = 100.0
                        },

                        // Рейды
                        new StreamEvent
                        {
                            Name = "Маленький рейд",
                            Description = "Реакция на рейды до 50 зрителей",
                            Type = EventType.Raid,
                            MaxAmount = 50,
                            Responses = new List<string> 
                            { 
                                "Рейд! Привет, {username} и ваши зрители!", 
                                "Ого, рейд от {username}!", 
                                "Спасибо за рейд, {username}!", 
                                "Добро пожаловать, рейдеры!" 
                            },
                            Priority = 1,
                            UsageChance = 100.0
                        },
                        new StreamEvent
                        {
                            Name = "Большой рейд",
                            Description = "Реакция на рейды от 50 зрителей",
                            Type = EventType.Raid,
                            MinAmount = 51,
                            Responses = new List<string> 
                            { 
                                "БОЛЬШОЙ РЕЙД! Спасибо, {username}!", 
                                "ВАУ! Огромный рейд от {username}!", 
                                "Невероятно! Спасибо за рейд, {username}!", 
                                "Добро пожаловать всем рейдерам!" 
                            },
                            Priority = 2,
                            UsageChance = 100.0
                        },

                        // Фоллоу
                        new StreamEvent
                        {
                            Name = "Новый фоллоу",
                            Description = "Реакция на новые фоллоу",
                            Type = EventType.Follow,
                            Responses = new List<string> 
                            { 
                                "Новый фоллоу! Привет, {username}!", 
                                "Спасибо за фоллоу, {username}!", 
                                "Добро пожаловать, {username}!", 
                                "Рад видеть тебя, {username}!" 
                            },
                            Priority = 1,
                            UsageChance = 100.0
                        }
                    };
                    SaveStreamEvents();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки событий стрима: {ex.Message}");
                _streamEvents = new List<StreamEvent>();
            }
        }

        private void SaveStreamEvents()
        {
            try
            {
                var json = JsonConvert.SerializeObject(_streamEvents, Formatting.Indented);
                File.WriteAllText(StreamEventsFile, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка сохранения событий стрима: {ex.Message}");
            }
        }
    }
} 