#define MEASURE_TIME

using System;
using System.IO;
using System.Text;
using System.Diagnostics;

namespace Text_Frequency_Analysis {
    static class Program {
        static void Main() {
            string filePath = EnterCorrectFilePath();

          #if MEASURE_TIME
            Stopwatch timer = new Stopwatch();
            timer.Start();
          #endif
            string textForAnalysis = File.ReadAllText(filePath, Encoding.Default);
            string[] triplets = TextAnalyst.FindMostFrequentTriplets(textForAnalysis, 10,
                                                                     TextAnalyst.TextExclusions.IgnoreSeparators);
            PrintTriplets(triplets);
          #if MEASURE_TIME
            timer.Stop();
            Console.WriteLine("Время работы программы [мс]: {0}", timer.ElapsedMilliseconds);
          #endif

            Console.ReadKey();
        }

        static string EnterCorrectFilePath() {
            string filePath;
            bool fileIsCorrect;
            do {
                Console.Write("Введите путь к текстовому файлу (.txt): ");
                filePath = Console.ReadLine();
                fileIsCorrect = File.Exists(filePath) &&
                                (Path.GetExtension(filePath).Equals(".txt"));
                if (!fileIsCorrect) {
                    Console.WriteLine("Файл не найден или не является текстовым (.txt).\n");
                }
            } while (!fileIsCorrect);
            return filePath;
        }
        static void PrintTriplets(string[] triplets) {
            string tripletsRow = String.Join(", ", triplets);
            Console.WriteLine(tripletsRow);
        }
    }
}
