using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace Text_Frequency_Analysis {
    /// <summary>
    /// Предоставляет статические методы для анализа текстов
    /// </summary>
    static class TextAnalyst {
        const int TRIPLET_LENGTH = 3;
        const char REFORM_SEPARATOR = ' ';
        const int TASKS_COUNT = 4;

        /// <summary>
        /// Класс сравнения ключей по убыванию с сохранением дублирующихся значений
        /// </summary>
        private class DuplicateDescOrderKeyComparer<TKey>: IComparer<TKey> where TKey : IComparable {
            public int Compare(TKey x, TKey y) {
                int result = -x.CompareTo(y);
                if (result == 0) return 1;
                else return result;
            }
        }

        /// <summary>
        /// Варианты исключения символов из текста
        /// </summary>
        public enum TextExclusions {
            SaveAll,
            IgnoreSeparators,
            LettersOrDigits,
            OnlyLetters
        };

        /// <summary>
        /// Находит в указанном тексте самые часто встречающиеся триплеты (3 идущих подряд буквы слова).
        /// </summary>
        /// <param name="sourceText">Исходный текст для поиска</param>
        /// <param name="exclusions">Параметр, определяющий допустимые в триплете символы</param>
        /// <param name="tripletsCount">Требуемое число триплетов</param>
        /// <returns>
        /// Массив строк - самых часто встречающихся триплетов в порядке убывания частоты.
        /// </returns>
        public static string[] FindMostFrequentTriplets(string sourceText, int tripletsCount,
                                                        TextExclusions exclusions = TextExclusions.SaveAll) {

            string treatedText = TreatText(sourceText, exclusions);

            bool textIsReformed = exclusions != TextExclusions.SaveAll;
            ConcurrentDictionary<string, int> tripletsFrequency = new ConcurrentDictionary<string, int>();
            int largePartsCount = treatedText.Length % TASKS_COUNT;
            int partsLengthBase = treatedText.Length / TASKS_COUNT;
            int partStartIndex = 0;
            if (partsLengthBase >= TRIPLET_LENGTH) {
                Task[] tasks = new Task[TASKS_COUNT];
                for (int taskNum = 0; taskNum < TASKS_COUNT; taskNum++) {
                    int partLength = largePartsCount > 0 ? partsLengthBase + 1 : partsLengthBase;

                    string textPart;
                    textPart = treatedText.Substring(partStartIndex,
                                                     partLength + (taskNum < TASKS_COUNT - 1 ? TRIPLET_LENGTH - 1 : 0));
                    partStartIndex += partLength;
                    if (largePartsCount > 0) {
                        largePartsCount--;
                    }

                    Action taskAction = () => FindTripletsInText(textPart, textIsReformed, tripletsFrequency);
                    tasks[taskNum] = Task.Run(taskAction);
                }
                Task.WaitAll(tasks);
            }
            else {
                FindTripletsInText(treatedText, textIsReformed, tripletsFrequency);
            }

            DuplicateDescOrderKeyComparer<int> keyComparer = new DuplicateDescOrderKeyComparer<int>();
            SortedList<int, string> mostFrequentTriplets = new SortedList<int, string>(keyComparer);
            foreach (KeyValuePair<string, int> tripletFrequency in tripletsFrequency) {
                if (mostFrequentTriplets.Count == 0 || tripletFrequency.Value >= mostFrequentTriplets.Last().Key) {
                    mostFrequentTriplets.Add(tripletFrequency.Value, tripletFrequency.Key);
                    if (mostFrequentTriplets.Count > tripletsCount) {
                        mostFrequentTriplets.RemoveAt(mostFrequentTriplets.Count - 1);
                    }
                }
            }

            return mostFrequentTriplets.Values.ToArray();
        }

        /// <summary>
        /// Непосредственно выполняет поиск триплетов в указанном тексте и увеличивает значение их частоты в словаре
        /// </summary>
        private static void FindTripletsInText(string text, bool isTextReformed,
                                               ConcurrentDictionary<string, int> tripletsFrequency_) {
            if (text.Length < TRIPLET_LENGTH) {
                return;
            }

            if (isTextReformed) {
                int charsToSkip = 0;
                for (int i = TRIPLET_LENGTH - 1; i >= 0; i--) {
                    if (text[i] == REFORM_SEPARATOR) {
                        charsToSkip = i + 1;
                        break;
                    }
                }

                for (int charIndex = 0; charIndex < text.Length - TRIPLET_LENGTH; charIndex++) {
                    if (text[charIndex + TRIPLET_LENGTH - 1] == REFORM_SEPARATOR) {
                        charsToSkip = TRIPLET_LENGTH;
                        continue;
                    }
                    if (charsToSkip > 0) {
                        charsToSkip--;
                        continue;
                    }
                    string triplet = text.Substring(charIndex, TRIPLET_LENGTH);
                    tripletsFrequency_.AddOrUpdate(triplet, 1, (key, val) => val + 1);
                }
            }
            else {
                for (int charIndex = 0; charIndex <= text.Length - TRIPLET_LENGTH; charIndex++) {
                    string triplet = text.Substring(charIndex, TRIPLET_LENGTH);
                    tripletsFrequency_.AddOrUpdate(triplet, 1, (key, val) => val + 1);
                }
            } 
        }
        /// <summary>
        /// Обрабатывает текст в соответствии с параметром исключения символов
        /// </summary>
        private static string TreatText(string text, TextExclusions exclusions) {
            switch (exclusions) {
                default:
                case TextExclusions.SaveAll:
                    return text;
                case TextExclusions.IgnoreSeparators:
                    return Regex.Replace(text, "[\t\v\r\n\f]+", REFORM_SEPARATOR.ToString());
                case TextExclusions.LettersOrDigits:
                    return Regex.Replace(text, "[^А-яёЁ|A-z|0-9]+", REFORM_SEPARATOR.ToString());
                case TextExclusions.OnlyLetters:
                    return Regex.Replace(text, "[^А-яёЁ|A-z]+", REFORM_SEPARATOR.ToString());
            }
        }
    }
}
