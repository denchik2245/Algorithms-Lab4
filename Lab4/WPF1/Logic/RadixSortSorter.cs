namespace WPF1.Logic;


public class RadixSortSorter : IWordSorter
{
    public List<string> Sort(List<string> words)
    {
        if (words == null || words.Count == 0)
            return new List<string>();

        int maxLength = words.Max(word => word.Length);

        for (int pos = maxLength - 1; pos >= 0; pos--)
        {
            // Расширяем диапазон символов для сортировки (например, от a до z + резерв для остальных)
            int bucketCount = 27; // 26 букв + 1 для остальных символов
            List<string>[] buckets = new List<string>[bucketCount];
            for (int i = 0; i < bucketCount; i++)
            {
                buckets[i] = new List<string>();
            }

            // Распределяем слова по "ведрам" на основе текущего символа
            foreach (var word in words)
            {
                if (pos < word.Length)
                {
                    char currentChar = char.ToLower(word[pos]); // Приводим к нижнему регистру
                    if (currentChar >= 'a' && currentChar <= 'z')
                    {
                        int bucketIndex = currentChar - 'a';
                        buckets[bucketIndex].Add(word);
                    }
                    else
                    {
                        // Если символ не в диапазоне a-z, добавляем в последнее "ведро"
                        buckets[26].Add(word);
                    }
                }
                else
                {
                    buckets[0].Add(word); // Слова, длина которых меньше текущей позиции
                }
            }

            // Собираем слова из "ведер"
            words = buckets.SelectMany(bucket => bucket).ToList();
        }

        return words;
    }
}
