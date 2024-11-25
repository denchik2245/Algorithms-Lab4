using WPF1.Logic;

public class InsertSort : ISortingAlgorithm
{
    public event Action<int[]> OnStepCompleted;
    public event Action<int, int, string> OnComparison;
    public event Action<int, int> OnSwap;
    public event Action<int[]> OnFinalizedElements;
    public event Action SortingCompleted;
    public event Action<string> OnExplanation;

    private ManualResetEventSlim pauseEvent = new ManualResetEventSlim(true);
    private volatile bool isStopped = false;

    public void Sort(int[] array, int delay)
    {
        int n = array.Length;
        
        OnExplanation?.Invoke("Входной массив:\n" + string.Join(",", array));
        Thread.Sleep(delay);
        
        OnExplanation?.Invoke($"\nПервый элемент ({array[0]}) уже отсортирован.");
        OnStepCompleted?.Invoke((int[])array.Clone());
        OnExplanation?.Invoke($"Состояние массива: {string.Join(",", array)}\n");
        Thread.Sleep(delay);

        for (int i = 1; i < n; i++)
        {
            if (isStopped)
                return;

            pauseEvent.Wait();

            int j = i;
            OnExplanation?.Invoke($"Берем {i + 1}-й элемент ({array[i]}):");
            OnStepCompleted?.Invoke((int[])array.Clone());
            Thread.Sleep(delay);

            while (j > 0 && array[j - 1] > array[j])
            {
                if (isStopped)
                    return;

                pauseEvent.Wait();

                // Подсветка элементов
                OnComparison?.Invoke(j - 1, j, $"{array[j]} < {array[j - 1]}, меняем местами");
                Thread.Sleep(delay);

                // Обмен элементов
                int temp = array[j];
                array[j] = array[j - 1];
                array[j - 1] = temp;

                // Анимация обмена
                OnSwap?.Invoke(j - 1, j);
                Thread.Sleep(delay);

                OnStepCompleted?.Invoke((int[])array.Clone());
                Thread.Sleep(delay);
                j--;
            }

            if (j != i)
            {
                OnExplanation?.Invoke($"{array[j]} установили на позицию {j + 1}");
            }
            else
            {
                OnExplanation?.Invoke($"{array[j]} остается на своем месте");
            }

            OnExplanation?.Invoke($"Состояние массива: {string.Join(",", array)}\n");
            Thread.Sleep(delay);
        }

        SortingCompleted?.Invoke();
    }

    public void Stop()
    {
        isStopped = true;
        pauseEvent.Reset();
    }

    public void Resume()
    {
        isStopped = false;
        pauseEvent.Set();
    }
}






