using System.Collections.ObjectModel;
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
        private ISortingAlgorithm sortingAlgorithm;
        private int delay = 1000;

        public MainWindow()
        {
            InitializeComponent();
            numbers = new ObservableCollection<NumberItem>
            {
                new NumberItem { Value = 5 },
                new NumberItem { Value = 9 },
                new NumberItem { Value = 3 },
                new NumberItem { Value = 1 },
                new NumberItem { Value = 8 },
                new NumberItem { Value = 6 },
                new NumberItem { Value = 4 },
                new NumberItem { Value = 2 },
                new NumberItem { Value = 7 },
            };
            ArrayDisplay.ItemsSource = numbers;
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            string selectedAlgorithm = TaskSelector.Text;
            if (string.IsNullOrEmpty(selectedAlgorithm))
            {
                MessageBox.Show("Пожалуйста, выберите алгоритм сортировки.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(DelayTextBox.Text, out delay) || delay < 0)
            {
                MessageBox.Show("Введите корректное значение задержки (положительное число).", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            foreach (var item in numbers)
            {
                item.IsFinalized = false;
                item.IsComparing = false;
            }

            sortingAlgorithm = selectedAlgorithm switch
            {
                "BubbleSort" => new BubbleSort(),
                "QuickSort" => new QuickSort(),
                _ => throw new NotImplementedException("Алгоритм не реализован.")
            };

            sortingAlgorithm.OnStepCompleted += UpdateArray;
            sortingAlgorithm.OnComparison += ShowComparison;
            sortingAlgorithm.OnFinalizedElements += ShowFinalizedElements;

            Thread sortingThread = new Thread(() =>
            {
                int[] arrayToSort = numbers.Select(n => n.Value).ToArray();
                sortingAlgorithm.Sort(arrayToSort, delay);
            });
            sortingThread.Start();
        }

        private void UpdateArray(int[] updatedArray)
        {
            Dispatcher.Invoke(() =>
            {
                for (int i = 0; i < numbers.Count; i++)
                {
                    numbers[i].Value = updatedArray[i];
                }
            });
        }
        
        private async void ShowComparison(int index1, int index2, string message)
        {
            // Проверяем индексы на корректность
            if (index1 < 0 || index1 >= numbers.Count || index2 < 0 || index2 >= numbers.Count)
            {
                // Если индексы некорректные, просто выходим из метода
                return;
            }

            // Подсветка текущих элементов для сравнения
            await Dispatcher.InvokeAsync(() =>
            {
                // Сбрасываем подсветку всех элементов
                foreach (var item in numbers)
                {
                    item.IsComparing = false;
                }

                // Убедимся, что индексы находятся в пределах списка
                if (index1 >= 0 && index1 < numbers.Count)
                    numbers[index1].IsComparing = true;
                if (index2 >= 0 && index2 < numbers.Count)
                    numbers[index2].IsComparing = true;

                // Выводим сообщение о сравнении
                OutputTextBox.AppendText($"{message}\n");
                OutputTextBox.ScrollToEnd();
            });

            // Задержка для визуализации подсветки
            await Task.Delay(300);

            // Проверяем, нужно ли менять местами
            if (numbers[index1].Value > numbers[index2].Value)
            {
                // Выводим сообщение о смене мест
                await Dispatcher.InvokeAsync(() =>
                {
                    OutputTextBox.AppendText($"{numbers[index1].Value} > {numbers[index2].Value}. Меняем местами\n");
                });

                // Выполняем анимацию перемещения
                await AnimateSwap(index1, index2);
            }
            else
            {
                // Если элементы остаются на местах
                await Dispatcher.InvokeAsync(() =>
                {
                    OutputTextBox.AppendText($"{numbers[index1].Value} < {numbers[index2].Value}. Остаются на местах\n");
                });
            }

            // Добавляем пробел только после завершения блока обработки
            await Dispatcher.InvokeAsync(() =>
            {
                OutputTextBox.AppendText("\n");
            });

            // Сбрасываем подсветку текущих элементов перед переходом к следующей паре
            await Dispatcher.InvokeAsync(() =>
            {
                numbers[index1].IsComparing = false;
                numbers[index2].IsComparing = false;
            });

            // Задержка перед следующим шагом алгоритма
            await Task.Delay(delay);
        }

        private async Task AnimateSwap(int index1, int index2)
        {
            const double itemWidth = 74;
            double duration = 0.5;

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
                border1.RenderTransform = new TranslateTransform();
                border2.RenderTransform = new TranslateTransform();

                DoubleAnimation animation1 = new DoubleAnimation
                {
                    To = itemWidth,
                    Duration = TimeSpan.FromSeconds(duration),
                    EasingFunction = new QuadraticEase()
                };

                DoubleAnimation animation2 = new DoubleAnimation
                {
                    To = -itemWidth,
                    Duration = TimeSpan.FromSeconds(duration),
                    EasingFunction = new QuadraticEase()
                };

                border1.RenderTransform.BeginAnimation(TranslateTransform.XProperty, animation1);
                border2.RenderTransform.BeginAnimation(TranslateTransform.XProperty, animation2);
            });

            await Task.Delay((int)(duration * 1000));

            await Dispatcher.InvokeAsync(() =>
            {
                int temp = numbers[index1].Value;
                numbers[index1].Value = numbers[index2].Value;
                numbers[index2].Value = temp;

                border1.RenderTransform = new TranslateTransform();
                border2.RenderTransform = new TranslateTransform();
            });
        }

        private static T FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
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

