namespace WPF1.Logic
{
    public interface ISortingAlgorithm
    {
        event Action<int[]> OnStepCompleted;
        event Action<int, int, string> OnComparison;
        event Action<int[]> OnFinalizedElements;

        void Sort(int[] array, int delay);
    }
}