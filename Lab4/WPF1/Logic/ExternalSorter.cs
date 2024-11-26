using System.Globalization;
using System.IO;

namespace WPF1.Logic;

public class ExternalSorter
{
    public static void MergeSort(string inputFilePath, string filterColumn, string filterValue, string sortKey, string outputFilePath, string sortingMethod)
    {
        // Вызываем соответствующий метод сортировки
        switch (sortingMethod)
        {
            case "Прямое слияние":
                DirectMergeSort(inputFilePath, filterColumn, filterValue, sortKey, outputFilePath);
                break;
            case "Естественное слияние":
                NaturalMergeSort(inputFilePath, filterColumn, filterValue, sortKey, outputFilePath);
                break;
            case "Многопутевое слияние":
                MultiwayMergeSort(inputFilePath, filterColumn, filterValue, sortKey, outputFilePath);
                break;
            default:
                throw new Exception("Неизвестный метод сортировки.");
        }
    }

    public static void DirectMergeSort(string inputFilePath, string filterColumn, string filterValue, string sortKey, string outputFilePath)
    {
        // Шаг 1: Создаем начальные отсортированные блоки
        var tempFiles = SplitIntoSortedChunks(inputFilePath, filterColumn, filterValue, sortKey);

        // Шаг 2: Сливаем блоки попарно
        while (tempFiles.Count > 1)
        {
            var mergedTempFiles = new List<string>();

            for (int i = 0; i < tempFiles.Count; i += 2)
            {
                if (i + 1 < tempFiles.Count)
                {
                    // Слияние tempFiles[i] и tempFiles[i + 1]
                    var mergedFile = Path.GetTempFileName();
                    MergeTwoFiles(tempFiles[i], tempFiles[i + 1], sortKey, mergedFile);
                    mergedTempFiles.Add(mergedFile);

                    // Удаляем объединенные файлы
                    File.Delete(tempFiles[i]);
                    File.Delete(tempFiles[i + 1]);
                }
                else
                {
                    // Если нечетное количество файлов, переносим последний файл
                    mergedTempFiles.Add(tempFiles[i]);
                }
            }

            tempFiles = mergedTempFiles;
        }

        // Последний оставшийся файл - это отсортированный результат
        if (tempFiles.Count == 1)
        {
            File.Move(tempFiles[0], outputFilePath, true);
        }
        else
        {
            throw new Exception("Ошибка при слиянии файлов.");
        }
    }

