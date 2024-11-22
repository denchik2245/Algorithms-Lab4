﻿using System.Collections.ObjectModel;
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
        private int delay = 1000;
        private bool isPaused = false;
        private bool isSortingStarted = false;
        private Thread sortingThread;
        private string countryFilePath = "Resources/Country.txt";
        private string outputFilePath = "Resources/SortedCountries.txt";
        private CountrySorter countrySorter;

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
            if (!IsLoaded) return; // Проверяем, загружено ли окно

            string selectedTask = ((ComboBoxItem)TaskSelector.SelectedItem)?.Content.ToString();

            if (TextSortingTasks.Contains(selectedTask))
            {
                // Если выбрана задача сортировки текста

                // Скрываем ненужные панели
                ArrayDisplay.Visibility = Visibility.Collapsed;
                StopButton.Visibility = Visibility.Collapsed;
                DelayInputPanel.Visibility = Visibility.Collapsed;
                ArrayInputPanel.Visibility = Visibility.Collapsed;

                // Показываем необходимые панели
                CustomFilePathPanel.Visibility = Visibility.Visible;
                AlgorithmSelectorPanel.Visibility = Visibility.Visible;

                // Скрываем другие специфичные панели, если есть
                ContinentPanel.Visibility = Visibility.Collapsed;
                AttributePanel.Visibility = Visibility.Collapsed;

                // Отображаем OutputTextBox для вывода результатов
                OutputTextBox.Visibility = Visibility.Visible;
            }
            else if (selectedTask == "Сведения о государствах")
            {
                // Если выбрана задача "Сведения о государствах"

                // Скрываем панели сортировки текста
                ArrayDisplay.Visibility = Visibility.Collapsed;
                StopButton.Visibility = Visibility.Collapsed;
                DelayInputPanel.Visibility = Visibility.Collapsed;
                ArrayInputPanel.Visibility = Visibility.Collapsed;
                CustomFilePathPanel.Visibility = Visibility.Collapsed;
                AlgorithmSelectorPanel.Visibility = Visibility.Collapsed;

                // Показываем специфичные для этой задачи панели
                ContinentPanel.Visibility = Visibility.Visible;
                AttributePanel.Visibility = Visibility.Visible;

                // Отображаем OutputTextBox для вывода результатов
                OutputTextBox.Visibility = Visibility.Visible;
                OutputTextBox.Height = 700;
            }
            else
            {
                // Для других задач (например, сортировка чисел)

                // Показываем стандартные панели
                ArrayInputPanel.Visibility = Visibility.Visible;
                DelayInputPanel.Visibility = Visibility.Visible;
                ArrayDisplay.Visibility = Visibility.Visible;
                StopButton.Visibility = Visibility.Visible;

                // Скрываем специфичные для других задач панели
                CustomFilePathPanel.Visibility = Visibility.Collapsed;
                AlgorithmSelectorPanel.Visibility = Visibility.Collapsed;
                ContinentPanel.Visibility = Visibility.Collapsed;
                AttributePanel.Visibility = Visibility.Collapsed;

                // Отображаем OutputTextBox для вывода результатов
                OutputTextBox.Visibility = Visibility.Visible;
                OutputTextBox.Height = 600;
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

                ArrayDisplay.ItemsSource = numbers;
                
                initialNumbers = numbers.Select(n => n.Value).ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка чтения файла: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void StartTextSorting()
        {
            string selectedTask = ((ComboBoxItem)TaskSelector.SelectedItem)?.Content.ToString();

            // Определение пути к файлу
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

            // Получение выбранного алгоритма
            string selectedAlgorithm = BaseAlgorithmRadioButton.IsChecked == true ? "Базовый или усовершенствованный" :
                                       RadixSortRadioButton.IsChecked == true ? "Radix сортировка" : "QuickSort";

            try
            {
                WordSorter sorter = new WordSorter();
                var sortedWordsWithCounts = sorter.SortWords(filePath, selectedAlgorithm);

                // Формирование строки для отображения
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

        // Метод для правильного склонения слова "раз"
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

                    sortingAlgorithm = selectedAlgorithm switch
                    {
                        "BubbleSort" => new BubbleSort(),
                        "QuickSort" => new QuickSort(),
                        _ => throw new NotImplementedException("Алгоритм не реализован.")
                    };
                    
                    sortingAlgorithm.OnStepCompleted += UpdateArray;
                    sortingAlgorithm.OnComparison += ShowComparison;
                    sortingAlgorithm.OnFinalizedElements += ShowFinalizedElements;
                    sortingAlgorithm.SortingCompleted += OnSortingCompleted;

                    isSortingStarted = true;
                    
                    sortingThread = new Thread(() =>
                    {
                        try
                        {
                            int[] arrayToSort = numbers.Select(n => n.Value).ToArray();
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
        
        private void UpdateArray(int[] array)
        {
            Dispatcher.Invoke(() =>
            {
                for (int i = 0; i < array.Length; i++)
                {
                    numbers[i].Value = array[i];
                }
            });
        }
        
        private void StartCountrySorting()
        {
            try
            {
                if (ContinentSelector.SelectedItem is ComboBoxItem selectedContinentItem &&
                    AttributeSelector.SelectedItem is ComboBoxItem selectedAttributeItem)
                {
                    string continent = selectedContinentItem.Content.ToString();
                    string sortAttribute = selectedAttributeItem.Content.ToString();

                    // Загружаем данные и фильтруем/сортируем
                    countrySorter = new CountrySorter(countryFilePath);
                    var sortedCountries = countrySorter.FilterAndSort(continent, sortAttribute);

                    // Сохраняем в файл
                    countrySorter.SaveToFile(sortedCountries, outputFilePath);

                    // Форматированный вывод
                    var outputBuilder = new System.Text.StringBuilder();
                    outputBuilder.AppendLine($"{continent}\n");

                    foreach (var country in sortedCountries)
                    {
                        outputBuilder.AppendLine(
                            $"{country.Name}, {country.Capital}, Площадь = {country.Area:N0} кв.км, Население = {country.Population:N0} человек"
                        );
                    }

                    // Выводим результат в текстовое поле
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
        
        private async void ShowComparison(int index1, int index2, string message)
        {
            if (index1 < 0 || index1 >= numbers.Count || index2 < 0 || index2 >= numbers.Count)
            {
                return;
            }
            
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
                
                OutputTextBox.AppendText($"{message}\n");
                OutputTextBox.ScrollToEnd();
            });
            
            await Task.Delay(300);
            
            if (numbers[index1].Value > numbers[index2].Value)
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    OutputTextBox.AppendText($"{numbers[index1].Value} > {numbers[index2].Value}. Меняем местами\n");
                });
                
                await AnimateSwap(index1, index2);
            }
            else
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    OutputTextBox.AppendText($"{numbers[index1].Value} < {numbers[index2].Value}. Остаются на местах\n");
                });
            }
            
            await Dispatcher.InvokeAsync(() =>
            {
                OutputTextBox.AppendText("\n");
            });
            
            await Dispatcher.InvokeAsync(() =>
            {
                numbers[index1].IsComparing = false;
                numbers[index2].IsComparing = false;
            });
            
            await Task.Delay(delay);
        }

        //Анимация обмена местами двух элементов (BubbleSort)
        private async Task AnimateSwap(int index1, int index2)
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
    }
}
