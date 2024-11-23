using System;
using System.Threading;

namespace WPF1.Logic
{
    public class BubbleSort : ISortingAlgorithm
    {
        public event Action<int[]> OnStepCompleted;
        public event Action<int, int, string> OnComparison;
        public event Action<int[]> OnFinalizedElements;

        public void Sort(int[] array, int delay)
        {
            int n = array.Length;
            int[] finalized = new int[array.Length];

            for (int i = 0; i < n - 1; i++)
            {
                bool swapped = false;

                for (int j = 0; j < n - i - 1; j++)
                {
                    // Сравниваем элементы
                    OnComparison?.Invoke(j, j + 1, $"Сравниваем: {array[j]} и {array[j + 1]}");
                    // Добавляем пробел между сообщениями
                    OnComparison?.Invoke(-1, -1, ""); // Передаем пустую строку, чтобы обработать пробел

                    Thread.Sleep(delay);

                    if (array[j] > array[j + 1])
                    {
                        // Меняем местами элементы, если первый больше второго
                        (array[j], array[j + 1]) = (array[j + 1], array[j]);
                        swapped = true;

                        // Событие вызывается только после изменения массива
                        OnStepCompleted?.Invoke((int[])array.Clone());
                    }

                    // Никакого дополнительного вызова OnComparison здесь нет
                }

                // Помечаем элемент как завершённый
                finalized[n - i - 1] = 1;
                OnFinalizedElements?.Invoke((int[])finalized.Clone());
                Thread.Sleep(delay);

                // Если обменов не было, массив уже отсортирован
                if (!swapped)
                    break;
            }

            // Помечаем все элементы как завершённые
            for (int i = 0; i < n; i++)
            {
                finalized[i] = 1;
            }
            OnFinalizedElements?.Invoke((int[])finalized.Clone());
        }
    }
}