using System.IO;

public class TableRow
{
    public Dictionary<string, string> Fields { get; set; }

    public TableRow()
    {
        Fields = new Dictionary<string, string>();
    }

    public string this[string key]
    {
        get => Fields.ContainsKey(key) ? Fields[key] : null;
        set => Fields[key] = value;
    }
}

public class Table
{
    public List<TableRow> Rows { get; set; }
    public List<string> Columns { get; set; }

    public Table()
    {
        Rows = new List<TableRow>();
        Columns = new List<string>();
    }

    public static Table LoadFromFile(string filePath)
    {
        var table = new Table();
        var lines = File.ReadAllLines(filePath);
        table.Columns = lines[0].Split(',').ToList();

        foreach (var line in lines.Skip(1))
        {
            var row = new TableRow();
            var values = line.Split(',');

            for (int i = 0; i < table.Columns.Count; i++)
            {
                row[table.Columns[i]] = values[i];
            }
            table.Rows.Add(row);
        }

        return table;
    }

    public void SaveToFile(string filePath)
    {
        using var writer = new StreamWriter(filePath);
        writer.WriteLine(string.Join(",", Columns));
        foreach (var row in Rows)
        {
            writer.WriteLine(string.Join(",", Columns.Select(col => row[col])));
        }
    }
}