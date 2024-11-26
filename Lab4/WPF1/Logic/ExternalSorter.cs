using System.Globalization;
using System.IO;
using System.Text;

namespace WPF1.Logic;

public class ExternalSorter
{
    //Вызывает конкретные алгоритмы сортировки
    public static void MergeSort(string inputFilePath, string filterColumn, string filterValue, string sortKey, string outputFilePath, string sortingMethod)
    {
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

    //Реализация прямого слияния
    public static void DirectMergeSort(string inputFilePath, string filterColumn, string filterValue, string sortKey, string outputFilePath)
    {
        var tempFiles = SplitIntoSortedChunks(inputFilePath, filterColumn, filterValue, sortKey);
        
        while (tempFiles.Count > 1)
        {
            var mergedTempFiles = new List<string>();

            for (int i = 0; i < tempFiles.Count; i += 2)
            {
                if (i + 1 < tempFiles.Count)
                {
                    var mergedFile = Path.GetTempFileName();
                    MergeTwoFiles(tempFiles[i], tempFiles[i + 1], sortKey, mergedFile);
                    mergedTempFiles.Add(mergedFile);
                    
                    File.Delete(tempFiles[i]);
                    File.Delete(tempFiles[i + 1]);
                }
                else
                {
                    mergedTempFiles.Add(tempFiles[i]);
                }
            }

            tempFiles = mergedTempFiles;
        }
        
        if (tempFiles.Count == 1)
        {
            File.Move(tempFiles[0], outputFilePath, true);
        }
        else
        {
            throw new Exception("Ошибка при слиянии файлов.");
        }
    }

    //Реализация естественного слияния
    public static void NaturalMergeSort(string inputFilePath, string filterColumn, string filterValue, string sortKey, string outputFilePath)
    {
        string tempFile1 = Path.GetTempFileName();
        string tempFile2 = Path.GetTempFileName();
        
        CreateInitialRunsForNaturalMerge(inputFilePath, filterColumn, filterValue, sortKey, tempFile1, tempFile2);

        bool isSorted = false;
        while (!isSorted)
        {
            isSorted = MergeRuns(tempFile1, tempFile2, sortKey);
        }
        
        File.Move(tempFile1, outputFilePath, true);
        File.Delete(tempFile2);
    }
    
    //Реализация многопутевого слияния
    public static void MultiwayMergeSort(string inputFilePath, string filterColumn, string filterValue, string sortKey, string outputFilePath)
    {
        var tempFiles = SplitIntoSortedChunks(inputFilePath, filterColumn, filterValue, sortKey);
        MergeChunks(tempFiles, sortKey, outputFilePath);
        
        foreach (var file in tempFiles)
        {
            File.Delete(file);
        }
    }

//////////////////////////////////////Методы для слияния данных
    
    //Сливает два отсортированных файла в один
    private static void MergeTwoFiles(string file1, string file2, string sortKey, string outputFile)
    {
        using var reader1 = new StreamReader(file1);
        using var reader2 = new StreamReader(file2);
        using var writer = new StreamWriter(outputFile);

        var header1 = reader1.ReadLine();
        var header2 = reader2.ReadLine();
        
        writer.WriteLine(header1);

        var headers = header1.Split(',').ToList();
        int sortKeyIndex = headers.IndexOf(sortKey);

        bool isNumeric = IsNumericColumnInFiles(new List<string> { file1, file2 }, sortKeyIndex);

        var comparer = new FileLineComparer(sortKeyIndex, isNumeric);
        
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
    
    //Обрабатывает слияние серий для естественного слияния
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
        
        File.Delete(inputFile1);
        File.Delete(inputFile2);

        File.Move(tempOutputFile1, inputFile1);
        File.Move(tempOutputFile2, inputFile2);

        return isSorted;
    }
    
    //Сливает несколько отсортированных файлов в один
    private static void MergeChunks(List<string> tempFiles, string sortKey, string outputFilePath)
    {
        var readers = new List<StreamReader>();
        try
        {
            foreach (var file in tempFiles)
            {
                readers.Add(new StreamReader(file));
            }

            string headerLine = readers[0].ReadLine();
            var headers = headerLine.Split(',').ToList();
            int sortKeyIndex = headers.IndexOf(sortKey);
            if (sortKeyIndex == -1) throw new Exception("Некорректный ключ сортировки.");

            bool isNumeric = IsNumericInFiles(readers, sortKeyIndex);
            var comparer = new FileLineComparer(sortKeyIndex, isNumeric);
            var priorityQueue = new List<FileLine>();

            for (int i = 0; i < readers.Count; i++)
            {
                var line = readers[i].ReadLine();
                if (!string.IsNullOrWhiteSpace(line))
                {
                    var fields = line.Split(',');
                    priorityQueue.Add(new FileLine { Fields = fields, FileIndex = i });
                }
            }

            priorityQueue.Sort(comparer);

            using (var writer = new StreamWriter(outputFilePath, false, Encoding.UTF8))
            {
                writer.WriteLine(string.Join(",", headers));

                while (priorityQueue.Count > 0)
                {
                    var nextItem = priorityQueue[0];
                    priorityQueue.RemoveAt(0);

                    writer.WriteLine(string.Join(",", nextItem.Fields));

                    var reader = readers[nextItem.FileIndex];
                    var line = reader.ReadLine();
                    if (!string.IsNullOrWhiteSpace(line))
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
                reader?.Close();
            }
        }
    }

//////////////////////////////////////Методы для обработки частей данных
    
    //Разделяет входной файл на отсортированные части
    private static List<string> SplitIntoSortedChunks(string inputFilePath, string filterColumn, string filterValue, string sortKey)
    {
        const int chunkSize = 1000;
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
                    continue;

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
            
            if (chunkLines.Count > 0)
            {
                SortAndSaveChunk(chunkLines, headers, sortKeyIndex, tempFiles);
            }
        }

        return tempFiles;
    }

    //Сортирует и сохраняет один кусок данных
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
    
    //Считывает последовательную серию строк из файла для слияния
    private static Queue<FileLine> ReadRun(StreamReader reader, int sortKeyIndex, FileLineComparer comparer)
    {
        var queue = new Queue<FileLine>();
        FileLine prevLine = null;

        while (!reader.EndOfStream)
        {
            var line = reader.ReadLine();
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var fields = line.Split(',');
            if (fields.Length <= sortKeyIndex)
            {
                Console.WriteLine($"Пропущена строка: {line} (недостаточно полей)");
                continue;
            }

            var currentLine = new FileLine { Fields = fields };

            if (prevLine != null && comparer.Compare(prevLine, currentLine) > 0)
            {
                Console.WriteLine($"Нарушение порядка: {string.Join(",", prevLine.Fields)} -> {string.Join(",", currentLine.Fields)}");
                break;
            }

            queue.Enqueue(currentLine);
            prevLine = currentLine;
        }

        return queue;
    }
    
//////////////////////////////////////Методы для определения типа данных
    
    //Является ли столбец числовым, анализируя данные в нескольких файлах
    private static bool IsNumericColumnInFiles(List<string> filePaths, int columnIndex)
    {
        var culture = CultureInfo.InvariantCulture;
        foreach (var filePath in filePaths)
        {
            using var reader = new StreamReader(filePath);
            var header = reader.ReadLine();

            string line;
            while ((line = reader.ReadLine()) != null)
            {
                var fields = line.Split(',');
                if (fields.Length > columnIndex)
                {
                    if (!double.TryParse(fields[columnIndex], NumberStyles.Any, culture, out _))
                    {
                        return false;
                    }
                }
            }
        }
        return true;
    }

    //Являются ли значения столбца числовыми, читая строки из одного файла
    private static bool IsNumericColumnInFile(StreamReader reader, int columnIndex)
    {
        long initialPosition = reader.BaseStream.Position;
        reader.DiscardBufferedData();

        var culture = CultureInfo.InvariantCulture;

        try
        {
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                var fields = line.Split(',');
                
                if (fields.Length > columnIndex)
                {
                    var value = fields[columnIndex];
                    
                    if (!double.TryParse(value, NumberStyles.Any, culture, out _))
                    {
                        return false;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            throw new Exception("Ошибка при проверке числового столбца", ex);
        }
        finally
        {
            reader.BaseStream.Position = initialPosition;
            reader.DiscardBufferedData();
        }

        return true;
    }

    //Является ли столбец числовым, анализируя строки одной части данных
    private static bool IsNumericColumn(List<string[]> rows, int columnIndex)
    {
        return rows.All(row => double.TryParse(row[columnIndex], out _));
    }

    //Содержит ли столбец числовые значения, анализируя строки в нескольких файловых потоках
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
    
//////////////////////////////////////Методы для определения типа данных

    //Создаёт начальные серии для естественного слияния
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
        
        reader.BaseStream.Position = 0;
        reader.DiscardBufferedData();
        reader.ReadLine();

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
}

//Представляет одну строку из файла
public class FileLine
{
    public string[] Fields { get; set; }
    public int FileIndex { get; set; }
}

//Реализует интерфейс для сравнения двух объектов FileLine
public class FileLineComparer : IComparer<FileLine>
{
    private readonly int _sortKeyIndex;
    private readonly bool _isNumeric;

    public FileLineComparer(int sortKeyIndex, bool isNumeric)
    {
        _sortKeyIndex = sortKeyIndex;
        _isNumeric = isNumeric;
    }

    //Сравнивает два объекта FileLine на основе значения ключа сортировки
    public int Compare(FileLine x, FileLine y)
    {
        string xValue = x.Fields[_sortKeyIndex];
        string yValue = y.Fields[_sortKeyIndex];

        int result;
        if (_isNumeric)
        {
            double xNum = double.Parse(xValue);
            double yNum = double.Parse(yValue);
            result = yNum.CompareTo(xNum);
        }
        else
        {
            result = string.Compare(xValue, yValue, StringComparison.Ordinal);
        }

        if (result == 0)
        {
            result = x.FileIndex.CompareTo(y.FileIndex);
        }

        return result;
    }
}