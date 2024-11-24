public class RadixSortStrings : IStringSortingAlgorithm
{
    public void Sort(string[] array)
    {
        int maxLength = array.Max(word => word.Length);
        for (int pos = maxLength - 1; pos >= 0; pos--)
        {
            CountingSortByCharacter(array, pos);
        }
    }

    private void CountingSortByCharacter(string[] array, int charPosition)
    {
        int range = 256;
        int[] count = new int[range];
        string[] output = new string[array.Length];

        foreach (var word in array)
        {
            char ch = charPosition < word.Length ? word[charPosition] : '\0';
            count[ch]++;
        }

        for (int i = 1; i < range; i++)
        {
            count[i] += count[i - 1];
        }

        for (int i = array.Length - 1; i >= 0; i--)
        {
            char ch = charPosition < array[i].Length ? array[i][charPosition] : '\0';
            output[--count[ch]] = array[i];
        }

        for (int i = 0; i < array.Length; i++)
        {
            array[i] = output[i];
        }
    }
}