    private static void MergeTwoFiles(string file1, string file2, string sortKey, string outputFile)
    {
        using var reader1 = new StreamReader(file1);
        using var reader2 = new StreamReader(file2);
        using var writer = new StreamWriter(outputFile);

        var header1 = reader1.ReadLine();
        var header2 = reader2.ReadLine();

        // Предполагаем, что заголовки одинаковы
        writer.WriteLine(header1);

        var headers = header1.Split(',').ToList();
        int sortKeyIndex = headers.IndexOf(sortKey);

        bool isNumeric = IsNumericColumnInFiles(new List<string> { file1, file2 }, sortKeyIndex);

        var comparer = new FileLineComparer(sortKeyIndex, isNumeric);

        // Чтение первой строки из каждого файла
        string line1 = reader1.ReadLine();
        string line2 = reader2.ReadLine();

        FileLine fileLine1 = null;
        FileLine fileLine2 = null;

        if (line1 != null)
        {
            var fields1 = line1.Split(',');
            fileLine1 = new FileLine { Fields = fields1 };
        }

        if (line2 != null)
        {
            var fields2 = line2.Split(',');
            fileLine2 = new FileLine { Fields = fields2 };
        }

        // Слияние двух файлов
        while (fileLine1 != null && fileLine2 != null)
        {
            if (comparer.Compare(fileLine1, fileLine2) <= 0)
            {
                writer.WriteLine(string.Join(",", fileLine1.Fields));
                line1 = reader1.ReadLine();
                if (line1 != null)
                {
                    var fields1 = line1.Split(',');
                    fileLine1 = new FileLine { Fields = fields1 };
                }
                else
                {
                    fileLine1 = null;
                }
            }
            else
            {
                writer.WriteLine(string.Join(",", fileLine2.Fields));
                line2 = reader2.ReadLine();
                if (line2 != null)
                {
                    var fields2 = line2.Split(',');
                    fileLine2 = new FileLine { Fields = fields2 };
                }
                else
                {
                    fileLine2 = null;
                }
            }
        }

        // Записываем оставшиеся строки из первого файла
        while (fileLine1 != null)
        {
            writer.WriteLine(string.Join(",", fileLine1.Fields));
            line1 = reader1.ReadLine();
            if (line1 != null)
            {
                var fields1 = line1.Split(',');
                fileLine1 = new FileLine { Fields = fields1 };
            }
            else
            {
                fileLine1 = null;
            }
        }

        // Записываем оставшиеся строки из второго файла
        while (fileLine2 != null)
        {
            writer.WriteLine(string.Join(",", fileLine2.Fields));
            line2 = reader2.ReadLine();
            if (line2 != null)
            {
                var fields2 = line2.Split(',');
                fileLine2 = new FileLine { Fields = fields2 };
            }
            else
            {
                fileLine2 = null;
            }
        }
    }
    private static bool IsNumericColumnInFiles(List<string> filePaths, int columnIndex)
    {
        var culture = CultureInfo.InvariantCulture;
        foreach (var filePath in filePaths)
        {
            using var reader = new StreamReader(filePath);
            // Пропускаем заголовок
            var header = reader.ReadLine();

            string line;
            while ((line = reader.ReadLine()) != null)
            {
                var fields = line.Split(',');
                if (fields.Length > columnIndex)
                {
                    if (!double.TryParse(fields[columnIndex], NumberStyles.Any, culture, out _))
                    {
                        return false; // Если хотя бы одно значение не число, столбец не числовой
                    }
                }
            }
        }
        return true; // Все значения во всех файлах являются числовыми
    }

    public static void NaturalMergeSort(string inputFilePath, string filterColumn, string filterValue, string sortKey, string outputFilePath)
    {
        string tempFile1 = Path.GetTempFileName();
        string tempFile2 = Path.GetTempFileName();

        // Создаем начальные руны
        CreateInitialRunsForNaturalMerge(inputFilePath, filterColumn, filterValue, sortKey, tempFile1, tempFile2);

        bool isSorted = false;
        while (!isSorted)
        {
            isSorted = MergeRuns(tempFile1, tempFile2, sortKey);
        }

        // Копируем отсортированный файл в выходной
        File.Move(tempFile1, outputFilePath, true);
        File.Delete(tempFile2);
    }

    private static void CreateInitialRunsForNaturalMerge(string inputFilePath, string filterColumn, string filterValue, string sortKey, string outputFile1, string outputFile2)
    {
        using var reader = new StreamReader(inputFilePath);
        using var writer1 = new StreamWriter(outputFile1);
        using var writer2 = new StreamWriter(outputFile2);

        var headerLine = reader.ReadLine();
        var headers = headerLine.Split(',').ToList();

        int filterColumnIndex = headers.IndexOf(filterColumn);
        int sortKeyIndex = headers.IndexOf(sortKey);

        if (filterColumnIndex == -1 || sortKeyIndex == -1)
            throw new Exception("Некорректный фильтрующий столбец или ключ сортировки.");

        writer1.WriteLine(headerLine);
        writer2.WriteLine(headerLine);

        string prevSortValue = null;
        bool writeToFirstFile = true;

        bool isNumeric = IsNumericColumnInFile(reader, sortKeyIndex);

        // Возвращаемся в начало файла после проверки
        reader.BaseStream.Position = 0;
        reader.DiscardBufferedData();
        reader.ReadLine(); // Пропускаем заголовок

        while (!reader.EndOfStream)
        {
            var line = reader.ReadLine();
            var fields = line.Split(',');

            if (fields.Length != headers.Count)
                continue;

            if (fields[filterColumnIndex] != filterValue)
                continue;

            string currentSortValue = fields[sortKeyIndex];

            if (prevSortValue != null)
            {
                int comparison;
                if (isNumeric)
                {
                    double prevValue = double.Parse(prevSortValue, CultureInfo.InvariantCulture);
                    double currentValue = double.Parse(currentSortValue, CultureInfo.InvariantCulture);
                    comparison = currentValue.CompareTo(prevValue);
                }
                else
                {
                    comparison = string.Compare(currentSortValue, prevSortValue, StringComparison.Ordinal);
                }

                // Если текущий элемент меньше предыдущего, начинаем новую руну
                if (comparison < 0)
                {
                    writeToFirstFile = !writeToFirstFile;
                }
            }

            if (writeToFirstFile)
            {
                writer1.WriteLine(line);
            }
            else
            {
                writer2.WriteLine(line);
            }

            prevSortValue = currentSortValue;
        }
    }

