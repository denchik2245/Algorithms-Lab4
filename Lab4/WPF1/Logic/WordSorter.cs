using System.IO;

namespace WPF1.Logic;

public class WordSorter
{
    private readonly IWordSorter _sorter;

    public WordSorter(IWordSorter sorter)
    {
        _sorter = sorter;
    }

    public List<(string Word, int Count)> SortWords(string filePath)
    {
        var words = ReadWordsFromFile(filePath);
        var sortedWords = _sorter.Sort(words);
        return CountWordFrequencies(sortedWords);
    }

    public List<string> ReadWordsFromFile(string filePath)
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
        
        if (sortedWords.Count > 0)
        {
            wordFrequencies.Add((sortedWords[^1], count));
        }

        return wordFrequencies;
    }
}