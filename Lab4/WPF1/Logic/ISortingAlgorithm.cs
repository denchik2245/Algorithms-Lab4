namespace WPF1.Logic
{
    public interface ISortingAlgorithm
    {
        event Action<int[]> OnStepCompleted; // Событие для обновления массива
        event Action<int, int, string> OnComparison; // Событие для сравнения элементов
        event Action<int[]> OnFinalizedElements; // Событие для завершённых элементов

        void Sort(int[] array, int delay); // Метод сортировки
    }
}