namespace WPF1.Logic
{
    public class QuickSort : ISortingAlgorithm
{
    public event Action<int[]> OnStepCompleted;
    public event Action<int, int, string> OnComparison;
    public event Action<int, int> OnSwap;
    public event Action<int[]> OnFinalizedElements;
    public event Action SortingCompleted;
    public event Action<string> OnExplanation;

    private ManualResetEventSlim pauseEvent = new ManualResetEventSlim(true);
    private volatile bool isStopped = false;
    private bool isResumed = false;
    private Stack<(int low, int high)> stack = new();
    private bool[] finalized;

    public void Sort(int[] array, int delay)
    {
        if (!isResumed)
        {
            stack.Clear();
            stack.Push((0, array.Length - 1));
            finalized = new bool[array.Length];
        }

        while (stack.Count > 0)
        {
            pauseEvent.Wait();
            
            if (isStopped)
            {
                return;
            }

            var (low, high) = stack.Pop();
            if (low < high)
            {
                int pivotIndex = Partition(array, low, high, delay);
                if (pivotIndex == -1)
                    return;
                
                finalized[pivotIndex] = true;
                OnFinalizedElements?.Invoke(GetFinalizedIndices());
                
                stack.Push((low, pivotIndex - 1));
                stack.Push((pivotIndex + 1, high));
            }
            else if (low == high)
            {
                finalized[low] = true;
                OnFinalizedElements?.Invoke(GetFinalizedIndices());
            }
        }

        isResumed = false;
        SortingCompleted?.Invoke();
    }

    private int Partition(int[] array, int low, int high, int delay)
    {
        int pivot = array[high];
        int i = low - 1;

        for (int j = low; j < high; j++)
        {
            pauseEvent.Wait();

            if (isStopped)
                return -1;

            OnComparison?.Invoke(j, high, $"Сравниваем: {array[j]} < {pivot}");
            Thread.Sleep(delay);

            if (array[j] < pivot)
            {
                i++;
                (array[i], array[j]) = (array[j], array[i]);
                OnStepCompleted?.Invoke((int[])array.Clone());
                Thread.Sleep(delay);
            }
        }

        (array[i + 1], array[high]) = (array[high], array[i + 1]);
        OnComparison?.Invoke(i + 1, high, $"Перемещаем опорный элемент: {array[i + 1]}");
        OnStepCompleted?.Invoke((int[])array.Clone());
        Thread.Sleep(delay);

        return i + 1;
    }

    public void Stop()
    {
        pauseEvent.Reset();
    }

    public void Resume()
    {
        pauseEvent.Set();
    }
    
    private int[] GetFinalizedIndices()
    {
        List<int> finalizedIndices = new List<int>();
        for (int i = 0; i < finalized.Length; i++)
        {
            if (finalized[i])
                finalizedIndices.Add(i);
        }
        return finalizedIndices.ToArray();
    }
}

}