    private static bool IsNumericColumnInFile(StreamReader reader, int columnIndex)
    {
        long initialPosition = reader.BaseStream.Position;
        reader.DiscardBufferedData(); // Очищаем буфер

        var culture = CultureInfo.InvariantCulture;

        string line;
        while ((line = reader.ReadLine()) != null)
        {
            var fields = line.Split(',');
            if (fields.Length > columnIndex)
            {
                if (!double.TryParse(fields[columnIndex], NumberStyles.Any, culture, out _))
                {
                    // Если хотя бы одно значение не число, столбец не числовой
                    reader.BaseStream.Position = initialPosition;
                    reader.DiscardBufferedData();
                    return false;
                }
            }
        }

        // Если все значения числовые
        reader.BaseStream.Position = initialPosition;
        reader.DiscardBufferedData();
        return true;
    }

    
    private static bool MergeRuns(string inputFile1, string inputFile2, string sortKey)
    {
        bool isSorted = true;

        using var reader1 = new StreamReader(inputFile1);
        using var reader2 = new StreamReader(inputFile2);

        string tempOutputFile1 = Path.GetTempFileName();
        string tempOutputFile2 = Path.GetTempFileName();

        using var writer1 = new StreamWriter(tempOutputFile1);
        using var writer2 = new StreamWriter(tempOutputFile2);

        var header1 = reader1.ReadLine();
        var header2 = reader2.ReadLine();
        var headers = header1.Split(',').ToList();
        int sortKeyIndex = headers.IndexOf(sortKey);

        bool isNumeric = IsNumericColumnInFiles(new List<string> { inputFile1, inputFile2 }, sortKeyIndex);

        var comparer = new FileLineComparer(sortKeyIndex, isNumeric);

        FileLine prevLine = null;
        bool writeToFirstFile = true;

        // Записываем заголовок в оба выходных файла
        writer1.WriteLine(header1);
        writer2.WriteLine(header2);

        Queue<FileLine> queue1 = ReadRun(reader1, sortKeyIndex, comparer);
        Queue<FileLine> queue2 = ReadRun(reader2, sortKeyIndex, comparer);

        while (queue1.Count > 0 || queue2.Count > 0)
        {
            while (queue1.Count > 0 && (queue2.Count == 0 || comparer.Compare(queue1.Peek(), queue2.Peek()) <= 0))
            {
                var line = queue1.Dequeue();
                var writer = writeToFirstFile ? writer1 : writer2;
                writer.WriteLine(string.Join(",", line.Fields));

                if (prevLine != null && comparer.Compare(prevLine, line) > 0)
                {
                    // Начинаем новую руну
                    writeToFirstFile = !writeToFirstFile;
                    isSorted = false;
                }

                prevLine = line;

                if (queue1.Count == 0)
                    queue1 = ReadRun(reader1, sortKeyIndex, comparer);
            }

            while (queue2.Count > 0 && (queue1.Count == 0 || comparer.Compare(queue2.Peek(), queue1.Peek()) <= 0))
            {
                var line = queue2.Dequeue();
                var writer = writeToFirstFile ? writer1 : writer2;
                writer.WriteLine(string.Join(",", line.Fields));

                if (prevLine != null && comparer.Compare(prevLine, line) > 0)
                {
                    // Начинаем новую руну
                    writeToFirstFile = !writeToFirstFile;
                    isSorted = false;
                }

                prevLine = line;

                if (queue2.Count == 0)
                    queue2 = ReadRun(reader2, sortKeyIndex, comparer);
            }
        }

        reader1.Close();
        reader2.Close();
        writer1.Close();
        writer2.Close();

        // Заменяем входные файлы на выходные
        File.Delete(inputFile1);
        File.Delete(inputFile2);

        File.Move(tempOutputFile1, inputFile1);
        File.Move(tempOutputFile2, inputFile2);

        return isSorted;
    }



