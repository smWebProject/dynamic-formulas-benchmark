using System.Data;
using System.Diagnostics;
using DynamicFormulas.Data;
using DynamicFormulas.Helpers;
using DynamicFormulas.Models;

namespace DynamicFormulas.Methods;

/// <summary>שיטה 1: DataTable.Compute - שימוש ב-System.Data.DataTable לחישוב ביטויים</summary>
public class DataTableMethod
{
    private readonly DatabaseManager _db;
    private const string MethodName = "DataTable.Compute";

    public DataTableMethod(DatabaseManager db) => _db = db;

    public void Run(List<LogEntry> logs, Dictionary<string, List<ResultEntry>> allResults)
    {
        Console.WriteLine("--- Method 1: DataTable.Compute ---");
        var formulas = _db.LoadFormulas();
        var data = _db.LoadData();
        var dt = new DataTable();
        int sampleSize = Math.Min(1000, data.Count);

        foreach (var formula in formulas)
        {
            Console.Write($"   Formula {formula.Id}: {formula.Targil}");
            if (formula.Tnai != null) Console.Write($" [if: {formula.Tnai}]");

            var results = new List<ResultEntry>();
            var sw = Stopwatch.StartNew();

            for (int i = 0; i < data.Count; i++)
            {
                var row = data[i];
                try
                {
                    string activeFormula;
                    if (formula.Tnai != null)
                    {
                        bool cond = FormulaHelper.EvalConditionDT(formula.Tnai, row.A, row.B, row.C, row.D);
                        activeFormula = cond ? formula.Targil : (formula.FalseTargil ?? "0");
                    }
                    else
                        activeFormula = formula.Targil;

                    var expr = FormulaHelper.ToDT(activeFormula, row.A, row.B, row.C, row.D);
                    var result = Convert.ToDouble(dt.Compute(expr, ""));

                    if (i < sampleSize)
                        results.Add(new ResultEntry(row.Id, formula.Id, MethodName, Math.Round(result, 6)));
                }
                catch
                {
                    if (i < sampleSize)
                        results.Add(new ResultEntry(row.Id, formula.Id, MethodName, double.NaN));
                }
            }

            sw.Stop();
            logs.Add(new LogEntry(formula.Id, formula.Targil, MethodName, sw.Elapsed.TotalSeconds));
            allResults[$"{MethodName}_{formula.Id}"] = results;
            Console.WriteLine($" -> {sw.Elapsed.TotalSeconds:F3}s");
        }
    }
}
