using System.Text;

namespace WPF1.Logic;

public static class SortingExplanation
{
    public static string GenerateExplanation(string sortingMethod, string tableName, string filterColumn, string filterValue, string sortKey)
    {
        StringBuilder explanation = new StringBuilder();

        explanation.AppendLine();
        explanation.AppendLine("Объяснение сортировки:");
        explanation.AppendLine($"Мы работаем с таблицей \"{tableName}\".");

        // Добавляем объяснение про фильтрацию
        explanation.AppendLine($"Сначала мы отфильтровали строки таблицы, оставив только те, где столбец \"{filterColumn}\" имеет значение \"{filterValue}\".");
        explanation.AppendLine($"После фильтрации данные были упорядочены по столбцу \"{sortKey}\".");

        // Объяснение в зависимости от выбранного метода сортировки
        switch (sortingMethod)
        {
            case "Прямое слияние":
                explanation.AppendLine("Для упорядочивания данных был использован метод \"Прямое слияние\".");
                explanation.AppendLine("Прямое слияние работает следующим образом:");
                explanation.AppendLine("1. Таблица разбивается на равные части (например, по 100 строк). Каждая часть сортируется отдельно.");
                explanation.AppendLine("2. Затем отсортированные части объединяются попарно, формируя всё более крупные блоки.");
                explanation.AppendLine("3. На последнем этапе все данные объединяются в один упорядоченный список.");
                break;

            case "Естественное слияние":
                explanation.AppendLine("Для упорядочивания данных был использован метод \"Естественное слияние\".");
                explanation.AppendLine("Особенность этого метода заключается в том, что он ищет уже отсортированные последовательности в таблице:");
                explanation.AppendLine("1. Алгоритм автоматически находит последовательности, которые уже упорядочены (например, подряд идущие строки, где численность населения увеличивается).");
                explanation.AppendLine("2. Затем эти естественные последовательности объединяются в один отсортированный список.");
                break;

            case "Многопутевое слияние":
                explanation.AppendLine("Для упорядочивания данных был использован метод \"Многопутевое слияние\".");
                explanation.AppendLine("Этот метод работает следующим образом:");
                explanation.AppendLine("1. Таблица разбивается на несколько потоков (например, данные делятся на 4 группы).");
                explanation.AppendLine("2. Каждая группа сортируется отдельно.");
                explanation.AppendLine("3. Все группы объединяются одновременно в один список, используя специальный алгоритм выбора минимального элемента.");
                break;

            default:
                explanation.AppendLine($"Для упорядочивания данных был использован метод \"{sortingMethod}\".");
                explanation.AppendLine("Подробное объяснение для этого метода отсутствует.");
                break;
        }

        return explanation.ToString();
    }
}