    private static Queue<FileLine> ReadRun(StreamReader reader, int sortKeyIndex, FileLineComparer comparer)
    {
        var queue = new Queue<FileLine>();
        FileLine prevLine = null;

        while (!reader.EndOfStream)
        {
            var line = reader.ReadLine();
            var fields = line.Split(',');
            var currentLine = new FileLine { Fields = fields };

            if (prevLine != null && comparer.Compare(prevLine, currentLine) > 0)
            {
                // Начало новой руны
                break;
            }

            queue.Enqueue(currentLine);
            prevLine = currentLine;
        }

        return queue;
    }

    
    public static void MultiwayMergeSort(string inputFilePath, string filterColumn, string filterValue, string sortKey, string outputFilePath)
    {
        // Разделение данных на блоки с учетом фильтрации
        var tempFiles = SplitIntoSortedChunks(inputFilePath, filterColumn, filterValue, sortKey);

        // Слияние блоков
        MergeChunks(tempFiles, sortKey, outputFilePath);

        // Удаление временных файлов
        foreach (var file in tempFiles)
        {
            File.Delete(file);
        }
    }
    
    private static List<string> SplitIntoSortedChunks(string inputFilePath, string filterColumn, string filterValue, string sortKey)
    {
        const int chunkSize = 1000; // Размер блока
        var tempFiles = new List<string>();

        using (var reader = new StreamReader(inputFilePath))
        {
            var headerLine = reader.ReadLine();
            var headers = headerLine.Split(',').ToList();

            int filterColumnIndex = headers.IndexOf(filterColumn);
            int sortKeyIndex = headers.IndexOf(sortKey);

            if (filterColumnIndex == -1 || sortKeyIndex == -1)
                throw new Exception("Некорректный фильтрующий столбец или ключ сортировки.");

            var chunkLines = new List<string[]>();
            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                var fields = line.Split(',');

                if (fields.Length != headers.Count)
                    continue; // Пропускаем некорректные строки

                if (fields[filterColumnIndex] == filterValue)
                {
                    chunkLines.Add(fields);

                    if (chunkLines.Count >= chunkSize)
                    {
                        SortAndSaveChunk(chunkLines, headers, sortKeyIndex, tempFiles);
                        chunkLines.Clear();
                    }
                }
            }

            // Обработка последнего блока
            if (chunkLines.Count > 0)
            {
                SortAndSaveChunk(chunkLines, headers, sortKeyIndex, tempFiles);
            }
        }

