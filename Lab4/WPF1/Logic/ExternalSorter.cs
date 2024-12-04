using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace WPF1.Logic
{
    public class ExternalSorter
    {
        private readonly FileHandler _fileHandler;
        private int _tempFileCounter = 0;
        private readonly string _workingDirectory;

        public ExternalSorter(FileHandler fileHandler)
        {
            _fileHandler = fileHandler;
            _workingDirectory = AppDomain.CurrentDomain.BaseDirectory;
        }

        public void Sort(
            string sortMethod,
            int filterKeyIndex,
            string filterKeyValue,
            int secondaryKeyIndex,
            IProgress<string> progress)
        {
            string indentLevel1 = "  ";
            string indentLevel2 = "    ";
            string indentLevel3 = "      ";

            try
            {
                // Шаг 1: Фильтрация данных
                progress?.Report("1. Фильтрация");
                progress?.Report($"Выбираем элементы с ключом \"{filterKeyValue}\"");
                var filteredData = FilterData(filterKeyIndex, filterKeyValue, progress, indentLevel1);

                if (filteredData.Count == 0)
                {
                    progress?.Report("Нет данных, соответствующих критерию фильтрации.");
                    return;
                }

                // Определение типа ключа (числовой или строковый) на основе первого элемента после заголовка
                bool isNumeric = TryDetermineIfNumeric(filteredData, secondaryKeyIndex, progress, indentLevel1);

                // Шаг 2: Разбиение данных на временные файлы в зависимости от метода сортировки
                progress?.Report("2. Разбиение данных");
                List<string> tempFiles;

                switch (sortMethod)
                {
                    case "Прямое слияние":
                        tempFiles = SplitDataForDirectMergeSort(filteredData, secondaryKeyIndex, isNumeric, progress, indentLevel1);
                        break;
                    case "Естественное слияние":
                        tempFiles = SplitDataForNaturalMergeSort(filteredData, secondaryKeyIndex, isNumeric, progress, indentLevel1);
                        break;
                    case "Многопутевое слияние":
                        tempFiles = SplitDataForMultiwayMergeSort(filteredData, secondaryKeyIndex, isNumeric, progress, indentLevel1);
                        break;
                    default:
                        throw new NotSupportedException($"Метод сортировки \"{sortMethod}\" не поддерживается.");
                }

                // Шаг 3: Выполнить сортировку выбранным методом
                progress?.Report($"3. Сортировка методом: {sortMethod} \n");
                string resultFile = sortMethod switch
                {
                    "Прямое слияние" => PerformDirectMergeSort(tempFiles, secondaryKeyIndex, isNumeric, progress, indentLevel1),
                    "Естественное слияние" => PerformNaturalMergeSort(tempFiles, secondaryKeyIndex, isNumeric, progress, indentLevel1),
                    "Многопутевое слияние" => PerformMultiwayMergeSort(tempFiles, secondaryKeyIndex, isNumeric, progress, indentLevel1),
                    _ => throw new NotSupportedException($"Метод сортировки \"{sortMethod}\" не поддерживается.")
                };

                // Шаг 4: Перемещение результата в SortedResult.txt
                var finalResultFile = Path.Combine(_workingDirectory, "SortedResult.txt");
                if (File.Exists(finalResultFile))
                {
                    File.Delete(finalResultFile);
                }
                File.Move(resultFile, finalResultFile);

                progress?.Report($"4. Сортировка завершена. Результат записан в файл: {Path.GetFileName(finalResultFile)} \n");

                // Шаг 5: Вывод результата сортировки
                progress?.Report("5. Результат сортировки:");
                var sortedData = File.ReadAllLines(finalResultFile);
                foreach (var line in sortedData)
                {
                    progress?.Report($"{indentLevel1}- {line}");
                }
            }
            catch (Exception ex)
            {
                progress?.Report($"Ошибка: {ex.Message}");
            }
        }

        private List<string> FilterData(int filterKeyIndex, string filterKeyValue, IProgress<string> progress, string indent)
        {
            var data = _fileHandler.ReadFromFile();
            var filteredData = data.Skip(1)
                .Where(line => line.Split(',')[filterKeyIndex].Trim() == filterKeyValue)
                .ToList();

            if (filteredData.Count == 0)
            {
                return filteredData;
            }
            
            progress?.Report("Результат записан в файл \n");
            filteredData.Insert(0, data[0]);
            return filteredData;
        }

        private bool TryDetermineIfNumeric(List<string> data, int keyIndex, IProgress<string> progress, string indent)
        {
            if (data.Count <= 1)
            {
                progress?.Report($"{indent}Недостаточно данных для определения типа ключа. По умолчанию строковый.");
                return false; // По умолчанию строковый
            }

            string firstKey = data[1].Split(',')[keyIndex].Trim();
            bool isNumeric = double.TryParse(firstKey, NumberStyles.Any, CultureInfo.InvariantCulture, out _);
            
            return isNumeric;
        }

        // Метод для прямого слияния
        private List<string> SplitDataForDirectMergeSort(List<string> data, int secondaryKeyIndex, bool isNumeric, IProgress<string> progress, string indent)
        {
            var tempFiles = new List<string>();
            var evenLines = new List<string>();
            var oddLines = new List<string>();

            for (int i = 1; i < data.Count; i++)
            {
                if ((i % 2) == 0)
                {
                    evenLines.Add(data[i]);
                }
                else
                {
                    oddLines.Add(data[i]);
                }
            }

            // Сортировка каждого файла
            evenLines.Sort((line1, line2) =>
            {
                return CompareLines(line1, line2, secondaryKeyIndex, isNumeric);
            });

            oddLines.Sort((line1, line2) =>
            {
                return CompareLines(line1, line2, secondaryKeyIndex, isNumeric);
            });

            var tempFileEven = GetTempFileName();
            File.WriteAllLines(tempFileEven, evenLines);
            tempFiles.Add(tempFileEven);
            progress?.Report($"Временный файл для четных индексов: {Path.GetFileName(tempFileEven)}");

            var tempFileOdd = GetTempFileName();
            File.WriteAllLines(tempFileOdd, oddLines);
            tempFiles.Add(tempFileOdd);
            progress?.Report($"Временный файл для нечетных индексов: {Path.GetFileName(tempFileOdd)}" + "\n");

            return tempFiles;
        }

        // Метод для естественного слияния
        private List<string> SplitDataForNaturalMergeSort(List<string> data, int secondaryKeyIndex, bool isNumeric, IProgress<string> progress, string indent)
        {
            var tempFiles = new List<string>();
            if (data.Count <= 1) return tempFiles;

            object previousKey = GetKeyValue(data[1], secondaryKeyIndex, isNumeric);
            var currentBlock = new List<string> { data[1] };

            for (int i = 2; i < data.Count; i++)
            {
                object currentKey = GetKeyValue(data[i], secondaryKeyIndex, isNumeric);

                if (IsInOrder(previousKey, currentKey, isNumeric))
                {
                    currentBlock.Add(data[i]);
                }
                else
                {
                    var tempFile = GetTempFileName();
                    File.WriteAllLines(tempFile, currentBlock);
                    tempFiles.Add(tempFile);
                    progress?.Report($"Создан временный файл для естественного блока: {Path.GetFileName(tempFile)}");
                    currentBlock = new List<string> { data[i] };
                }

                previousKey = currentKey;
            }

            if (currentBlock.Count > 0)
            {
                var tempFile = GetTempFileName();
                File.WriteAllLines(tempFile, currentBlock);
                tempFiles.Add(tempFile);
                progress?.Report($"Создан временный файл для естественного блока: {Path.GetFileName(tempFile)} \n");
            }

            return tempFiles;
        }

        // Метод для многопутевого слияния
        private List<string> SplitDataForMultiwayMergeSort(List<string> data, int secondaryKeyIndex, bool isNumeric, IProgress<string> progress, string indent)
        {
            var tempFiles = new List<string>();
            int chunkSize = 1;

            for (int i = 1; i < data.Count; i += chunkSize)
            {
                var chunk = data.Skip(i).Take(chunkSize). ToList();
                
                chunk.Sort((line1, line2) =>
                {
                    return CompareLines(line1, line2, secondaryKeyIndex, isNumeric);
                });

                var tempFile = GetTempFileName();
                File.WriteAllLines(tempFile, chunk);
                tempFiles.Add(tempFile);

                progress?.Report($"Создан временный файл для блока: {Path.GetFileName(tempFile)}");
            }

            progress?.Report("\n");
            return tempFiles;
        }

        // Метод для прямого слияния
        private string PerformDirectMergeSort(List<string> tempFiles, int secondaryKeyIndex, bool isNumeric, IProgress<string> progress, string indent)
        {
            int step = 1;
            while (tempFiles.Count > 1)
            {
                progress?.Report($"Шаг {step}. Слияние пар блоков");
                var mergedFiles = new List<string>();
                var filesToDelete = new List<string>();

                for (int i = 0; i < tempFiles.Count; i += 2)
                {
                    if (i + 1 < tempFiles.Count)
                    {
                        var mergedFile = MergeFiles(tempFiles[i], tempFiles[i + 1], secondaryKeyIndex, isNumeric, progress, indent + "  ");
                        mergedFiles.Add(mergedFile);

                        progress?.Report($"Слияние файлов {Path.GetFileName(tempFiles[i])} и {Path.GetFileName(tempFiles[i + 1])} в {Path.GetFileName(mergedFile)} \n ");

                        filesToDelete.Add(tempFiles[i]);
                        filesToDelete.Add(tempFiles[i + 1]);
                    }
                    else
                    {
                        mergedFiles.Add(tempFiles[i]);
                    }
                }

                foreach (var file in filesToDelete)
                {
                    File.Delete(file);
                }

                tempFiles = mergedFiles;
                step++;
            }

            return tempFiles[0];
        }

        private string PerformNaturalMergeSort(List<string> tempFiles, int secondaryKeyIndex, bool isNumeric, IProgress<string> progress, string indent)
        {
            int step = 1;
            while (tempFiles.Count > 1)
            {
                progress?.Report($"Шаг {step}. Слияние блоков");
                var mergedFiles = new List<string>();
                var filesToDelete = new List<string>();

                for (int i = 0; i < tempFiles.Count; i += 2)
                {
                    if (i + 1 < tempFiles.Count)
                    {
                        var mergedFile = MergeFiles(tempFiles[i], tempFiles[i + 1], secondaryKeyIndex, isNumeric, progress, indent + "  ");
                        mergedFiles.Add(mergedFile);

                        progress?.Report($"Слияние файлов {Path.GetFileName(tempFiles[i])} и {Path.GetFileName(tempFiles[i + 1])} в {Path.GetFileName(mergedFile)} \n");

                        filesToDelete.Add(tempFiles[i]);
                        filesToDelete.Add(tempFiles[i + 1]);
                    }
                    else
                    {
                        mergedFiles.Add(tempFiles[i]);
                    }
                }

                // Удаляем только те файлы, которые были использованы в слиянии
                foreach (var file in filesToDelete)
                {
                    File.Delete(file);
                }

                tempFiles = mergedFiles;
                step++;
            }

            return tempFiles[0];
        }

        private string PerformMultiwayMergeSort(List<string> tempFiles, int secondaryKeyIndex, bool isNumeric, IProgress<string> progress, string indent)
        {
            progress?.Report($"Слияние блоков");
            string resultFile = GetTempFileName();
            using var writer = new StreamWriter(resultFile);

            var readers = tempFiles.Select(file => new StreamReader(file)).ToList();
            var buffer = new List<(string line, StreamReader reader, string fileName)>();

            foreach (var reader in readers)
            {
                if (!reader.EndOfStream)
                {
                    string line = reader.ReadLine();
                    buffer.Add((line, reader, Path.GetFileName(reader.BaseStream is FileStream fs ? fs.Name : "Unknown")));
                }
            }

            while (buffer.Count > 0)
            {
                // Получаем текущие элементы и их ключи
                var currentItems = buffer.Select(x => new
                {
                    x.line,
                    x.reader,
                    x.fileName,
                    key = GetKeyValue(x.line, secondaryKeyIndex, isNumeric)
                }).ToList();

                progress?.Report($"{indent}Текущие элементы для сравнения:");
                foreach (var item in currentItems)
                {
                    progress?.Report($"{indent}- \"{item.line}\" из {item.fileName}");
                }

                var minItem = currentItems.OrderBy(x => x.key, new DynamicKeyComparer(isNumeric)).First();

                progress?.Report($"{indent}Выбираем минимальный элемент \"{minItem.line}\" и добавляем в результирующий файл. \n");
                writer.WriteLine(minItem.line);

                var readerToUpdate = buffer.First(x => x.reader == minItem.reader);
                buffer.Remove(readerToUpdate);

                if (!minItem.reader.EndOfStream)
                {
                    string newLine = minItem.reader.ReadLine();
                    buffer.Add((newLine, minItem.reader, minItem.fileName));
                }
            }

            foreach (var reader in readers)
            {
                reader.Dispose();
            }

            foreach (var file in tempFiles)
            {
                File.Delete(file);
            }

            progress?.Report($"Результат записан во временный файл {Path.GetFileName(resultFile)} \n");
            return resultFile;
        }

        private string MergeFiles(string file1, string file2, int secondaryKeyIndex, bool isNumeric, IProgress<string> progress, string indent)
        {
            var tempFile = GetTempFileName();
            using var reader1 = new StreamReader(file1);
            using var reader2 = new StreamReader(file2);
            using var writer = new StreamWriter(tempFile);

            string line1 = reader1.ReadLine();
            string line2 = reader2.ReadLine();

            while (line1 != null && line2 != null)
            {
                int comparison = CompareLines(line1, line2, secondaryKeyIndex, isNumeric);

                progress?.Report($"{indent}Сравниваем \"{GetKeyValue(line1, secondaryKeyIndex, isNumeric)}\" из {Path.GetFileName(file1)} и \"{GetKeyValue(line2, secondaryKeyIndex, isNumeric)}\" из {Path.GetFileName(file2)}");

                if (comparison <= 0)
                {
                    progress?.Report($"{indent}Добавляем \"{line1}\" в результирующий файл, потому что \"{GetKeyValue(line1, secondaryKeyIndex, isNumeric)}\" ≤ \"{GetKeyValue(line2, secondaryKeyIndex, isNumeric)}\". \n");
                    writer.WriteLine(line1);
                    line1 = reader1.ReadLine();
                }
                else
                {
                    progress?.Report($"{indent}Добавляем \"{line2}\" в результирующий файл, потому что \"{GetKeyValue(line1, secondaryKeyIndex, isNumeric)}\" > \"{GetKeyValue(line2, secondaryKeyIndex, isNumeric)}\". \n");
                    writer.WriteLine(line2);
                    line2 = reader2.ReadLine();
                }
            }

            while (line1 != null)
            {
                progress?.Report($"{indent}Добавляем оставшийся элемент \"{line1}\" в результирующий файл.");
                writer.WriteLine(line1);
                line1 = reader1.ReadLine();
            }

            while (line2 != null)
            {
                progress?.Report($"{indent}Добавляем оставшийся элемент \"{line2}\" в результирующий файл.");
                writer.WriteLine(line2);
                line2 = reader2.ReadLine();
            }

            return tempFile;
        }

        // Метод для сравнения ключей в зависимости от типа
        private int CompareKeys(object key1, object key2, bool isNumeric)
        {
            if (isNumeric)
            {
                return ((double)key1).CompareTo((double)key2);
            }
            else
            {
                return string.Compare((string)key1, (string)key2, StringComparison.Ordinal);
            }
        }

        // Метод для получения значения ключа в зависимости от типа
        private object GetKeyValue(string line, int keyIndex, bool isNumeric)
        {
            var parts = line.Split(',');
            string keyString = parts[keyIndex].Trim();

            if (isNumeric)
            {
                if (double.TryParse(keyString, NumberStyles.Any, CultureInfo.InvariantCulture, out double key))
                {
                    return key;
                }
                else
                {
                    throw new FormatException($"Невозможно преобразовать ключ \"{keyString}\" в число.");
                }
            }
            else
            {
                return keyString;
            }
        }

        // Метод для сравнения двух строковых или числовых ключей
        private int CompareLines(string line1, string line2, int keyIndex, bool isNumeric)
        {
            object key1 = GetKeyValue(line1, keyIndex, isNumeric);
            object key2 = GetKeyValue(line2, keyIndex, isNumeric);

            return CompareKeys(key1, key2, isNumeric);
        }

        private string GetTempFileName()
        {
            return Path.Combine(_workingDirectory, $"TempSortFile_{_tempFileCounter++}.txt");
        }

        // Помогает определить, находится ли последовательность в порядке
        private bool IsInOrder(object previousKey, object currentKey, bool isNumeric)
        {
            if (isNumeric)
            {
                return ((double)previousKey) <= ((double)currentKey);
            }
            else
            {
                return string.Compare((string)previousKey, (string)currentKey, StringComparison.Ordinal) <= 0;
            }
        }

        // Вспомогательный класс для сравнения ключей в Multiway Merge Sort
        private class DynamicKeyComparer : IComparer<object>
        {
            private readonly bool _isNumeric;

            public DynamicKeyComparer(bool isNumeric)
            {
                _isNumeric = isNumeric;
            }

            public int Compare(object x, object y)
            {
                if (_isNumeric)
                {
                    return ((double)x).CompareTo((double)y);
                }
                else
                {
                    return string.Compare((string)x, (string)y, StringComparison.Ordinal);
                }
            }
        }
    }
}