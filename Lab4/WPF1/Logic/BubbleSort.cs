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
            int[] finalized = new int[array.Length]; // Массив для отслеживания завершённых элементов

            for (int i = 0; i < n - 1; i++)
            {
                bool swapped = false;

                for (int j = 0; j < n - i - 1; j++)
                {
                    // Сравнение элементов
                    OnComparison?.Invoke(j, j + 1, $"Сравниваем: {array[j]} и {array[j + 1]}");
                    Thread.Sleep(delay);

                    if (array[j] > array[j + 1])
                    {
                        // Перестановка
                        OnComparison?.Invoke(j, j + 1, $"{array[j]} > {array[j + 1]}. Меняем местами");
                        (array[j], array[j + 1]) = (array[j + 1], array[j]);
                        swapped = true;

                        OnStepCompleted?.Invoke((int[])array.Clone()); // Обновляем массив
                    }

                    Thread.Sleep(delay);
                }

                // Помечаем элемент, который завершил участие
                finalized[n - i - 1] = 1;
                OnFinalizedElements?.Invoke((int[])finalized.Clone());
                Thread.Sleep(delay);
            }

            // Обновляем все элементы как завершённые после завершения сортировки
            for (int i = 0; i < n; i++)
            {
                finalized[i] = 1;
            }
            OnFinalizedElements?.Invoke((int[])finalized.Clone());
        }
    }
}
