using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using WPF1.Logic;

namespace WPF1
{
    public partial class MainWindow : Window
    {
        private ObservableCollection<NumberItem> numbers;
        private List<int> initialNumbers;
        private ISortingAlgorithm sortingAlgorithm;
        private int delay = 500;
        private bool isPaused = false;
        private bool isSortingStarted = false;
        private Thread sortingThread;
        private string countryFilePath = "Resources/Country.txt";
        private string outputFilePath = "Resources/SortedCountries.txt";
        private CountrySorter countrySorter;
        private bool isAnimating = false;

        public MainWindow()
        {
            InitializeComponent();
            LoadArrayFromFile("Resources/ArrayForVisual.txt");
            DataContext = this;
        }

        private readonly List<string> TextSortingTasks = new List<string>
        {
            "Сортировка текста (100 слов)",
            "Сортировка текста (500 слов)",
            "Сортировка текста (1000 слов)",
            "Сортировка текста (2000 слов)",
            "Сортировка текста (5000 слов)"
        };
        
        private void TaskSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded) return; // Предотвращаем вызов кода до полной загрузки окна

            // Сбрасываем визуализацию массива
            if (initialNumbers != null && initialNumbers.Count > 0)
            {
                numbers = new ObservableCollection<NumberItem>(
                    initialNumbers.Select(num => new NumberItem { Value = num })
                );
                ArrayDisplay.ItemsSource = null;
                ArrayDisplay.ItemsSource = numbers;
            }

            // Остальная логика обработки выбора пункта меню
            OutputTextBox.Clear();
            string selectedTask = ((ComboBoxItem)TaskSelector.SelectedItem)?.Content.ToString();

