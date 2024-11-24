using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class Country
{
    public string Name { get; set; }
    public string Continent { get; set; }
    public string Capital { get; set; }
    public int Area { get; set; }
    public long Population { get; set; }
}

public class CountrySorter
{
    private List<Country> countries;

    public CountrySorter(string filePath)
    {
        countries = LoadCountriesFromFile(filePath);
    }

    private List<Country> LoadCountriesFromFile(string filePath)
    {
        var result = new List<Country>();
        try
        {
            foreach (var line in File.ReadAllLines(filePath).Skip(1)) // Пропускаем заголовок
            {
                var parts = line.Split(',');
                if (parts.Length == 5)
                {
                    result.Add(new Country
                    {
                        Name = parts[0],
                        Continent = parts[1],
                        Capital = parts[2],
                        Area = int.Parse(parts[3]),
                        Population = long.Parse(parts[4])
                    });
                }
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"Ошибка при чтении файла: {ex.Message}");
        }
        return result;
    }

    public List<Country> FilterAndSort(string continent, string sortAttribute)
    {
        var filtered = countries.Where(c => c.Continent == continent).ToList();

        return sortAttribute switch
        {
            "Столица" => filtered.OrderBy(c => c.Capital).ToList(),
            "Площадь" => filtered.OrderByDescending(c => c.Area).ToList(),
            "Численность населения" => filtered.OrderByDescending(c => c.Population).ToList(),
            _ => throw new ArgumentException("Неверный атрибут сортировки.")
        };
    }

    public void SaveToFile(List<Country> sortedCountries, string outputFilePath)
    {
        try
        {
            using var writer = new StreamWriter(outputFilePath);
            writer.WriteLine("Название,Континент,Столица,Площадь (км²),Численность населения");
            foreach (var country in sortedCountries)
            {
                writer.WriteLine($"{country.Name},{country.Continent},{country.Capital},{country.Area},{country.Population}");
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"Ошибка записи в файл: {ex.Message}");
        }
    }
}
