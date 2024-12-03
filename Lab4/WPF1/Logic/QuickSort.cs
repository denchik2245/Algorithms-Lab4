namespace WPF1.Logic
{
    public class QuickSort : ISortingAlgorithm
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
            OnExplanation?.Invoke("Начинаем QuickSort...");
            Thread.Sleep(delay);

            QuickSortRecursive(array, 0, array.Length - 1, delay);

            OnExplanation?.Invoke("Сортировка завершена.");
            SortingCompleted?.Invoke();
        }

        private void QuickSortRecursive(int[] array, int low, int high, int delay)
        {
            if (low < high)
            {
                pauseEvent.Wait();

                if (isStopped)
                    return;

                int pi = Partition(array, low, high, delay);

                QuickSortRecursive(array, low, pi - 1, delay);
                QuickSortRecursive(array, pi + 1, high, delay);
            }
        }

        private int Partition(int[] array, int low, int high, int delay)
        {
            int pivot = array[high]; // выбираем опорный элемент
            OnExplanation?.Invoke($"Выбран опорный элемент: {pivot}");
            Thread.Sleep(delay);

            int i = (low - 1);

            for (int j = low; j < high; j++)
            {
                pauseEvent.Wait();

                if (isStopped)
                    return i + 1;

                OnComparison?.Invoke(j, high, -1);
                OnExplanation?.Invoke($"Сравниваем {array[j]} и {pivot}");
                Thread.Sleep(delay);

                if (array[j] < pivot)
                {
                    i++;

                    OnSwap?.Invoke(i, j);
                    OnExplanation?.Invoke($"Меняем местами {array[i]} и {array[j]}");
                    Thread.Sleep(delay);

                    int temp = array[i];
                    array[i] = array[j];
                    array[j] = temp;

                    OnStepCompleted?.Invoke((int[])array.Clone());
                }
            }

            OnSwap?.Invoke(i + 1, high);
            OnExplanation?.Invoke($"Перемещаем опорный элемент на позицию {i + 1}");
            Thread.Sleep(delay);

            int temp1 = array[i + 1];
            array[i + 1] = array[high];
            array[high] = temp1;

            OnStepCompleted?.Invoke((int[])array.Clone());

            return i + 1;
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