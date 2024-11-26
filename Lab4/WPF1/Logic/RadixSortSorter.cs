namespace WPF1.Logic;

public class RadixSortSorter : IWordSorter
{
    public List<string> Sort(List<string> words)
    {
        int maxLength = words.Max(word => word.Length);

        for (int pos = maxLength - 1; pos >= 0; pos--)
        {
            words = words.OrderBy(word => pos < word.Length ? word[pos] : '\0').ToList();
        }

        return words;
    }
}