using System;
using System.Threading;

namespace WPF1.Logic
{
    public class BubbleSort : ISortingAlgorithm
    {
        public event Action<int[]> OnStepCompleted;
        public event Action<int, int, string> OnComparison;
        public event Action<int[]> OnFinalizedElements;
        public event Action SortingCompleted;

        private ManualResetEventSlim pauseEvent = new ManualResetEventSlim(true); // Изначально не приостановлено
        private volatile bool isStopped = false;
        private int currentI = 0;
        private int currentJ = 0;

        public void Sort(int[] array, int delay)
        {
            int n = array.Length;
            int[] finalized = new int[array.Length];

            for (int i = currentI; i < n - 1; i++)
            {
                currentI = i;
                bool swapped = false;

                for (int j = currentJ; j < n - i - 1; j++)
                {
                    currentJ = j;

                    pauseEvent.Wait(); // Ожидаем, если сортировка приостановлена

                    // Проверяем, была ли сортировка полностью остановлена
                    if (isStopped)
                    {
                        return;
                    }

                    OnComparison?.Invoke(j, j + 1, $"Сравниваем: {array[j]} и {array[j + 1]}");
                    Thread.Sleep(delay);

                    if (array[j] > array[j + 1])
                    {
                        // Обмен элементов
                        (array[j], array[j + 1]) = (array[j + 1], array[j]);
                        swapped = true;
                        OnStepCompleted?.Invoke((int[])array.Clone());
                    }
                }

                finalized[n - i - 1] = 1;
                OnFinalizedElements?.Invoke((int[])finalized.Clone());
                Thread.Sleep(delay);

                if (!swapped)
                    break;

                currentJ = 0; // Сброс индекса j для следующего i
            }

            // Сортировка завершена
            currentI = 0;
            currentJ = 0;
            SortingCompleted?.Invoke();
        }

        public void Stop()
        {
            pauseEvent.Reset(); // Приостанавливаем сортировку
        }

        public void Resume()
        {
            pauseEvent.Set(); // Возобновляем сортировку
        }
    }
}