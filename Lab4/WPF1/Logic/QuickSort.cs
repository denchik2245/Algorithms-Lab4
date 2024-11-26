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
        private Stack<(int low, int high)> stack = new();
        private bool[] finalized;

        private int[] array;
        private int delay;
        private int sortStep = 1;

        public void Sort(int[] array, int delay)
        {
            this.array = array;
            this.delay = delay;

            // Исходный массив
            OnExplanation?.Invoke($"Исходный массив: {string.Join(",", array)}\n");

            // Инициализация стека и финализированных элементов
            stack.Clear();
            stack.Push((0, array.Length - 1));
            finalized = new bool[array.Length];
            sortStep = 1;

            // Запуск сортировки в отдельном потоке
            Task.Run(() => QuickSortAlgorithm());
        }

        private void QuickSortAlgorithm()
        {
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
                    OnExplanation?.Invoke($"{sortStep}. Выбираем подмассив с индексами от {low} до {high}.\n");
                    int pivotIndex = Partition(low, high);
                    if (pivotIndex == -1)
                        return;

                    finalized[pivotIndex] = true;
                    OnFinalizedElements?.Invoke(GetFinalizedIndices());

                    stack.Push((low, pivotIndex - 1));
                    stack.Push((pivotIndex + 1, high));
                    sortStep++;
                }
                else if (low == high)
                {
                    finalized[low] = true;
                    OnFinalizedElements?.Invoke(GetFinalizedIndices());
                    OnExplanation?.Invoke($"{sortStep}. Элемент с индексом {low} окончательно размещен.\n");
                    sortStep++;
                }
            }

            OnExplanation?.Invoke($"Массив отсортирован: {string.Join(",", array)}.");
            SortingCompleted?.Invoke();
        }

        private int Partition(int low, int high)
        {
            int pivot = array[high];
            OnExplanation?.Invoke($"Выбираем опорный элемент {pivot} (индекс {high}).");
            int i = low - 1;

            for (int j = low; j < high; j++)
            {
                pauseEvent.Wait();

                if (isStopped)
                    return -1;

                OnComparison?.Invoke(j, high, -1);
                OnExplanation?.Invoke($"Сравниваем элемент {array[j]} (индекс {j}) с опорным {pivot}.");

                Thread.Sleep(delay);

                if (array[j] < pivot)
                {
                    i++;
                    if (i != j)
                    {
                        OnExplanation?.Invoke($"Меняем местами {array[i]} (индекс {i}) и {array[j]} (индекс {j}).");
                        Swap(i, j);
                        OnSwap?.Invoke(i, j);
                        OnStepCompleted?.Invoke((int[])array.Clone());
                        Thread.Sleep(delay);
                    }
                }
            }

            if (i + 1 != high)
            {
                OnExplanation?.Invoke($"Меняем опорный элемент {pivot} с элементом {array[i + 1]} (индекс {i + 1}).");
                Swap(i + 1, high);
                OnSwap?.Invoke(i + 1, high);
                OnStepCompleted?.Invoke((int[])array.Clone());
                Thread.Sleep(delay);
            }
            else
            {
                OnExplanation?.Invoke($"Опорный элемент {pivot} остается на месте.");
            }

            OnExplanation?.Invoke($"Элемент {array[i + 1]} (индекс {i + 1}) установлен на окончательное место.\n");
            return i + 1;
        }

        private void Swap(int index1, int index2)
        {
            int temp = array[index1];
            array[index1] = array[index2];
            array[index2] = temp;
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