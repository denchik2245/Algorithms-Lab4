namespace WPF1.Logic
{
    public class HeapSort : IAsyncSortingAlgorithm
    {
        public event Action SortingCompleted;

        private Func<int, int, int, Task> showComparisonAsync;
        private Func<int, int, Task> showSwapAsync;
        private Func<string, Task> showExplanationAsync;
        private Action<int[]> showFinalizedElements;

        private ManualResetEventSlim pauseEvent = new ManualResetEventSlim(true);
        private volatile bool isStopped = false;

        public HeapSort(
            Func<int, int, int, Task> showComparisonAsync,
            Func<int, int, Task> showSwapAsync,
            Func<string, Task> showExplanationAsync,
            Action<int[]> showFinalizedElements)
        {
            this.showComparisonAsync = showComparisonAsync;
            this.showSwapAsync = showSwapAsync;
            this.showExplanationAsync = showExplanationAsync;
            this.showFinalizedElements = showFinalizedElements;
        }

        public async Task SortAsync(int[] array, int delay)
        {
            int n = array.Length;
            int[] finalized = new int[n];

            await showExplanationAsync("Начинаем построение кучи...");
            await Task.Delay(delay);
            
            for (int i = n / 2 - 1; i >= 0; i--)
            {
                await Heapify(array, n, i, delay);
            }

            await showExplanationAsync("Куча построена: " + string.Join(",", array));
            await Task.Delay(delay);
            
            for (int i = n - 1; i >= 0; i--)
            {
                pauseEvent.Wait();

                if (isStopped)
                    return;
                
                await showExplanationAsync($"Меняем местами {array[0]} и {array[i]}");
                await showSwapAsync(0, i);
                await Task.Delay(delay);

                (array[0], array[i]) = (array[i], array[0]);

                finalized[i] = 1;
                showFinalizedElements?.Invoke((int[])finalized.Clone());
                await showExplanationAsync($"Состояние массива: {string.Join(",", array)}");
                await Task.Delay(delay);
                
                await Heapify(array, i, 0, delay);
            }

            SortingCompleted?.Invoke();
        }

        private async Task Heapify(int[] array, int n, int i, int delay)
        {
            int largest = i;
            int l = 2 * i + 1;
            int r = 2 * i + 2;

            pauseEvent.Wait();

            if (isStopped)
                return;
            
            if (l < n)
            {
                await showExplanationAsync($"Сравниваем {array[i]} и левый потомок {array[l]}");
                await showComparisonAsync(i, l, -1);
                await Task.Delay(delay);

                if (array[l] > array[largest])
                    largest = l;
                
                await showComparisonAsync(-1, -1, -1);
            }

            pauseEvent.Wait();

            if (isStopped)
                return;
            
            if (r < n)
            {
                await showExplanationAsync($"Сравниваем {array[largest]} и правый потомок {array[r]}");
                await showComparisonAsync(largest, r, -1);
                await Task.Delay(delay);

                if (array[r] > array[largest])
                    largest = r;
                
                await showComparisonAsync(-1, -1, -1);
            }
            
            if (largest != i)
            {
                await showExplanationAsync($"Меняем местами {array[i]} и {array[largest]}");
                await showSwapAsync(i, largest);
                await Task.Delay(delay);

                (array[i], array[largest]) = (array[largest], array[i]);

                await Heapify(array, n, largest, delay);
            }
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
