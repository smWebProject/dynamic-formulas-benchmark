using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using DynamicFormulas.Data;
using DynamicFormulas.Helpers;
using DynamicFormulas.Models;

namespace DynamicFormulas.Methods;

/// <summary>שיטה 2: Roslyn Scripting - קומפילציה דינמית חד-פעמית והרצה מהירה</summary>
public class RoslynMethod
{
    private readonly DatabaseManager _db;
    private const string MethodName = "Roslyn";

    public RoslynMethod(DatabaseManager db) => _db = db;

    public void Run(List<LogEntry> logs, Dictionary<string, List<ResultEntry>> allResults)
    {
        Console.WriteLine("\n--- Method 2: Roslyn Scripting ---");
        var formulas = _db.LoadFormulas();
        var data = _db.LoadData();
        int sampleSize = Math.Min(1000, data.Count);

        foreach (var formula in formulas)
        {
            Console.Write($"   Formula {formula.Id}: {formula.Targil}");
            if (formula.Tnai != null) Console.Write($" [if: {formula.Tnai}]");

            var results = new List<ResultEntry>();

            try
            {
                var options = ScriptOptions.Default
                    .AddReferences(typeof(Math).Assembly)
                    .AddImports("System");

                // בניית ביטוי C# מלא עם/בלי תנאי
                string scriptCode;
                if (formula.Tnai != null)
                {
                    var csTrue = FormulaHelper.ToCS(formula.Targil);
                    var csFalse = FormulaHelper.ToCS(formula.FalseTargil ?? "0");
                    scriptCode = $"({formula.Tnai}) ? (double)({csTrue}) : (double)({csFalse})";
                }
                else
                    scriptCode = $"(double)({FormulaHelper.ToCS(formula.Targil)})";

                // קומפילציה חד-פעמית - זה היתרון של Roslyn
                var script = CSharpScript.Create<double>(scriptCode, options, typeof(FormulaGlobals));
                var runner = script.CreateDelegate();

                var sw = Stopwatch.StartNew();

                for (int i = 0; i < data.Count; i++)
                {
                    var row = data[i];
                    try
                    {
                        var globals = new FormulaGlobals { a = row.A, b = row.B, c = row.C, d = row.D };
                        var result = runner(globals).Result;

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
            catch (Exception ex)
            {
                Console.WriteLine($" -> Compile error: {ex.Message}");
                logs.Add(new LogEntry(formula.Id, formula.Targil, MethodName, -1));
            }
        }
    }
}
