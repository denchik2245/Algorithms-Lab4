using System.Collections.ObjectModel;
using System.IO;
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

        public MainWindow()
        {
            InitializeComponent();
            LoadArrayFromFile("Resources/ArrayForVisual.txt");
            DataContext = this;
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
        
        //Кнопка "Начать"
        private void StartButton_Click(object sender, RoutedEventArgs e)
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
