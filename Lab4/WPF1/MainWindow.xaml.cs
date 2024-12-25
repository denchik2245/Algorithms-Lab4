using System.Collections.ObjectModel;
using System.Diagnostics;
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
        private bool isAnimating = false;

        public MainWindow()
        {
            InitializeComponent();
            LoadArrayFromFile("Resources/ArrayForVisual.txt");
            DataContext = this;
        }
        
        //Обработчик пунктов меню
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

            if (selectedTask == "Сортировка текста")
            {
                OutputTextBox.Height = 700;

                ArrayDisplay.Visibility = Visibility.Collapsed;
                StopButton.Visibility = Visibility.Collapsed;
                DelayInputPanel.Visibility = Visibility.Collapsed;
                ArrayInputPanel.Visibility = Visibility.Collapsed;
                CustomFilePathPanel.Visibility = Visibility.Visible;
                AlgorithmSelectorPanel.Visibility = Visibility.Visible;
                AllWordsTextBox.Visibility = Visibility.Visible;
                UniqueWordsTextBox.Visibility = Visibility.Visible;
                CustomFilePathPanel.Visibility = Visibility.Visible;

                SortPanel.Visibility = Visibility.Collapsed;
                FilterValueInput.Visibility = Visibility.Collapsed;
                FilterPanel.Visibility = Visibility.Collapsed;
                MethodSelectorPanel.Visibility = Visibility.Collapsed;
                Table2.Visibility = Visibility.Collapsed;

                OutputTextBox.Visibility = Visibility.Collapsed;
            }
            else if (selectedTask == "Фильтрация таблиц")
            {
                ArrayDisplay.Visibility = Visibility.Collapsed;
                StopButton.Visibility = Visibility.Collapsed;
                DelayInputPanel.Visibility = Visibility.Collapsed;
                ArrayInputPanel.Visibility = Visibility.Collapsed;
                CustomFilePathPanel.Visibility = Visibility.Collapsed;
                AlgorithmSelectorPanel.Visibility = Visibility.Collapsed;
                AllWordsTextBox.Visibility = Visibility.Collapsed;
                UniqueWordsTextBox.Visibility = Visibility.Collapsed;
                FilterValueInput.Visibility = Visibility.Visible;

                SortPanel.Visibility = Visibility.Visible;
                FilterPanel.Visibility = Visibility.Visible;
                MethodSelectorPanel.Visibility = Visibility.Visible;
                CustomFilePathPanel.Visibility = Visibility.Visible;
                Table2.Visibility = Visibility.Collapsed;

                OutputTextBox.Visibility = Visibility.Visible;
                OutputTextBox.Height = 700;
            }
            else if (selectedTask == "Таблица сравнения")
            {
                ArrayDisplay.Visibility = Visibility.Collapsed;
                StopButton.Visibility = Visibility.Collapsed;
                DelayInputPanel.Visibility = Visibility.Collapsed;
                ArrayInputPanel.Visibility = Visibility.Collapsed;
                CustomFilePathPanel.Visibility = Visibility.Collapsed;
                AlgorithmSelectorPanel.Visibility = Visibility.Collapsed;
                AllWordsTextBox.Visibility = Visibility.Collapsed;
                UniqueWordsTextBox.Visibility = Visibility.Collapsed;
                SortPanel.Visibility = Visibility.Collapsed;
                FilterValueInput.Visibility = Visibility.Collapsed;
                FilterPanel.Visibility = Visibility.Collapsed;
                MethodSelectorPanel.Visibility = Visibility.Collapsed;
                OutputTextBox.Visibility = Visibility.Collapsed;
                Table2.Visibility = Visibility.Visible;
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
                AllWordsTextBox.Visibility = Visibility.Collapsed;
                UniqueWordsTextBox.Visibility = Visibility.Collapsed;
                Table2.Visibility = Visibility.Collapsed;

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
        
        //Получить путь к файлу
        private string GetFilePath()
        {
            string customPath = CustomFilePathTextBox?.Text;
            return string.IsNullOrWhiteSpace(customPath) ? "Resources/Country.txt" : customPath;
        }
        
        //Кнопка "Начать"
        private async void StartButton_Click(object sender, RoutedEventArgs e)
        {
            string selectedTask = ((ComboBoxItem)TaskSelector.SelectedItem)?.Content.ToString();

            if (selectedTask == "Фильтрация таблиц")
            {
                StartSorting();
                OutputTextBox.Clear();
            }
            else if (selectedTask.StartsWith("Сортировка текста"))
            {
                StartTextSorting();
            }
            else if (selectedTask == "Таблица сравнения")
            {
                StartSortingExperiments();
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

                    isSortingStarted = true;

                    if (selectedAlgorithm == "HeapSort")
                    {
                        IAsyncSortingAlgorithm asyncSortingAlgorithm = new HeapSort(
                            ShowComparisonAsync,
                            ShowSwapAsync,
                            ShowExplanationAsync,
                            ShowFinalizedElements);

                        asyncSortingAlgorithm.SortingCompleted += OnSortingCompleted;

                        try
                        {
                            await asyncSortingAlgorithm.SortAsync(arrayToSort, delay);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Ошибка выполнения сортировки: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                    else if (selectedAlgorithm == "QuickSort")
                    {
                        sortingAlgorithm = new QuickSort();

                        sortingAlgorithm.OnStepCompleted += UpdateArray;
                        sortingAlgorithm.SortingCompleted += OnSortingCompleted;
                        sortingAlgorithm.OnComparison += ShowComparison;
                        sortingAlgorithm.OnSwap += AnimateSwapHandlerForQuickSort;
                        sortingAlgorithm.OnExplanation += ShowExplanation;

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
                        sortingAlgorithm = selectedAlgorithm switch
                        {
                            "BubbleSort" => new BubbleSort(),
                            "QuickSort" => new QuickSort(),
                            "InsertSort" => new InsertSort(),
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
                        else if (sortingAlgorithm is InsertSort)
                        {
                            sortingAlgorithm.OnSwap += AnimateSwapHandlerForInsertSort;
                        }
                        else if (sortingAlgorithm is QuickSort)
                        {
                            sortingAlgorithm.OnComparison -= ShowComparison;
                        }

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
        
        //Методы для первого задания
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
            Dispatcher.Invoke(() =>
            {
                for (int i = 0; i < newArray.Length; i++)
                {
                    if (i < numbers.Count)
                    {
                        // Обновляем значение элемента
                        numbers[i].Value = newArray[i];
                    }
                    else
                    {
                        numbers.Add(new NumberItem { Value = newArray[i] });
                    }
                }

                // Удаляем лишние элементы, если новые данные короче
                while (numbers.Count > newArray.Length)
                {
                    numbers.RemoveAt(numbers.Count - 1);
                }
            });
        }
        
        //Показать объяснение
        private async void ShowExplanation(string message)
        {
            await Dispatcher.InvokeAsync(() =>
            {
                OutputTextBox.AppendText($"{message}\n");
                OutputTextBox.ScrollToEnd();
            });

            await Task.Delay(delay);
        }
        
        //Подсветка элементов
        private async void ShowComparison(int index1, int index2, int index3)
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
                if (index3 >= 0 && index3 < numbers.Count)
                    numbers[index3].IsComparing = true;
            });

            // Задержка для отображения подсветки
            await Task.Delay(delay);

            await Dispatcher.InvokeAsync(() =>
            {
                if (index1 >= 0 && index1 < numbers.Count)
                    numbers[index1].IsComparing = false;
                if (index2 >= 0 && index2 < numbers.Count)
                    numbers[index2].IsComparing = false;
                if (index3 >= 0 && index3 < numbers.Count)
                    numbers[index3].IsComparing = false;
            });
        }
        
        //Элементы которые закончили свой путь
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
        
        private async Task ShowComparisonAsync(int index1, int index2, int index3)
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
                if (index3 >= 0 && index3 < numbers.Count)
                    numbers[index3].IsComparing = true;
            });
        }

        private async Task ShowExplanationAsync(string explanation)
        {
            await Dispatcher.InvokeAsync(() =>
            {
                // Обновляем UI с новым объяснением
                OutputTextBox.Text += explanation + "\n";
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
        
        private async Task AnimateSwapForHeapSort(int index1, int index2)
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

            // Рассчитываем расстояние между элементами
            double distance = (index2 - index1) * itemWidth;

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
                    To = distance,
                    Duration = TimeSpan.FromSeconds(animationDurationSeconds),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
                };

                DoubleAnimation animation2 = new DoubleAnimation
                {
                    To = -distance,
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

        private async Task AnimateSwapForQuickSort(int index1, int index2)
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

            // Рассчитываем расстояние между элементами
            double distance = (index2 - index1) * itemWidth;

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
                    To = distance,
                    Duration = TimeSpan.FromSeconds(animationDurationSeconds),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
                };

                DoubleAnimation animation2 = new DoubleAnimation
                {
                    To = -distance,
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
        
        private async Task ShowSwapAsync(int index1, int index2)
        {
            await AnimateSwapForHeapSort(index1, index2);
        }
        
        private async void AnimateSwapHandlerForQuickSort(int index1, int index2)
        {
            await AnimateSwapForQuickSort(index1, index2);
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
        
        //Начать сортировку
        private void StartSorting()
        {
            try
            {
                string inputFilePath = GetFilePath();
                string filterColumn = (FilterAttributeSelector.SelectedItem as ComboBoxItem)?.Content.ToString();
                string filterValue = (FilterValueInput2.SelectedItem as ComboBoxItem)?.Content.ToString();
                string sortKey = (SortAttributeSelector.SelectedItem as ComboBoxItem)?.Content.ToString();
                string sortMethod = (MethodSelector.SelectedItem as ComboBoxItem)?.Content.ToString();

                if (string.IsNullOrEmpty(filterColumn) || string.IsNullOrEmpty(filterValue) || string.IsNullOrEmpty(sortKey) || string.IsNullOrEmpty(sortMethod))
                {
                    MessageBox.Show("Пожалуйста, выберите столбец для фильтрации, значение фильтрации, столбец для сортировки и метод сортировки.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Чтение заголовков столбцов из файла
                string headerLine;
                List<string> headers;
                try
                {
                    var tempFileHandler = new FileHandler(inputFilePath, null);
                    var data = tempFileHandler.ReadFromFile();
                    if (data == null || data.Count == 0)
                    {
                        MessageBox.Show("Файл пустой или не удалось прочитать данные.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                    headerLine = data[0];
                    headers = headerLine.Split(',').Select(h => h.Trim()).ToList();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при чтении файла: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Находим индексы выбранных столбцов
                int filterKeyIndex = headers.FindIndex(h => string.Equals(h, filterColumn, StringComparison.OrdinalIgnoreCase));
                int secondaryKeyIndex = headers.FindIndex(h => string.Equals(h, sortKey, StringComparison.OrdinalIgnoreCase));

                if (filterKeyIndex == -1 || secondaryKeyIndex == -1)
                {
                    MessageBox.Show("Не удалось найти выбранные столбцы в файле.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Создаем экземпляры FileHandler и ExternalSorter
                var fileHandler = new FileHandler(inputFilePath, null);
                var externalSorter = new ExternalSorter(fileHandler);

                // Создаем объект для отслеживания прогресса
                IProgress<string> progress = new Progress<string>(message =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        OutputTextBox.AppendText(message + Environment.NewLine);
                    });
                });

                // Запускаем сортировку
                externalSorter.Sort(sortMethod, filterKeyIndex, filterValue, secondaryKeyIndex, progress);

                try
                {
                    // Читаем отсортированные данные из файла и отображаем их
                    string sortedResultFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SortedResult.txt");
                    var sortedData = File.ReadAllLines(sortedResultFilePath).ToList();

                    // Вызываем метод для отображения отсортированных данных
                    DisplaySortedData(sortedData);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при отображении отсортированных данных: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        //Отображение отфильтрованных и отсортированных данных таблицы
        private void DisplaySortedData(List<string> sortedData)
        {
            if (sortedData == null || sortedData.Count == 0)
            {
                MessageBox.Show("Нет данных для отображения.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            
            StringBuilder sb = new StringBuilder();
            
            sb.AppendLine("Отсортированные данные:");
            sb.AppendLine();

            foreach (var line in sortedData)
            {
                sb.AppendLine(line);
            }
        }
        
        //Получение пунктов списка взависимости от файла
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

                var headers = lines[0].Split(',').Select(h => h.Trim()).ToList();

                if (headers.Count < 2)
                {
                    MessageBox.Show("Файл должен содержать как минимум два столбца.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                
                FilterAttributeSelector.Items.Clear();
                foreach (var header in headers)
                {
                    FilterAttributeSelector.Items.Add(new ComboBoxItem { Content = header });
                }

                SortAttributeSelector.Items.Clear();
                foreach (var header in headers)
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
        
        private void FilterAttributeSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedColumn = (FilterAttributeSelector.SelectedItem as ComboBoxItem)?.Content.ToString();
            if (string.IsNullOrEmpty(selectedColumn))
                return;

            try
            {
                var lines = File.ReadAllLines(GetFilePath());
                var headers = lines[0].Split(',').Select(h => h.Trim()).ToList();
                int columnIndex = headers.FindIndex(h => string.Equals(h, selectedColumn, StringComparison.OrdinalIgnoreCase));

                if (columnIndex == -1)
                    return;

                var filterValues = lines.Skip(1)
                    .Select(line => line.Split(',')[columnIndex].Trim())
                    .Distinct()
                    .ToList();

                FilterValueInput2.Items.Clear();
                foreach (var value in filterValues)
                {
                    FilterValueInput2.Items.Add(new ComboBoxItem { Content = value });
                }

                if (FilterValueInput2.Items.Count > 0)
                    FilterValueInput2.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при обновлении значений фильтрации: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        //Сортировка текста
        private void StartTextSorting()
        {
            string selectedTask = ((ComboBoxItem)TaskSelector.SelectedItem)?.Content.ToString();

            string defaultFilePath = selectedTask switch
            {
                "Сортировка текста" => "Resources/Text_100.txt",
                _ => throw new NotImplementedException("Выбранная задача сортировки текста не поддерживается.")
            };

            string customFilePath = CustomFilePathTextBox.Text.Trim();
            string filePath = string.IsNullOrEmpty(customFilePath) ? defaultFilePath : customFilePath;

            if (!File.Exists(filePath))
            {
                MessageBox.Show($"Файл не найден: {filePath}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            
            IWordSorter sorterAlgorithm = BaseAlgorithmRadioButton.IsChecked == true
                ? new QuickSortSorter()
                : RadixSortRadioButton.IsChecked == true
                    ? new RadixSortSorter()
                    : throw new NotImplementedException("Выбранный алгоритм сортировки не поддерживается.");

            try
            {
                WordSorter wordSorter = new WordSorter(sorterAlgorithm);
                var sortedWordsWithCounts = wordSorter.SortWords(filePath);
                
                AllWordsTextBox.Text = string.Join(Environment.NewLine, sortedWordsWithCounts.SelectMany(wc => Enumerable.Repeat(wc.Word, wc.Count)));
                UniqueWordsTextBox.Text = string.Join(Environment.NewLine, sortedWordsWithCounts
                    .Select(wc => $"{wc.Word} (Встречалось {wc.Count} {GetRussianPlural(wc.Count)})"));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сортировке: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Метод для корректного склонения слова "раз"
        private string GetRussianPlural(int count)
        {
            if (count % 10 == 1 && count % 100 != 11)
                return "раз";
            else if (count % 10 >= 2 && count % 10 <= 4 && (count % 100 < 10 || count % 100 >= 20))
                return "раза";
            else
                return "раз";
        }

        private void StartSortingExperiments()
        {
            var fileSizes = new List<int> { 100, 500, 1000, 2000, 5000, 10000, 20000 };
            var results = new List<(int WordCount, double QuickSortTime, double RadixSortTime)>();

            foreach (var size in fileSizes)
            {
                string filePath = Path.Combine("Resources", $"Text_{size}.txt");

                if (!File.Exists(filePath))
                {
                    MessageBox.Show($"Файл не найден: {filePath}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    continue;
                }

                try
                {
                    var words = new WordSorter(new QuickSortSorter()).ReadWordsFromFile(filePath);

                    double quickSortTotalTime = 0;
                    double radixSortTotalTime = 0;
                    const int iterations = 50;

                    for (int i = 0; i < iterations; i++)
                    {
                        // Измерение времени для RadixSort
                        var radixSortSorter = new RadixSortSorter();
                        var stopwatchRadix = Stopwatch.StartNew();
                        var radixSorted = radixSortSorter.Sort(new List<string>(words));
                        stopwatchRadix.Stop();
                        radixSortTotalTime += stopwatchRadix.Elapsed.TotalSeconds;

                        // Измерение времени для QuickSort
                        var quickSortSorter = new QuickSortSorter();
                        var stopwatchQuick = Stopwatch.StartNew();
                        var quickSorted = quickSortSorter.Sort(new List<string>(words));
                        stopwatchQuick.Stop();
                        quickSortTotalTime += stopwatchQuick.Elapsed.TotalSeconds;
                    }

                    // Рассчитываем среднее время
                    double quickSortAvgTime = quickSortTotalTime / iterations;
                    double radixSortAvgTime = radixSortTotalTime / iterations;

                    results.Add((size, quickSortAvgTime, radixSortAvgTime));
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при обработке файла {filePath}: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            // Создание таблицы с результатами
            var tableBuilder = new StringBuilder();
            tableBuilder.AppendLine(string.Format("{0,-20} {1,-25} {2,-25}", "Количество слов", "QuickSort (сек)", "RadixSort (сек)"));
            tableBuilder.AppendLine(new string('-', 70));

            foreach (var result in results)
            {
                tableBuilder.AppendLine(string.Format("{0,-20} {1,-25:F5} {2,-25:F5}", result.WordCount, result.QuickSortTime, result.RadixSortTime));
            }

            Table2.Text = tableBuilder.ToString();
            Table2.Visibility = Visibility.Visible;
        }
    }
}