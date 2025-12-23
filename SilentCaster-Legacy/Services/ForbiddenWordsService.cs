using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using SilentCaster.Models;

namespace SilentCaster.Services
{
    public class ForbiddenWordsService
    {
        private const string ForbiddenWordsFile = "ForbiddenWords.json";
        private ForbiddenWords _forbiddenWords;

        public ForbiddenWordsService()
        {
            _forbiddenWords = new ForbiddenWords();
            LoadForbiddenWords();
        }

        public ForbiddenWords GetForbiddenWords()
        {
            return _forbiddenWords;
        }

        public bool ContainsForbiddenWords(string message)
        {
            if (!_forbiddenWords.IsEnabled || string.IsNullOrEmpty(message))
                return false;

            var messageToCheck = _forbiddenWords.CaseSensitive ? message : message.ToLower();

            foreach (var word in _forbiddenWords.Words)
            {
                var wordToCheck = _forbiddenWords.CaseSensitive ? word : word.ToLower();
                if (messageToCheck.Contains(wordToCheck))
                {
                    return true;
                }
            }

            return false;
        }

        public void AddWord(string word)
        {
            if (!string.IsNullOrWhiteSpace(word) && !_forbiddenWords.Words.Contains(word))
            {
                _forbiddenWords.Words.Add(word.Trim());
                SaveForbiddenWords();
            }
        }

        public void RemoveWord(string word)
        {
            if (_forbiddenWords.Words.Contains(word))
            {
                _forbiddenWords.Words.Remove(word);
                SaveForbiddenWords();
            }
        }

        public void UpdateSettings(bool isEnabled, bool caseSensitive)
        {
            _forbiddenWords.IsEnabled = isEnabled;
            _forbiddenWords.CaseSensitive = caseSensitive;
            SaveForbiddenWords();
        }

        public void UpdateAllWords(List<string> newWords)
        {
            _forbiddenWords.Words.Clear();
            _forbiddenWords.Words.AddRange(newWords);
            SaveForbiddenWords();
        }

        public void LoadForbiddenWords()
        {
            try
            {
                if (File.Exists(ForbiddenWordsFile))
                {
                    var json = File.ReadAllText(ForbiddenWordsFile);
                    _forbiddenWords = JsonConvert.DeserializeObject<ForbiddenWords>(json) ?? new ForbiddenWords();
                }
                else
                {
                    // Создаем список с предустановленными запрещенными словами Twitch и YouTube
                    _forbiddenWords = new ForbiddenWords
                    {
                        IsEnabled = true,
                        CaseSensitive = false,
                        Words = new List<string>
                        {
                            // Twitch запрещенные слова
                            "fuck", "shit", "bitch", "ass", "dick", "cock", "pussy", "cunt", "whore", "slut",
                            "nigger", "nigga", "faggot", "fag", "retard", "retarded", "autist", "autistic",
                            "kys", "kill yourself", "kys", "kys", "kys", "kys", "kys", "kys", "kys", "kys",
                            
                            // YouTube запрещенные слова
                            "fuck", "shit", "bitch", "ass", "dick", "cock", "pussy", "cunt", "whore", "slut",
                            "nigger", "nigga", "faggot", "fag", "retard", "retarded", "autist", "autistic",
                            "kys", "kill yourself", "kys", "kys", "kys", "kys", "kys", "kys", "kys", "kys",
                            
                            // Русские запрещенные слова
                            "хуй", "хуя", "хуем", "хуи", "пизда", "пиздец", "пиздеть", "пиздишь", "пиздюк",
                            "блять", "блядь", "бля", "ебать", "ебаться", "ебало", "ебал", "ебала", "ебали",
                            "сука", "суки", "суке", "суку", "сукой", "сукам", "суками", "суках",
                            "говно", "говна", "говну", "говном", "говне", "говны", "говнами", "говнах",
                            "заебись", "заебал", "заебала", "заебали", "заебись", "заебись", "заебись",
                            "похуй", "нахуй", "нахуя", "нахуй", "нахуя", "нахуй", "нахуя", "нахуй",
                            "иди нахуй", "пошел нахуй", "иди в пизду", "пошел в пизду",
                            "убивайся", "убийся", "сдохни", "сдох", "сдохла", "сдохли",
                            
                            // Общие запрещенные фразы
                            "убий себя", "покончи с собой", "сдохни", "сдох", "умри", "умри", "умри",
                            "ненавижу", "ненависть", "ненавистник", "ненавистница", "ненавистники",
                            "расист", "расизм", "расистский", "расистская", "расистское", "расистские",
                            "фашист", "фашизм", "фашистский", "фашистская", "фашистское", "фашистские",
                            "нацист", "нацизм", "нацистский", "нацистская", "нацистское", "нацистские",
                            "террорист", "терроризм", "террористический", "террористическая", "террористическое", "террористические",
                            "экстремист", "экстремизм", "экстремистский", "экстремистская", "экстремистское", "экстремистские"
                        }
                    };
                    SaveForbiddenWords();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки запрещенных слов: {ex.Message}");
                _forbiddenWords = new ForbiddenWords();
            }
        }

        private void SaveForbiddenWords()
        {
            try
            {
                var json = JsonConvert.SerializeObject(_forbiddenWords, Formatting.Indented);
                File.WriteAllText(ForbiddenWordsFile, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка сохранения запрещенных слов: {ex.Message}");
            }
        }
    }
} 