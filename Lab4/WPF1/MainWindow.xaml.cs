using System.Collections.ObjectModel;
using System.Globalization;
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
            if (!IsLoaded) return;
            
            if (initialNumbers != null && initialNumbers.Count > 0)
            {
                numbers = new ObservableCollection<NumberItem>(
                    initialNumbers.Select(num => new NumberItem { Value = num })
                );
                ArrayDisplay.ItemsSource = null;
                ArrayDisplay.ItemsSource = numbers;
            }
            
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

                SortPanel.Visibility = Visibility.Collapsed;
                FilterPanel.Visibility = Visibility.Collapsed;
                MethodSelectorPanel.Visibility = Visibility.Collapsed;
                CustomFilePathPanel.Visibility = Visibility.Collapsed;

                OutputTextBox.Visibility = Visibility.Visible;
            }
            else if (selectedTask == "Фильтрация таблиц")
            {
                ArrayDisplay.Visibility = Visibility.Collapsed;
                StopButton.Visibility = Visibility.Collapsed;
                DelayInputPanel.Visibility = Visibility.Collapsed;
                ArrayInputPanel.Visibility = Visibility.Collapsed;
                CustomFilePathPanel.Visibility = Visibility.Collapsed;
                AlgorithmSelectorPanel.Visibility = Visibility.Collapsed;

                SortPanel.Visibility = Visibility.Visible;
                FilterPanel.Visibility = Visibility.Visible;
                MethodSelectorPanel.Visibility = Visibility.Visible;
                CustomFilePathPanel.Visibility = Visibility.Visible;

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
                SortPanel.Visibility = Visibility.Collapsed;
                FilterPanel.Visibility = Visibility.Collapsed;
                MethodSelectorPanel.Visibility = Visibility.Collapsed;
                CustomFilePathPanel.Visibility = Visibility.Collapsed;

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
        
        //Кнопка "Выбрать файл"
        private void OpenFileButton_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Выберите файл",
                Filter = "Текстовые файлы (*.txt)|*.txt|Все файлы (*.*)|*.*"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                CustomFilePathTextBox.Text = openFileDialog.FileName;
                PopulateSelectors(openFileDialog.FileName);
            }
        }
        
        //Получить путь к файлу
        private string GetFilePath()
        {
            string customPath = CustomFilePathTextBox?.Text;
            return string.IsNullOrWhiteSpace(customPath) ? "Resources/Country.txt" : customPath;
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

            if (selectedTask == "Фильтрация таблиц")
            {
                StartSorting();
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
                    string inputArrayText = ArrayInputTextBox.Text;
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
                    
                    int[] arrayToSort = numbers.Select(n => n.Value).ToArray();

                    sortingAlgorithm = selectedAlgorithm switch
                    {
                        "BubbleSort" => new BubbleSort(),
                        "QuickSort" => new QuickSort(),
                        "InsertSort" => new InsertSort(),
                        "HeapSort" => new HeapSort(),
                        _ => throw new NotImplementedException("Алгоритм не реализован.")
                    };
                    
                    sortingAlgorithm.OnStepCompleted += UpdateArray;
                    sortingAlgorithm.SortingCompleted += OnSortingCompleted;
                    sortingAlgorithm.OnComparison += ShowComparison;
                    sortingAlgorithm.OnExplanation += ShowExplanation;
                    
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
                int temp = numbers[index1].Value;
                numbers[index1].Value = numbers[index2].Value;
                numbers[index2].Value = temp;
                
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
        
        private void PopulateSelectors(string filePath)
        {
            try
            {
                var lines = File.ReadAllLines(filePath);
                if (lines.Length < 2)
                {
                    MessageBox.Show("Файл должен содержать заголовок и хотя бы одну строку данных.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var headers = lines[0].Split(',');

                if (headers.Length < 2)
                {
                    MessageBox.Show("Файл должен содержать как минимум два столбца.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                
                FilterAttributeSelector.Items.Clear();
                var filterValues = lines.Skip(1)
                    .Select(line => line.Split(',')[1].Trim())
                    .Distinct()
                    .ToList();

                foreach (var value in filterValues)
                {
                    FilterAttributeSelector.Items.Add(new ComboBoxItem { Content = value });
                }
                
                // Заполнение атрибута сортировки (первый столбец и остальные, кроме фильтрующего)
                SortAttributeSelector.Items.Clear();
                foreach (var header in headers.Where((h, index) => index != 1))
                {
                    SortAttributeSelector.Items.Add(new ComboBoxItem { Content = header });
                }
                
                if (FilterAttributeSelector.Items.Count > 0)
                    FilterAttributeSelector.SelectedIndex = 0;

                if (SortAttributeSelector.Items.Count > 0)
                    SortAttributeSelector.SelectedIndex = 0;

                MessageBox.Show("Списки успешно обновлены на основе данных из файла.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка обновления меню: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private string GetFilterColumn(string filePath)
        {
            var lines = File.ReadLines(filePath);
            var headerLine = lines.FirstOrDefault();
            if (headerLine != null)
            {
                var headers = headerLine.Split(',');
                if (headers.Length >= 2)
                {
                    return headers[1];
                }
            }
            throw new Exception("Невозможно определить фильтрующий столбец.");
        }
        
        private void DisplaySortedTable(Table table, string filterValue, string sortKey, string explanation)
        {
            // Строим строку вывода
            StringBuilder sb = new StringBuilder();

            // Выводим значение фильтра вверху
            sb.AppendLine(filterValue);
            sb.AppendLine();

            // Определяем числовые столбцы
            var numericalColumns = GetNumericalColumns(table);

            // Получаем названия столбцов
            var headers = table.Columns;

            // Для каждой строки формируем вывод
            foreach (var row in table.Rows)
            {
                List<string> rowValues = new List<string>();

                for (int i = 0; i < headers.Count; i++)
                {
                    var column = headers[i];

                    // Пропускаем фильтрующий столбец (второй столбец)
                    if (i == 1)
                        continue;

                    var value = row[column];

                    if (numericalColumns.Contains(column))
                    {
                        // Добавляем параметр и значение
                        rowValues.Add($"{column}: {FormatNumericValue(value, column)}");
                    }
                    else
                    {
                        // Добавляем значение напрямую
                        rowValues.Add(value);
                    }
                }

                // Соединяем значения через запятую
                sb.AppendLine(string.Join(", ", rowValues));
            }

            // Добавляем объяснение
            sb.AppendLine(explanation);

            // Выводим результат в интерфейсе (например, в TextBox)
            OutputTextBox.Text = sb.ToString();
        }
        
        private HashSet<string> GetNumericalColumns(Table table)
        {
            var numericalColumns = new HashSet<string>();

            if (table.Rows.Count == 0)
                return numericalColumns;

            var culture = CultureInfo.GetCultureInfo("ru-RU");

            foreach (var column in table.Columns)
            {
                // Пропускаем фильтрующий столбец (второй столбец)
                if (table.Columns.IndexOf(column) == 1)
                    continue;

                bool isNumeric = true;

                foreach (var row in table.Rows.Take(5)) // Проверяем первые 5 строк
                {
                    var value = row[column];
                    if (!double.TryParse(value, NumberStyles.Any, culture, out _))
                    {
                        isNumeric = false;
                        break;
                    }
                }

                if (isNumeric)
                {
                    numericalColumns.Add(column);
                }
            }

            return numericalColumns;
        }
        
        private string FormatNumericValue(string value, string column)
        {
            var culture = CultureInfo.GetCultureInfo("ru-RU");

            if (double.TryParse(value, NumberStyles.Any, culture, out double numericValue))
            {
                string formattedValue = numericValue.ToString("N0", culture); // Убираем дробную часть

                // Добавляем единицы измерения в зависимости от столбца
                if (column.Contains("Площадь"))
                {
                    return $"{formattedValue} кв.км";
                }
                else if (column.Contains("Численность населения"))
                {
                    return $"{formattedValue} человек";
                }
                else if (column.Contains("Молекулярная масса"))
                {
                    return $"{formattedValue} г/моль";
                }
                else
                {
                    return formattedValue;
                }
            }
            else
            {
                return value;
            }
        }
        
        private void StartSorting()
        {
            try
            {
                string filePath = GetFilePath();

                string filterValue = (FilterAttributeSelector.SelectedItem as ComboBoxItem)?.Content.ToString();
                string sortKey = (SortAttributeSelector.SelectedItem as ComboBoxItem)?.Content.ToString();
                string sortingMethod = (MethodSelector.SelectedItem as ComboBoxItem)?.Content.ToString();

                if (string.IsNullOrEmpty(filterValue) || string.IsNullOrEmpty(sortKey) || string.IsNullOrEmpty(sortingMethod))
                {
                    MessageBox.Show("Пожалуйста, выберите параметры для фильтрации, сортировки и метод сортировки.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Фильтрующий столбец всегда второй
                string filterColumn = GetFilterColumn(filePath);
                if (string.IsNullOrEmpty(filterColumn))
                {
                    MessageBox.Show("Не удалось определить фильтрующий столбец.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                string outputFilePath = "sorted_output.txt";

                // Передаем выбранный метод сортировки
                ExternalSorter.MergeSort(filePath, filterColumn, filterValue, sortKey, outputFilePath, sortingMethod);

                // Генерируем объяснение сортировки
                try
                {
                    string explanation = SortingExplanation.GenerateExplanation(sortingMethod, "Таблица данных", filterColumn, filterValue, sortKey);

                    // Отображаем результат
                    var sortedTable = Table.LoadFromFile(outputFilePath);
                    DisplaySortedTable(sortedTable, filterValue, sortKey, explanation);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка в генерации объяснения: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

    }
}
