using System.IO;

public class WordSorter
{
    public List<(string Word, int Count)> SortWords(string filePath, string algorithm)
    {
        var words = ReadWordsFromFile(filePath);

        // Сортировка
        List<string> sortedWords = algorithm switch
        {
            "Базовый или усовершенствованный" => QuickSort(words),
            "Radix сортировка" => RadixSort(words),
            _ => throw new NotImplementedException("Алгоритм не поддерживается.")
        };

        // Подсчет частот
        return CountWordFrequencies(sortedWords);
    }

    private List<string> ReadWordsFromFile(string filePath)
    {
        try
        {
            string text = File.ReadAllText(filePath);
            return text.Split(new[] { ' ', '\r', '\n', '\t', ',', '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries)
                       .Select(word => word.ToLowerInvariant())
                       .ToList();
        }
        catch (Exception ex)
        {
            throw new Exception($"Ошибка при чтении файла: {ex.Message}");
        }
    }

    private List<string> QuickSort(List<string> words)
    {
        if (words.Count <= 1) return words;

        string pivot = words[words.Count / 2];
        var left = words.Where(w => string.Compare(w, pivot, StringComparison.Ordinal) < 0).ToList();
        var middle = words.Where(w => string.Compare(w, pivot, StringComparison.Ordinal) == 0).ToList();
        var right = words.Where(w => string.Compare(w, pivot, StringComparison.Ordinal) > 0).ToList();

        return QuickSort(left).Concat(middle).Concat(QuickSort(right)).ToList();
    }

    private List<string> RadixSort(List<string> words)
    {
        int maxLength = words.Max(word => word.Length);

        for (int pos = maxLength - 1; pos >= 0; pos--)
        {
            words = words.OrderBy(word => pos < word.Length ? word[pos] : '\0').ToList();
        }

        return words;
    }

    private List<(string Word, int Count)> CountWordFrequencies(List<string> sortedWords)
    {
        var wordFrequencies = new List<(string Word, int Count)>();
        int count = 1;

        for (int i = 1; i < sortedWords.Count; i++)
        {
            if (sortedWords[i] == sortedWords[i - 1])
            {
                count++;
            }
            else
            {
                wordFrequencies.Add((sortedWords[i - 1], count));
                count = 1;
            }
        }

        // Добавляем последнее слово
        if (sortedWords.Count > 0)
        {
            wordFrequencies.Add((sortedWords[^1], count));
        }

        return wordFrequencies;
    }
}
