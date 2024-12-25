namespace WPF1.Logic;

public class QuickSortSorter : IWordSorter
{
    public List<string> Sort(List<string> words)
    {
        if (words == null || words.Count <= 1)
        {
            return words; // Возвращаем список, если он пуст или содержит 1 элемент.
        }

        // Создаем копию входного списка, чтобы избежать модификации оригинального.
        var wordsCopy = new List<string>(words);

        QuickSort(wordsCopy, 0, wordsCopy.Count - 1);

        return wordsCopy;
    }

    private void QuickSort(List<string> words, int left, int right)
    {
        if (left < right)
        {
            int pivotIndex = Partition(words, left, right);

            QuickSort(words, left, pivotIndex - 1);  // Рекурсивно сортируем левую часть
            QuickSort(words, pivotIndex + 1, right); // Рекурсивно сортируем правую часть
        }
    }

    private int Partition(List<string> words, int left, int right)
    {
        string pivot = words[right]; // Берем правый элемент как опорный
        int i = left - 1;

        for (int j = left; j < right; j++)
        {
            if (string.Compare(words[j], pivot, StringComparison.Ordinal) <= 0)
            {
                i++;
                Swap(words, i, j);
            }
        }

        Swap(words, i + 1, right); // Перемещаем опорный элемент на правильное место
        return i + 1;
    }

    private void Swap(List<string> words, int i, int j)
    {
        string temp = words[i];
        words[i] = words[j];
        words[j] = temp;
    }
}