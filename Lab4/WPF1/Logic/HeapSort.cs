using WPF1.Logic;

public class HeapSort : ISortingAlgorithm
{
    public event Action<int[]> OnStepCompleted;
    public event Action<int, int, int> OnComparison;
    public event Action<int, int> OnSwap;
    public event Action<int[]> OnFinalizedElements;
    public event Action SortingCompleted;
    public event Action<string> OnExplanation;

    private ManualResetEventSlim pauseEvent = new ManualResetEventSlim(true);
    private volatile bool isStopped = false;

    private int buildHeapStep = 1;
    private int sortStep = 1;

    public void Sort(int[] array, int delay)
    {
        Task.Run(() => HeapSortAlgorithm(array, delay));
    }

    private void HeapSortAlgorithm(int[] array, int delay)
    {
        int n = array.Length;

        // Исходный массив
        OnExplanation?.Invoke($"Исходный массив: {string.Join(",", array)}\n");

        // Этап 1: Построение кучи
        OnExplanation?.Invoke("Построение кучи:");
        for (int i = n / 2 - 1; i >= 0; i--)
        {
            Heapify(array, n, i, delay, isBuildingHeap: true);
        }
        OnExplanation?.Invoke($"Массив: {string.Join(",", array)}\n");

        // Этап 2: Сортировка
        OnExplanation?.Invoke("Сортировка:");
        for (int i = n - 1; i >= 0; i--)
        {
            pauseEvent.Wait();

            if (isStopped)
                return;

            // Объяснение замены корня с последним элементом
            string mainSwapExplanation = $"{sortStep}. {array[0]}(корень) меняется с {array[i]}. Перестраиваем кучу:";
            OnExplanation?.Invoke(mainSwapExplanation);

            // Выполнение замены
            Swap(array, 0, i);
            OnSwap?.Invoke(0, i);
            OnStepCompleted?.Invoke((int[])array.Clone());
            Thread.Sleep(delay);

            // Восстановление кучи и сбор описаний перестановок
            List<string> swapDescriptions = new List<string>();
            Heapify(array, i, 0, delay, isBuildingHeap: false, swapDescriptions);

            if (swapDescriptions.Count > 0)
            {
                string swaps = string.Join(", ", swapDescriptions);
                OnExplanation?.Invoke(swaps);
            }

            OnExplanation?.Invoke($"Массив: {string.Join(",", array)}");
            sortStep++;
        }

        OnExplanation?.Invoke($"Массив отсортирован: {string.Join(",", array)}.");
        SortingCompleted?.Invoke();
    }

    private void Heapify(int[] array, int heapSize, int rootIndex, int delay, bool isBuildingHeap, List<string> swapDescriptions = null)
    {
        int largest = rootIndex;
        int leftChild = 2 * rootIndex + 1;
        int rightChild = 2 * rootIndex + 2;

        // Значения узла и его потомков
        string nodeValue = array[rootIndex].ToString();
        string leftDesc = leftChild < heapSize ? array[leftChild].ToString() : "нету";
        string rightDesc = rightChild < heapSize ? array[rightChild].ToString() : "нету";

        if (isBuildingHeap)
        {
            bool swapped = false;

            if (leftChild < heapSize && array[leftChild] > array[largest])
            {
                largest = leftChild;
                swapped = true;
            }

            if (rightChild < heapSize && array[rightChild] > array[largest])
            {
                largest = rightChild;
                swapped = true;
            }

            string comparisonResult = swapped
                ? $"{array[rootIndex]} меняется с {array[largest]}"
                : $"{array[rootIndex]} остается на месте";

            OnExplanation?.Invoke($"{buildHeapStep}. Узел {nodeValue} сравнивается с {leftDesc} и {rightDesc} = {comparisonResult}");
            buildHeapStep++;
        }

        pauseEvent.Wait();

        if (isStopped)
            return;

        Thread.Sleep(delay);

        if (leftChild < heapSize && array[leftChild] > array[largest])
        {
            largest = leftChild;
        }

        if (rightChild < heapSize && array[rightChild] > array[largest])
        {
            largest = rightChild;
        }

        if (largest != rootIndex)
        {
            // Объяснение замены во время сортировки
            if (!isBuildingHeap && swapDescriptions != null)
            {
                swapDescriptions.Add($"{array[largest]} меняется с {array[rootIndex]}");
            }

            Swap(array, rootIndex, largest);
            OnSwap?.Invoke(rootIndex, largest);
            OnStepCompleted?.Invoke((int[])array.Clone());
            Thread.Sleep(delay);

            Heapify(array, heapSize, largest, delay, isBuildingHeap, swapDescriptions);
        }
    }

    private void Swap(int[] array, int index1, int index2)
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
}