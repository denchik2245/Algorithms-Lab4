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

    public void Sort(int[] array, int delay)
    {
        Task.Run(() => HeapSortAlgorithm(array, delay));
    }

    private void HeapSortAlgorithm(int[] array, int delay)
    {
        int n = array.Length;
        int[] finalized = new int[array.Length];

        // Этап 1: Построение кучи
        OnExplanation?.Invoke("Этап 1: Построение кучи\n");
        OnExplanation?.Invoke($"Входной массив: {FormatArray(array)}.");

        for (int i = n / 2 - 1; i >= 0; i--)
        {
            Heapify(array, n, i, delay, $"Узел с индексом {i}");
        }

        OnExplanation?.Invoke($"Теперь массив превращен в max-heap: {FormatArray(array)}.\n");

        // Этап 2: Сортировка
        OnExplanation?.Invoke("Этап 2: Сортировка\n");

        for (int i = n - 1; i >= 0; i--)
        {
            pauseEvent.Wait();

            if (isStopped)
                return;

            // Меняем местами корень и последний элемент
            OnExplanation?.Invoke($"Меняем местами корень ({array[0]}) и элемент ({array[i]}).");
            Swap(array, 0, i);
            OnSwap?.Invoke(0, i);
            Thread.Sleep(delay);

            // Восстанавливаем кучу
            OnExplanation?.Invoke($"Уменьшаем размер кучи и восстанавливаем max-heap:");
            Heapify(array, i, 0, delay, $"Узел 0 (значение {array[0]})");
            OnExplanation?.Invoke($"Новый массив: {FormatArray(array)}.\n");
        }

        OnExplanation?.Invoke($"Массив отсортирован: {FormatArray(array)}.");
    }

    private void Heapify(int[] array, int heapSize, int rootIndex, int delay, string explanationContext)
    {
        int largest = rootIndex;
        int leftChild = 2 * rootIndex + 1;
        int rightChild = 2 * rootIndex + 2;

        // Проверяем потомков
        string leftDesc = leftChild < heapSize ? array[leftChild].ToString() : "нету";
        string rightDesc = rightChild < heapSize ? array[rightChild].ToString() : "нету";
        OnExplanation?.Invoke($"{explanationContext}, потомки: {leftDesc} и {rightDesc}.");

        OnComparison?.Invoke(rootIndex, leftChild < heapSize ? leftChild : -1, rightChild < heapSize ? rightChild : -1);

        if (leftChild < heapSize)
        {
            pauseEvent.Wait();

            if (isStopped)
                return;

            Thread.Sleep(delay);

            if (array[leftChild] > array[largest])
            {
                largest = leftChild;
            }
        }

        if (rightChild < heapSize)
        {
            pauseEvent.Wait();

            if (isStopped)
                return;

            Thread.Sleep(delay);

            if (array[rightChild] > array[largest])
            {
                largest = rightChild;
            }
        }

        if (largest != rootIndex)
        {
            OnExplanation?.Invoke($"Меняем местами с {array[largest]}: {FormatArrayAfterSwap(array, rootIndex, largest)}.");
            Swap(array, rootIndex, largest);
            OnSwap?.Invoke(rootIndex, largest);
            Thread.Sleep(delay);

            // Рекурсивный вызов для затронутого поддерева
            Heapify(array, heapSize, largest, delay, $"Узел {largest} (значение {array[largest]})");
        }
        else
        {
            OnExplanation?.Invoke("Ничего не делаем.");
        }

        OnStepCompleted?.Invoke((int[])array.Clone());
    }

    private void Swap(int[] array, int index1, int index2)
    {
        int temp = array[index1];
        array[index1] = array[index2];
        array[index2] = temp;
    }

    private string FormatArray(int[] array)
    {
        return "[" + string.Join(", ", array) + "]";
    }

    private string FormatArrayAfterSwap(int[] array, int index1, int index2)
    {
        int[] tempArray = (int[])array.Clone();
        int temp = tempArray[index1];
        tempArray[index1] = tempArray[index2];
        tempArray[index2] = temp;
        return FormatArray(tempArray);
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

