using System.IO;

namespace WPF1.Logic;

public class FileHandler
{
    private readonly string _inputFilePath;
    private readonly string _outputFilePath;

    public FileHandler(string inputFilePath, string outputFilePath)
    {
        _inputFilePath = inputFilePath;
        _outputFilePath = outputFilePath;
    }

    public List<string> ReadFromFile()
    {
        try
        {
            if (!File.Exists(_inputFilePath))
                throw new FileNotFoundException($"Файл не найден: {_inputFilePath}");

            var lines = File.ReadAllLines(_inputFilePath).ToList();
            if (lines.Count == 0)
                throw new InvalidOperationException("Файл пустой.");

            return lines;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при чтении файла: {ex.Message}");
            throw;
        }
    }

    public void WriteToFile(List<string> lines)
    {
        try
        {
            File.WriteAllLines(_outputFilePath, lines);
            Console.WriteLine($"Результат успешно записан в файл: {_outputFilePath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при записи в файл: {ex.Message}");
            throw;
        }
    }
}