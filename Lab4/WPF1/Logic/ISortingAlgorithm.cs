namespace WPF1.Logic
{
    public interface ISortingAlgorithm
    {
        event Action<int[]> OnStepCompleted;
        event Action<int, int, int> OnComparison; // Заменяем string на int
        event Action<int, int> OnSwap;
        event Action<int[]> OnFinalizedElements;
        event Action SortingCompleted;
        event Action<string> OnExplanation;
        void Sort(int[] array, int delay);
        void Stop();
        void Resume();
    }

}