namespace WPF1.Logic;


public class RadixSortSorter : IWordSorter
{
    public List<string> Sort(List<string> words)
    {
        if (words == null || words.Count == 0)
        {
            return new List<string>();
        }

        int maxLength = words.Max(word => word.Length); // Максимальная длина строки
        string[] buffer1 = words.ToArray();            // Преобразуем в массив для оптимальной работы
        string[] buffer2 = new string[buffer1.Length]; // Вспомогательный массив для перестановок
        int[] counts = new int[28];                    // Массив для подсчета частот (26 букв + прочие)

        for (int pos = maxLength - 1; pos >= 0; pos--)
        {
            Array.Clear(counts, 0, counts.Length);     // Сброс массива частот

            // Подсчет количества символов по bucket'ам
            foreach (string word in buffer1)
            {
                int bucketIndex = GetBucketIndex(word, pos) + 1; // Индексация с 1 (для пустых строк 0)
                counts[bucketIndex]++;
            }

            // Построение префиксных сумм
            for (int i = 1; i < counts.Length; i++)
            {
                counts[i] += counts[i - 1];
            }

            // Распределение слов по bucket'ам
            for (int i = buffer1.Length - 1; i >= 0; i--)
            {
                int bucketIndex = GetBucketIndex(buffer1[i], pos) + 1;
                buffer2[--counts[bucketIndex]] = buffer1[i];
            }

            // Обмен буферов
            (buffer1, buffer2) = (buffer2, buffer1);
        }

        // Возвращаем результат в виде List<string>
        return new List<string>(buffer1);
    }

    private int GetBucketIndex(string word, int pos)
    {
        if (pos >= word.Length) return -1; // Пустой символ -> индекс 0
        char currentChar = word[pos];
        return currentChar switch
        {
            >= 'a' and <= 'z' => currentChar - 'a', // Индекс буквы
            _ => 26                                 // Прочие символы
        };
    }
}