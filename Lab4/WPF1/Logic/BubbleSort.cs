namespace WPF1.Logic
{
    public class BubbleSort : ISortingAlgorithm
    {
        public event Action<int[]> OnStepCompleted;
        public event Action<int, int, int> OnComparison;
        public event Action<int, int> OnSwap;
        public event Action<int[]> OnFinalizedElements;
        public event Action SortingCompleted;
        public event Action<string> OnExplanation;

        private ManualResetEventSlim pauseEvent = new ManualResetEventSlim(true);
        private volatile bool isStopped = false;

        public void Sort(int[] array, int delay)
        {
            int n = array.Length;
            int[] finalized = new int[array.Length];
            
            OnExplanation?.Invoke("Входной массив:\n" + string.Join(",", array));
            Thread.Sleep(delay);

            for (int i = 0; i < n - 1; i++)
            {
                bool swapped = false;
                OnExplanation?.Invoke($"\nИтерация {i + 1}:");
                Thread.Sleep(delay);

                for (int j = 0; j < n - i - 1; j++)
                {
                    pauseEvent.Wait();

                    if (isStopped)
                        return;

                    OnExplanation?.Invoke($"Сравниваем {array[j]} и {array[j + 1]}");
                    OnComparison?.Invoke(j, j + 1, -1);
                    Thread.Sleep(delay);

                    if (array[j] > array[j + 1])
                    {
                        OnExplanation?.Invoke($"{array[j]} > {array[j + 1]}, меняем местами");
                        (array[j], array[j + 1]) = (array[j + 1], array[j]);
                        swapped = true;
                        OnSwap?.Invoke(j, j + 1);
                        Thread.Sleep(delay);
                    }
                    else
                    {
                        OnExplanation?.Invoke($"{array[j]} < {array[j + 1]}, оставляем на местах");
                        Thread.Sleep(delay);
                    }
                }

                finalized[n - i - 1] = 1;
                OnFinalizedElements?.Invoke((int[])finalized.Clone());
                OnExplanation?.Invoke($"Состояние массива: {string.Join(",", array)}\n");
                Thread.Sleep(delay);

                if (!swapped)
                {
                    OnExplanation?.Invoke("Массив отсортирован");
                    break;
                }
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
}