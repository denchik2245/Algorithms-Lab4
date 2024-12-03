namespace WPF1.Logic
{
    public interface IAsyncSortingAlgorithm
    {
        event Action SortingCompleted;
        Task SortAsync(int[] array, int delay);
        void Stop();
        void Resume();
    }
}