        return tempFiles;
    }

    private static void SortAndSaveChunk(List<string[]> chunkLines, List<string> headers, int sortKeyIndex, List<string> tempFiles)
    {
        bool isNumeric = IsNumericColumn(chunkLines, sortKeyIndex);

        if (isNumeric)
        {
            chunkLines.Sort((a, b) => double.Parse(b[sortKeyIndex]).CompareTo(double.Parse(a[sortKeyIndex]))); // От большего к меньшему
        }
        else
        {
            chunkLines.Sort((a, b) => string.Compare(a[sortKeyIndex], b[sortKeyIndex], StringComparison.Ordinal)); // По алфавиту
        }

        var tempFile = Path.GetTempFileName();
        using (var writer = new StreamWriter(tempFile))
        {
            writer.WriteLine(string.Join(",", headers));
            foreach (var fields in chunkLines)
            {
                writer.WriteLine(string.Join(",", fields));
            }
        }

        tempFiles.Add(tempFile);
    }

    private static bool IsNumericColumn(List<string[]> rows, int columnIndex)
    {
        return rows.All(row => double.TryParse(row[columnIndex], out _));
    }

    private static void MergeChunks(List<string> tempFiles, string sortKey, string outputFilePath)
    {
        var readers = new List<StreamReader>();
        try
        {
            foreach (var file in tempFiles)
            {
                readers.Add(new StreamReader(file));
            }

            var headerLine = readers[0].ReadLine();
            var headers = headerLine.Split(',').ToList();
            int sortKeyIndex = headers.IndexOf(sortKey);

            if (sortKeyIndex == -1)
                throw new Exception("Некорректный ключ сортировки.");

            bool isNumeric = IsNumericInFiles(readers, sortKeyIndex);

            var comparer = new FileLineComparer(sortKeyIndex, isNumeric);
            var priorityQueue = new List<FileLine>();

            // Чтение первой строки из каждого файла
            for (int i = 0; i < readers.Count; i++)
            {
                var reader = readers[i];
                var line = reader.ReadLine();
                if (line != null)
                {
                    var fields = line.Split(',');
                    priorityQueue.Add(new FileLine { Fields = fields, FileIndex = i });
                }
            }

            priorityQueue.Sort(comparer);

            using (var writer = new StreamWriter(outputFilePath))
            {
                writer.WriteLine(string.Join(",", headers));

                while (priorityQueue.Count > 0)
                {
                    var nextItem = priorityQueue[0];
                    priorityQueue.RemoveAt(0);

                    writer.WriteLine(string.Join(",", nextItem.Fields));

                    var reader = readers[nextItem.FileIndex];
                    var line = reader.ReadLine();
                    if (line != null)
                    {
                        var fields = line.Split(',');
                        priorityQueue.Add(new FileLine { Fields = fields, FileIndex = nextItem.FileIndex });
                        priorityQueue.Sort(comparer);
                    }
                }
            }
        }
        finally
        {
            foreach (var reader in readers)
            {
                reader.Close();
            }
        }
    }

    private static bool IsNumericInFiles(List<StreamReader> readers, int columnIndex)
    {
        foreach (var reader in readers)
        {
            long initialPosition = reader.BaseStream.Position;
            var line = reader.ReadLine();
            if (line != null)
            {
                var fields = line.Split(',');
                if (double.TryParse(fields[columnIndex], out _))
                {
                    reader.BaseStream.Position = initialPosition;
                    return true;
                }
            }
            reader.BaseStream.Position = initialPosition;
        }
        return false;
    }
}

public class FileLine
{
    public string[] Fields { get; set; }
    public int FileIndex { get; set; }
}

public class FileLineComparer : IComparer<FileLine>
{
    private readonly int _sortKeyIndex;
    private readonly bool _isNumeric;

    public FileLineComparer(int sortKeyIndex, bool isNumeric)
    {
        _sortKeyIndex = sortKeyIndex;
        _isNumeric = isNumeric;
    }

    public int Compare(FileLine x, FileLine y)
    {
        string xValue = x.Fields[_sortKeyIndex];
        string yValue = y.Fields[_sortKeyIndex];

        int result;
        if (_isNumeric)
        {
            double xNum = double.Parse(xValue);
            double yNum = double.Parse(yValue);
            result = yNum.CompareTo(xNum); // От большего к меньшему
        }
        else
        {
            result = string.Compare(xValue, yValue, StringComparison.Ordinal); // По алфавиту
        }

        if (result == 0)
        {
            result = x.FileIndex.CompareTo(y.FileIndex);
        }

        return result;
    }
}