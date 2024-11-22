using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using WPF1.Logic;

namespace WPF1
{
    public partial class MainWindow : Window
    {
        private ObservableCollection<int> numbers;
        private ISortingAlgorithm sortingAlgorithm;
        private int[] finalized;
        private int delay = 500;

        public MainWindow()
        {
            InitializeComponent();
            numbers = new ObservableCollection<int> { 5, 9, 3, 1, 8, 6, 4, 2, 7 };
            ArrayDisplay.ItemsSource = numbers;
            finalized = new int[numbers.Count];
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

            // Сбрасываем массив завершённых элементов
            finalized = new int[numbers.Count];

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
                sortingAlgorithm.Sort(numbers.ToArray(), delay);
            });
            sortingThread.Start();
        }
        
        private void UpdateArray(int[] updatedArray)
        {
            Dispatcher.Invoke(() =>
            {
                numbers.Clear();
                foreach (var num in updatedArray)
                    numbers.Add(num);
            });
        }
        
        private void ShowComparison(int index1, int index2, string message)
        {
            Dispatcher.Invoke(() =>
            {
                for (int i = 0; i < ArrayDisplay.Items.Count; i++)
                {
                    var container = ArrayDisplay.ItemContainerGenerator.ContainerFromIndex(i) as ContentPresenter;
                    if (container != null)
                    {
                        var border = (Border)ArrayDisplay.ItemTemplate.FindName("border", container);
                        if (border != null)
                        {
                            // Если элемент завершил участие, оставляем его синим
                            if (finalized[i] == 1)
                            {
                                border.Background = System.Windows.Media.Brushes.Blue;
                            }
                            else
                            {
                                // Подсвечиваем только сравниваемые элементы
                                border.Background = (i == index1 || i == index2)
                                    ? System.Windows.Media.Brushes.LightYellow // Красный для сравниваемых
                                    : System.Windows.Media.Brushes.White; // Белый для остальных
                            }
                        }
                    }
                }

                // Добавляем текст в поле вывода
                OutputTextBox.AppendText($"{message}\n");
                OutputTextBox.ScrollToEnd();
            });
        }

        private void ShowFinalizedElements(int[] finalizedArray)
        {
            Dispatcher.Invoke(() =>
            {
                for (int i = 0; i < ArrayDisplay.Items.Count; i++)
                {
                    if (finalizedArray[i] == 1) // Проверяем массив завершённых элементов
                    {
                        var container = ArrayDisplay.ItemContainerGenerator.ContainerFromIndex(i) as ContentPresenter;
                        if (container != null)
                        {
                            var border = (Border)ArrayDisplay.ItemTemplate.FindName("border", container);
                            if (border != null)
                            {
                                border.Background = System.Windows.Media.Brushes.LightGreen; // Подсвечиваем синим
                            }
                        }
                    }
                }
            });
        }

    }
}
