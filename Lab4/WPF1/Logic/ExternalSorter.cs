namespace WPF1.Logic;

public static class ExternalSorter
{
    public static void MergeSort(string inputFilePath, string filterColumn, string filterValue, string sortKey, string outputFilePath, string sortingMethod)
    {
        var table = Table.LoadFromFile(inputFilePath);
        var filteredRows = table.Rows.Where(row => row[filterColumn].Equals(filterValue)).ToList();
        
        List<Dictionary<string, string>> sortedRows;
        switch (sortingMethod)
        {
            case "Прямое слияние":
                sortedRows = DirectSort(filteredRows, sortKey);
                break;
            case "Естественное слияние":
                sortedRows = NaturalMergeSort(filteredRows, sortKey);
                break;
            case "Многопутевое слияние":
                sortedRows = MultiwayMergeSort(filteredRows, sortKey);
                break;
            default:
                throw new ArgumentException("Неизвестный метод сортировки.");
        }
        
        Table sortedTable = new Table
        {
            Columns = table.Columns,
            Rows = sortedRows
        };
        sortedTable.SaveToFile(outputFilePath);
    }

    // Прямая сортировка (например, Quick Sort)
    private static List<Dictionary<string, string>> DirectSort(List<Dictionary<string, string>> rows, string sortKey)
    {
        return rows.OrderBy(row => row[sortKey], StringComparer.Ordinal).ToList();
    }

    // Естественная сортировка слиянием
    private static List<Dictionary<string, string>> NaturalMergeSort(List<Dictionary<string, string>> rows, string sortKey)
    {
        if (rows.Count <= 1)
            return rows;

        List<List<Dictionary<string, string>>> runs = new List<List<Dictionary<string, string>>>();
        List<Dictionary<string, string>> currentRun = new List<Dictionary<string, string>> { rows[0] };

        for (int i = 1; i < rows.Count; i++)
        {
            if (string.Compare(rows[i - 1][sortKey], rows[i][sortKey], StringComparison.Ordinal) <= 0)
            {
                currentRun.Add(rows[i]);
            }
            else
            {
                runs.Add(currentRun);
                currentRun = new List<Dictionary<string, string>> { rows[i] };
            }
        }
        runs.Add(currentRun);

        while (runs.Count > 1)
        {
            List<List<Dictionary<string, string>>> mergedRuns = new List<List<Dictionary<string, string>>>();

            for (int i = 0; i < runs.Count; i += 2)
            {
                if (i + 1 < runs.Count)
                {
                    mergedRuns.Add(Merge(runs[i], runs[i + 1], sortKey));
                }
                else
                {
                    mergedRuns.Add(runs[i]);
                }
            }

            runs = mergedRuns;
        }

        return runs[0];
    }

    // Многопутевая сортировка слиянием (например, K-way Merge Sort)
    private static List<Dictionary<string, string>> MultiwayMergeSort(List<Dictionary<string, string>> rows, string sortKey, int numberOfWays = 4)
    {
        if (rows.Count <= 1)
            return rows;
        
        int size = (int)Math.Ceiling((double)rows.Count / numberOfWays);
        List<List<Dictionary<string, string>>> sortedChunks = new List<List<Dictionary<string, string>>>();

        for (int i = 0; i < rows.Count; i += size)
        {
            var chunk = rows.Skip(i).Take(size).OrderBy(row => row[sortKey], StringComparer.Ordinal).ToList();
            sortedChunks.Add(chunk);
        }
        
        return KWayMerge(sortedChunks, sortKey);
    }

    // Вспомогательный метод для слияния двух списков
    private static List<Dictionary<string, string>> Merge(List<Dictionary<string, string>> left, List<Dictionary<string, string>> right, string sortKey)
    {
        List<Dictionary<string, string>> result = new List<Dictionary<string, string>>();
        int i = 0, j = 0;

        while (i < left.Count && j < right.Count)
        {
            if (string.Compare(left[i][sortKey], right[j][sortKey], StringComparison.Ordinal) <= 0)
            {
                result.Add(left[i++]);
            }
            else
            {
                result.Add(right[j++]);
            }
        }

        while (i < left.Count)
            result.Add(left[i++]);

        while (j < right.Count)
            result.Add(right[j++]);

        return result;
    }

    // Вспомогательный метод для многопутевого слияния
    private static List<Dictionary<string, string>> KWayMerge(List<List<Dictionary<string, string>>> sortedChunks, string sortKey)
    {
        List<Dictionary<string, string>> result = new List<Dictionary<string, string>>();
        var comparer = Comparer<(Dictionary<string, string> row, int chunkIndex)>.Create((a, b) =>
            string.Compare(a.row[sortKey], b.row[sortKey], StringComparison.Ordinal));

        SortedSet<(Dictionary<string, string> row, int chunkIndex)> priorityQueue = new SortedSet<(Dictionary<string, string>, int)>(comparer);
        
        int[] indices = new int[sortedChunks.Count];
        
        for (int i = 0; i < sortedChunks.Count; i++)
        {
            if (sortedChunks[i].Count > 0)
            {
                priorityQueue.Add((sortedChunks[i][0], i));
                indices[i] = 1;
            }
        }

        while (priorityQueue.Count > 0)
        {
            var smallest = priorityQueue.Min;
            priorityQueue.Remove(smallest);
            result.Add(smallest.row);

            int chunkIdx = smallest.chunkIndex;
            if (indices[chunkIdx] < sortedChunks[chunkIdx].Count)
            {
                priorityQueue.Add((sortedChunks[chunkIdx][indices[chunkIdx]++], chunkIdx));
            }
        }

        return result;
    }
}