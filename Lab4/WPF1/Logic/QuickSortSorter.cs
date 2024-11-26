namespace WPF1.Logic;

public class QuickSortSorter : IWordSorter
{
    public List<string> Sort(List<string> words)
    {
        if (words.Count <= 1) return words;

        string pivot = words[words.Count / 2];
        var left = words.Where(w => string.Compare(w, pivot, StringComparison.Ordinal) < 0).ToList();
        var middle = words.Where(w => string.Compare(w, pivot, StringComparison.Ordinal) == 0).ToList();
        var right = words.Where(w => string.Compare(w, pivot, StringComparison.Ordinal) > 0).ToList();

        return Sort(left).Concat(middle).Concat(Sort(right)).ToList();
    }
}