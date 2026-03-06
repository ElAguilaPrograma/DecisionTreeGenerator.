using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;
class Program
{
    static void Main(string[] args)
    {
        using var reader = new StreamReader(Path.Combine(AppContext.BaseDirectory, "Csv", "DETREETITANIC.csv"));
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true
        };
        using var csv = new CsvReader(reader, config);
        var records = csv.GetRecords<DataRecord>().ToList();
        List<string> columnsName = csv.HeaderRecord.ToList();

        List<DataResult> results = new List<DataResult>();

        // Print full dataset
        /*
        Console.WriteLine($"{"SURVIVED",-10} | {"PCLASS",-10} | {"SEX",-10} | {"EMBARKED",-10}");
        Console.WriteLine(new string('-', 50));

        foreach (var record in records)
        {
            Console.WriteLine($"{record.Survived,-10} | {record.Pclass,-10} | {record.Sex,-10} | {record.Embarked,-10}");
        }

        */

        Routes routes = new Routes();
        var allRoutes = routes.GenerateRoutes(columnsName);
        int counter = 1;
        foreach (var route in allRoutes)
        {
            Console.WriteLine($"Route {counter++}: {string.Join(" -> ", route)}");
        }

        List<DataResult> allResultsToExport = new List<DataResult>();
        double lowestEntropy = double.MaxValue;
        List<string>? bestTree = null;
        int treeCounter = 0;
        // Empieza lo chido.
        foreach (var route in allRoutes)
        {
            treeCounter++;
            Console.WriteLine($"\n=== Generating Tree {treeCounter}: {string.Join(" -> ", route)} ===");
            List<DataResult> resultsTree = new List<DataResult>();

            ProcessLevel(records, route, 0, "", records.Count, resultsTree);

            Console.WriteLine($"{"Route",-55} | {"Ni",-10} | {"Pi",-10} | {"Entropy",-10}");
            foreach (var res in resultsTree)
            {
                Console.WriteLine($"{res.Route,-55} | {res.Ni,-10} | {res.Pi,10:F4} | {res.Entropy,10:F4}");
            }
            double totalEntropy = resultsTree.Sum(r => r.Entropy);
            if (totalEntropy < lowestEntropy)
            {
                lowestEntropy = totalEntropy;
                bestTree = route;
            }
            Console.WriteLine("Total entropy tree: " + totalEntropy);

            string currentTreeName = string.Join(" -> ", route);
            foreach (var res in resultsTree)
            {
                res.TreeName = currentTreeName;
                allResultsToExport.Add(res);
            }

            allResultsToExport.Add(new DataResult
            {
                TreeName = "Total entropy of the tree",
                Route = currentTreeName,
                Entropy = totalEntropy
            });
        }

        allResultsToExport.Add(new DataResult { TreeName = "- Best Tree -" });
        allResultsToExport.Add(new DataResult
        {
            TreeName = "Best",
            Route = string.Join(" -> ", bestTree),
            Entropy = lowestEntropy
        });

        Console.WriteLine("\nBest Tree (Lowest Entropy)");
        Console.WriteLine($"Route: {string.Join(" -> ", bestTree)}");
        Console.WriteLine($"Entropy: {lowestEntropy}");

        string outputPath = Path.Combine(AppContext.BaseDirectory, "ResultadosEntropia.csv");
        using (var writer = new StreamWriter(outputPath))
        using (var csvWriter = new CsvWriter(writer, CultureInfo.InvariantCulture))
        {
            csvWriter.WriteRecords(allResultsToExport);
        }

        Console.WriteLine($"\nExport complete! File saved in: {outputPath}");

        static void ProcessLevel(List<DataRecord> currentData, List<string> columnsRoute, int indexLevel, string textPath, int totalFather, List<DataResult> results)
        {
            if (indexLevel >= columnsRoute.Count || currentData.Count == 0) return;

            string columnName = columnsRoute[indexLevel];

            var uniqueValues = GetUniqueValues(currentData, columnName);

            foreach (var value in uniqueValues)
            {
                var filteredRecords = FilterByColumn(currentData, columnName, value);

                int ni = filteredRecords.Count;
                double pi = (double)ni / totalFather;
                double h = (pi > 0) ? -pi * Math.Log10(pi) : 0;

                string newRouteText = string.IsNullOrEmpty(textPath) ? $"{columnName}: {value}" : $"{textPath} -> {columnName}: {value}";

                results.Add(new DataResult
                {
                    Route = newRouteText,
                    Ni = ni,
                    Pi = pi,
                    Entropy = h
                });

                ProcessLevel(filteredRecords, columnsRoute, indexLevel + 1, newRouteText, ni, results);
            }
        }

        static List<object?> GetUniqueValues(List<DataRecord> data, string column)
        {
            var prop = typeof(DataRecord).GetProperty(column);
            return data
                .Select(r => prop.GetValue(r))
                .Distinct()
                .ToList();
        }

        static List<DataRecord> FilterByColumn(List<DataRecord> data, string column, object value)
        {
            var prop = typeof(DataRecord).GetProperty(column);
            return data
                .Where(r => Equals(prop.GetValue(r), value))
                .ToList();
        }

        Console.ReadLine();
    }
}

public class DataRecord
{
    public int Survived { get; set; } 
    public int Pclass { get; set; }   
    public string Sex { get; set; }
    public string? Embarked { get; set; }
}

public class DataResult
{
    public string? TreeName { get; set; }
    public string? Route { get; set; }
    public int Ni { get; set; }
    public double Pi { get; set; }
    public double Entropy { get; set; }
}

class Routes
{
    public List<List<string>> GenerateRoutes(List<string> columns)
    {
        var result = new List<List<string>>();
        Permute(columns, 0, columns.Count - 1, result);
        return result;
    }

    private static void Permute(List<string> list, int start, int end, List<List<string>> result)
    {
        if (start == end)
        {
            result.Add(new List<string>(list));
        }
        else
        {
            for (int i = start; i <= end; i++)
            {
                Exchange(list, start, i);
                Permute(list, start + 1, end, result);
                Exchange(list, start, i);
            }
        }
    }

    private static void Exchange(List<string> list, int a, int b)
    {
        string temp = list[a];
        list[a] = list[b];
        list[b] = temp;
    }
}