            if (TextSortingTasks.Contains(selectedTask))
            {
                OutputTextBox.Height = 700;

                ArrayDisplay.Visibility = Visibility.Collapsed;
                StopButton.Visibility = Visibility.Collapsed;
                DelayInputPanel.Visibility = Visibility.Collapsed;
                ArrayInputPanel.Visibility = Visibility.Collapsed;

                CustomFilePathPanel.Visibility = Visibility.Visible;
                AlgorithmSelectorPanel.Visibility = Visibility.Visible;

                ContinentPanel.Visibility = Visibility.Collapsed;
                AttributePanel.Visibility = Visibility.Collapsed;

                OutputTextBox.Visibility = Visibility.Visible;
            }
            else if (selectedTask == "Сведения о государствах")
            {
                ArrayDisplay.Visibility = Visibility.Collapsed;
                StopButton.Visibility = Visibility.Collapsed;
                DelayInputPanel.Visibility = Visibility.Collapsed;
                ArrayInputPanel.Visibility = Visibility.Collapsed;
                CustomFilePathPanel.Visibility = Visibility.Collapsed;
                AlgorithmSelectorPanel.Visibility = Visibility.Collapsed;

                ContinentPanel.Visibility = Visibility.Visible;
                AttributePanel.Visibility = Visibility.Visible;

                OutputTextBox.Visibility = Visibility.Visible;
                OutputTextBox.Height = 700;
            }
            else
            {
                OutputTextBox.Height = 600;

                ArrayInputPanel.Visibility = Visibility.Visible;
                DelayInputPanel.Visibility = Visibility.Visible;
                ArrayDisplay.Visibility = Visibility.Visible;
                StopButton.Visibility = Visibility.Visible;

                CustomFilePathPanel.Visibility = Visibility.Collapsed;
                AlgorithmSelectorPanel.Visibility = Visibility.Collapsed;
                ContinentPanel.Visibility = Visibility.Collapsed;
                AttributePanel.Visibility = Visibility.Collapsed;

                OutputTextBox.Visibility = Visibility.Visible;
            }
        }
        
        //Считывание чисел
        private void LoadArrayFromFile(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    MessageBox.Show($"Файл не найден: {filePath}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                string[] lines = File.ReadAllText(filePath)
                    .Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                
                if (lines.Length > 16)
                {
                    MessageBox.Show("Файл содержит больше 16 чисел. Используются только первые 16.", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                }

                numbers = new ObservableCollection<NumberItem>(
                    lines.Take(16).Select(num =>
                    {
                        if (int.TryParse(num, out int value))
                        {
                            return new NumberItem { Value = value };
                        }
                        else
                        {
                            throw new FormatException($"Неверный формат числа: '{num}'");
                        }
                    })
                );

                initialNumbers = numbers.Select(n => n.Value).ToList();
                ArrayDisplay.ItemsSource = numbers;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка чтения файла: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        //Сортировка текста
        private void StartTextSorting()
        {
            string selectedTask = ((ComboBoxItem)TaskSelector.SelectedItem)?.Content.ToString();
            
            string defaultFilePath = selectedTask switch
            {
                "Сортировка текста (100 слов)" => "Resources/Text_100.txt",
                "Сортировка текста (500 слов)" => "Resources/Text_500.txt",
                "Сортировка текста (1000 слов)" => "Resources/Text_1000.txt",
                "Сортировка текста (2000 слов)" => "Resources/Text_2000.txt",
                "Сортировка текста (5000 слов)" => "Resources/Text_5000.txt",
                _ => throw new NotImplementedException("Выбранная задача сортировки текста не поддерживается.")
            };

            string customFilePath = CustomFilePathTextBox.Text.Trim();
            string filePath = string.IsNullOrEmpty(customFilePath) ? defaultFilePath : customFilePath;

            if (!File.Exists(filePath))
            {
                MessageBox.Show($"Файл не найден: {filePath}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            
            string selectedAlgorithm = BaseAlgorithmRadioButton.IsChecked == true ? "Базовый или усовершенствованный" :
                                       RadixSortRadioButton.IsChecked == true ? "Radix сортировка" : "QuickSort";

            try
            {
                WordSorter sorter = new WordSorter();
                var sortedWordsWithCounts = sorter.SortWords(filePath, selectedAlgorithm);
                
                StringBuilder sb = new StringBuilder();
                foreach (var (Word, Count) in sortedWordsWithCounts)
                {
                    sb.AppendLine($"{Word} (Встречалось {Count} {GetRussianPlural(Count)})");
                }

                OutputTextBox.Text = sb.ToString();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сортировке: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        //"1 Раз" или "3 Раза"
        private string GetRussianPlural(int count)
        {
            if (count % 10 == 1 && count % 100 != 11)
                return "раз";
            else if (count % 10 >= 2 && count % 10 <= 4 && (count % 100 < 10 || count % 100 >= 20))
                return "раза";
            else
                return "раз";
        }
        
        //Кнопка "Начать"
        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            string selectedTask = ((ComboBoxItem)TaskSelector.SelectedItem)?.Content.ToString();

            if (selectedTask == "Сведения о государствах")
            {
                StartCountrySorting();
            }
            else if (selectedTask.StartsWith("Сортировка текста"))
            {
                StartTextSorting();
            }
            else
            {
                if (isPaused && isSortingStarted)
                {
                    isPaused = false;
                    ResumeSorting();
                    return;
                }

                if (!isSortingStarted)
                {
                    string selectedAlgorithm = ((ComboBoxItem)TaskSelector.SelectedItem)?.Content.ToString();
                    if (string.IsNullOrEmpty(selectedAlgorithm))
                    {
                        MessageBox.Show("Пожалуйста, выберите алгоритм сортировки.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    if (!int.TryParse(DelayTextBox.Text, out delay) || delay < 500)
                    {
                        MessageBox.Show("Введите корректное значение задержки (минимум 500 мс).", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    ResetArrayVisuals();

                    // Проверяем ввод массива
                    string inputArrayText = ArrayInputTextBox.Text; // Предполагается, что ArrayInputTextBox — это TextBox в ArrayInputPanel
                    if (string.IsNullOrWhiteSpace(inputArrayText))
                    {
                        LoadArrayFromFile("Resources/ArrayForVisual.txt");
                    }
                    else
                    {
                        try
                        {
                            numbers = new ObservableCollection<NumberItem>(
                                inputArrayText
                                    .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                                    .Select(num =>
                                    {
                                        if (int.TryParse(num, out int value))
                                        {
                                            return new NumberItem { Value = value };
                                        }
                                        else
                                        {
                                            throw new FormatException($"Неверный формат числа: '{num}'");
                                        }
                                    })
                            );

                            initialNumbers = numbers.Select(n => n.Value).ToList();
                            
                            // Обновляем визуализацию после изменения numbers
                            ArrayDisplay.ItemsSource = null;
                            ArrayDisplay.ItemsSource = numbers;
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Ошибка ввода массива: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                    }

                    if (numbers == null || numbers.Count == 0)
                    {
                        MessageBox.Show("Массив для сортировки не задан.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    ArrayDisplay.ItemsSource = numbers;
                    initialNumbers = numbers.Select(n => n.Value).ToList();

                    // Извлечение массива для сортировки
                    int[] arrayToSort = numbers.Select(n => n.Value).ToArray();

                    sortingAlgorithm = selectedAlgorithm switch
                    {
                        "BubbleSort" => new BubbleSort(),
                        "QuickSort" => new QuickSort(),
                        "InsertSort" => new InsertSort(),
                        "HeapSort" => new HeapSort(),
                        _ => throw new NotImplementedException("Алгоритм не реализован.")
                    };

                    // Подписки на события
                    sortingAlgorithm.OnStepCompleted += UpdateArray;
                    sortingAlgorithm.SortingCompleted += OnSortingCompleted;
                    sortingAlgorithm.OnComparison += ShowComparison;
                    sortingAlgorithm.OnExplanation += ShowExplanation;

                    // Специфичные подписки для разных алгоритмов
                    if (sortingAlgorithm is BubbleSort)
                    {
                        sortingAlgorithm.OnSwap += AnimateSwapHandlerForBubbleSort;
                        sortingAlgorithm.OnFinalizedElements += ShowFinalizedElements;
                    }
                    else if (sortingAlgorithm is HeapSort)
                    {
                        sortingAlgorithm.OnComparison += async (nodeIndex, leftChildIndex, rightChildIndex) =>
                        {
                            await HandleCompareAndSwap(nodeIndex, leftChildIndex, rightChildIndex);
                        };
                    }
                    else if (sortingAlgorithm is InsertSort)
                    {
                        sortingAlgorithm.OnSwap += AnimateSwapHandlerForInsertSort;
                    }
                    
                    isSortingStarted = true;

                    sortingThread = new Thread(() =>
                    {
                        try
                        {
                            sortingAlgorithm.Sort(arrayToSort, delay);
                        }
                        catch (Exception ex)
                        {
                            Dispatcher.Invoke(() =>
                                MessageBox.Show($"Ошибка выполнения сортировки: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error));
                        }
                    })
                    {
                        IsBackground = true
                    };
                    sortingThread.Start();
                }
                else
                {
                    var result = MessageBox.Show("Сортировка уже запущена. Хотите начать заново?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (result == MessageBoxResult.Yes)
                    {
                        StopCurrentSorting();
                        ResetArrayVisuals();
                        isSortingStarted = false;
                        StartButton_Click(sender, e);
                    }
                }
            }
        }
        
        //Кнопка "Стоп"
        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            if (!isSortingStarted) return;

            if (sortingAlgorithm != null)
            {
                sortingAlgorithm.Stop();
                isPaused = true;
            }
        }

        private void ResumeSorting()
        {
            if (sortingAlgorithm == null) return;

            sortingAlgorithm.Resume();
        }

        private void StopCurrentSorting()
        {
            if (sortingAlgorithm != null)
            {
                sortingAlgorithm.Stop();
            }

            isPaused = false;
            isSortingStarted = false;
        }

        private void OnSortingCompleted()
        {
            Dispatcher.Invoke(() =>
            {
                isSortingStarted = false;
                isPaused = false;
                MessageBox.Show("Сортировка завершена!", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
            });
        }

        private void ResetArrayVisuals()
        {
            foreach (var item in numbers)
            {
                item.IsFinalized = false;
                item.IsComparing = false;
                item.XOffset = 0;
            }
    
            for (int i = 0; i < numbers.Count; i++)
            {
                numbers[i].Value = initialNumbers[i];
            }
        }
        
        private void UpdateArray(int[] newArray)
        {
            if (isAnimating) return;

            Dispatcher.Invoke(() =>
            {
                for (int i = 0; i < numbers.Count; i++)
                {
                    numbers[i].Value = newArray[i];
                }
            });
        }
        
        private async void ShowExplanation(string message)
        {
            await Dispatcher.InvokeAsync(() =>
            {
                OutputTextBox.AppendText($"{message}\n");
                OutputTextBox.ScrollToEnd();
            });

            await Task.Delay(delay);
        }
        
        private async Task HighlightElements(int index1, int index2 = -1)
        {
            await Dispatcher.InvokeAsync(() =>
            {
                foreach (var item in numbers)
                {
                    item.IsComparing = false;
                }
                
                if (index1 >= 0 && index1 < numbers.Count)
                {
                    numbers[index1].IsComparing = true;
                }

                if (index2 >= 0 && index2 < numbers.Count)
                {
                    numbers[index2].IsComparing = true;
                }
            });

            await Task.Delay(300);
        }
        
        private async Task HandleCompareAndSwap(int nodeIndex, int leftChildIndex, int rightChildIndex)
        {
            int largestIndex = nodeIndex;

            if (leftChildIndex >= 0 && leftChildIndex < numbers.Count &&
                numbers[leftChildIndex].Value > numbers[largestIndex].Value)
            {
                largestIndex = leftChildIndex;
            }

            if (rightChildIndex >= 0 && rightChildIndex < numbers.Count &&
                numbers[rightChildIndex].Value > numbers[largestIndex].Value)
            {
                largestIndex = rightChildIndex;
            }
            
            if (largestIndex != nodeIndex)
            {
                await HighlightElements(nodeIndex, largestIndex);

                await Dispatcher.InvokeAsync(() =>
                {
                    (numbers[nodeIndex].Value, numbers[largestIndex].Value) =
                        (numbers[largestIndex].Value, numbers[nodeIndex].Value);
                });
            }
            else
            {
                await HighlightElements(nodeIndex);
            }
        }
        
        private async void ShowComparison(int index1, int index2, int unused)
        {
            await Dispatcher.InvokeAsync(() =>
            {
                foreach (var item in numbers)
                {
                    item.IsComparing = false;
                }

                if (index1 >= 0 && index1 < numbers.Count)
                    numbers[index1].IsComparing = true;
                if (index2 >= 0 && index2 < numbers.Count)
                    numbers[index2].IsComparing = true;
            });

            await Task.Delay(delay);

            await Dispatcher.InvokeAsync(() =>
            {
                if (index1 >= 0 && index1 < numbers.Count)
                    numbers[index1].IsComparing = false;
                if (index2 >= 0 && index2 < numbers.Count)
                    numbers[index2].IsComparing = false;
            });
        }
        
        //Обновление элементов списка, помечая их как законченные.
        private void ShowFinalizedElements(int[] finalizedArray)
        {
            Dispatcher.Invoke(() =>
            {
                for (int i = 0; i < numbers.Count; i++)
                {
                    if (finalizedArray[i] == 1)
                    {
                        numbers[i].IsFinalized = true;
                        numbers[i].IsComparing = false;
                    }
                }
            });
        }
        
        //Анимация для BubbleSort
        private async Task AnimateSwapForBubbleSort(int index1, int index2)
        {
            const double itemWidth = 77;
            double animationDurationSeconds = 0.5;

            FrameworkElement container1 = null;
            FrameworkElement container2 = null;
            Border border1 = null;
            Border border2 = null;
            
            await Dispatcher.InvokeAsync(() =>
            {
                container1 = ArrayDisplay.ItemContainerGenerator.ContainerFromIndex(index1) as FrameworkElement;
                container2 = ArrayDisplay.ItemContainerGenerator.ContainerFromIndex(index2) as FrameworkElement;

                if (container1 != null && container2 != null)
                {
                    border1 = FindVisualChild<Border>(container1);
                    border2 = FindVisualChild<Border>(container2);
                }
            });

            if (border1 == null || border2 == null) return;

            await Dispatcher.InvokeAsync(() =>
            {
                if (!(border1.RenderTransform is TranslateTransform))
                {
                    border1.RenderTransform = new TranslateTransform();
                }
                if (!(border2.RenderTransform is TranslateTransform))
                {
                    border2.RenderTransform = new TranslateTransform();
                }

                TranslateTransform tt1 = (TranslateTransform)border1.RenderTransform;
                TranslateTransform tt2 = (TranslateTransform)border2.RenderTransform;
                
                DoubleAnimation animation1 = new DoubleAnimation
                {
                    To = itemWidth,
                    Duration = TimeSpan.FromSeconds(animationDurationSeconds),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
                };

                DoubleAnimation animation2 = new DoubleAnimation
                {
                    To = -itemWidth,
                    Duration = TimeSpan.FromSeconds(animationDurationSeconds),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
                };
                
                tt1.BeginAnimation(TranslateTransform.XProperty, animation1);
                tt2.BeginAnimation(TranslateTransform.XProperty, animation2);
            });
            
            await Task.Delay((int)(animationDurationSeconds * 1000));

            await Dispatcher.InvokeAsync(() =>
            {
                // Обновляем значения в списке numbers
                int temp = numbers[index1].Value;
                numbers[index1].Value = numbers[index2].Value;
                numbers[index2].Value = temp;
                
                // Сбрасываем трансформации
                if (border1.RenderTransform is TranslateTransform tt1)
                {
                    tt1.BeginAnimation(TranslateTransform.XProperty, null);
                    tt1.X = 0;
                }

                if (border2.RenderTransform is TranslateTransform tt2)
                {
                    tt2.BeginAnimation(TranslateTransform.XProperty, null);
                    tt2.X = 0;
                }
            });
        }
        
        //Анимация для InsertSort
        private async Task AnimateSwapForInsertSort(int index1, int index2)
        {
            const double itemWidth = 77;
            double animationDurationSeconds = 0.5;

            FrameworkElement container1 = null;
            FrameworkElement container2 = null;
            Border border1 = null;
            Border border2 = null;
            
            await Dispatcher.InvokeAsync(() =>
            {
                container1 = ArrayDisplay.ItemContainerGenerator.ContainerFromIndex(index1) as FrameworkElement;
                container2 = ArrayDisplay.ItemContainerGenerator.ContainerFromIndex(index2) as FrameworkElement;

                if (container1 != null && container2 != null)
                {
                    border1 = FindVisualChild<Border>(container1);
                    border2 = FindVisualChild<Border>(container2);
                }
            });

            if (border1 == null || border2 == null) return;

            await Dispatcher.InvokeAsync(() =>
            {
                if (!(border1.RenderTransform is TranslateTransform))
                {
                    border1.RenderTransform = new TranslateTransform();
                }
                if (!(border2.RenderTransform is TranslateTransform))
                {
                    border2.RenderTransform = new TranslateTransform();
                }

                TranslateTransform tt1 = (TranslateTransform)border1.RenderTransform;
                TranslateTransform tt2 = (TranslateTransform)border2.RenderTransform;
                
                DoubleAnimation animation1 = new DoubleAnimation
                {
                    To = itemWidth,
                    Duration = TimeSpan.FromSeconds(animationDurationSeconds),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
                };

                DoubleAnimation animation2 = new DoubleAnimation
                {
                    To = -itemWidth,
                    Duration = TimeSpan.FromSeconds(animationDurationSeconds),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
                };
                
                tt1.BeginAnimation(TranslateTransform.XProperty, animation1);
                tt2.BeginAnimation(TranslateTransform.XProperty, animation2);
            });
            
            await Task.Delay((int)(animationDurationSeconds * 1000));

            await Dispatcher.InvokeAsync(() =>
            {
                // Сбрасываем трансформации
                if (border1.RenderTransform is TranslateTransform tt1)
                {
                    tt1.BeginAnimation(TranslateTransform.XProperty, null);
                    tt1.X = 0;
                }

                if (border2.RenderTransform is TranslateTransform tt2)
                {
                    tt2.BeginAnimation(TranslateTransform.XProperty, null);
                    tt2.X = 0;
                }
            });
        }
        
        // Метод для анимации обмена элементов
        private async void AnimateSwapHandlerForBubbleSort(int index1, int index2)
        {
            await AnimateSwapForBubbleSort(index1, index2);
        }
        
        // Метод для InsertSort (без обновления значений)
        private async void AnimateSwapHandlerForInsertSort(int index1, int index2)
        {
            await AnimateSwapForInsertSort(index1, index2);
        }
        
        //Поиск визуального дочернего элемента
        private static T FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            if (parent == null) return null;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T t)
                {
                    return t;
                }

                var childOfChild = FindVisualChild<T>(child);
                if (childOfChild != null)
                {
                    return childOfChild;
                }
            }
            return null;
        }
        
        //Сортировка стран
        private void StartCountrySorting()
        {
            try
            {
                if (ContinentSelector.SelectedItem is ComboBoxItem selectedContinentItem &&
                    AttributeSelector.SelectedItem is ComboBoxItem selectedAttributeItem)
                {
                    string continent = selectedContinentItem.Content.ToString();
                    string sortAttribute = selectedAttributeItem.Content.ToString();
                    
                    countrySorter = new CountrySorter(countryFilePath);
                    var sortedCountries = countrySorter.FilterAndSort(continent, sortAttribute);
                    countrySorter.SaveToFile(sortedCountries, outputFilePath);
                    
                    var outputBuilder = new System.Text.StringBuilder();
                    outputBuilder.AppendLine($"{continent}\n");

                    foreach (var country in sortedCountries)
                    {
                        outputBuilder.AppendLine(
                            $"{country.Name}, {country.Capital}, Площадь = {country.Area:N0} кв.км, Население = {country.Population:N0} человек"
                        );
                    }
                    
                    OutputTextBox.Text = outputBuilder.ToString();
                }
                else
                {
                    MessageBox.Show("Выберите континент и атрибут сортировки.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
