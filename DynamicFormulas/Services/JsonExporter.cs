using System.Text.Json;
using DynamicFormulas.Data;
using DynamicFormulas.Models;

namespace DynamicFormulas.Services;

/// <summary>ייצוא תוצאות לקובץ JSON עבור דוח Angular</summary>
public class JsonExporter
{
    private readonly DatabaseManager _db;
    private readonly string _outputPath;
    private readonly int _dataCount;

    public JsonExporter(DatabaseManager db, string outputPath, int dataCount)
    {
        _db = db;
        _outputPath = outputPath;
        _dataCount = dataCount;
    }

    public void Export(List<LogEntry> logs, Dictionary<string, List<ResultEntry>> allResults)
    {
        var formulaList = _db.LoadFormulas();

        var report = new
        {
            generatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            dataCount = _dataCount,
            formulas = formulaList.Select(f => new
            {
                id = f.Id,
                formula = f.Targil,
                condition = f.Tnai,
                falseFormula = f.FalseTargil,
                type = f.Tnai != null ? "conditional" :
                       (f.Targil.Contains("sqrt") || f.Targil.Contains("log") || f.Targil.Contains("abs"))
                       ? "complex" : "simple"
            }).ToList(),
            performanceLogs = logs.Select(l => new
            {
                formulaId = l.TargilId,
                formulaName = l.FormulaName,
                method = l.Method,
                runTimeSeconds = Math.Round(l.RunTimeSeconds, 4)
            }).ToList(),
            summary = logs.GroupBy(l => l.Method).Select(g => new
            {
                method = g.Key,
                totalTime = Math.Round(g.Sum(l => l.RunTimeSeconds), 4),
                avgTime = Math.Round(g.Average(l => l.RunTimeSeconds), 4),
                minTime = Math.Round(g.Min(l => l.RunTimeSeconds), 4),
                maxTime = Math.Round(g.Max(l => l.RunTimeSeconds), 4),
                formulaCount = g.Count()
            }).ToList(),
            sampleResults = allResults
                .SelectMany(kvp => kvp.Value.Take(5))
                .Select(r => new
                {
                    dataId = r.DataId,
                    formulaId = r.TargilId,
                    method = r.Method,
                    result = double.IsNaN(r.Result) ? (double?)null : r.Result
                }).ToList()
        };

        var dir = Path.GetDirectoryName(_outputPath);
        if (dir != null && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        var json = JsonSerializer.Serialize(report, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        File.WriteAllText(_outputPath, json);
        Console.WriteLine($"   OK: JSON exported to {_outputPath}");
    }
}
