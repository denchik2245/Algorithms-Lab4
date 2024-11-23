namespace WPF1.Logic
{
    public class QuickSort : ISortingAlgorithm
    {
        public event Action<int[]> OnStepCompleted;
        public event Action<int, int, string> OnComparison;
        public event Action<int[]> OnFinalizedElements;

        public void Sort(int[] array, int delay)
        {
            QuickSortInternal(array, 0, array.Length - 1, delay);
        }

        private void QuickSortInternal(int[] array, int low, int high, int delay)
        {
            if (low < high)
            {
                int pivotIndex = Partition(array, low, high, delay);
                QuickSortInternal(array, low, pivotIndex - 1, delay);
                QuickSortInternal(array, pivotIndex + 1, high, delay);
            }
        }

        private int Partition(int[] array, int low, int high, int delay)
        {
            int pivot = array[high];
            int i = low - 1;

            for (int j = low; j < high; j++)
            {
                OnComparison?.Invoke(j, high, $"Сравниваем: {array[j]} < {pivot}");
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
    }
}