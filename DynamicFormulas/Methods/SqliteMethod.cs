using System.Diagnostics;
using Microsoft.Data.Sqlite;
using DynamicFormulas.Data;
using DynamicFormulas.Helpers;
using DynamicFormulas.Models;

namespace DynamicFormulas.Methods;

/// <summary>שיטה 3: SQLite Dynamic SQL - שאילתות SQL דינמיות ישירות במסד הנתונים</summary>
public class SqliteMethod
{
    private readonly DatabaseManager _db;
    private const string MethodName = "SQLite";

    public SqliteMethod(DatabaseManager db) => _db = db;

    public void Run(List<LogEntry> logs, Dictionary<string, List<ResultEntry>> allResults)
    {
        Console.WriteLine("\n--- Method 3: SQLite Dynamic SQL ---");
        var formulas = _db.LoadFormulas();
        int sampleSize = 1000;

        using var conn = _db.GetOpenConnection();

        // רישום פונקציות מתמטיות מותאמות ב-SQLite
        conn.CreateFunction("sqrt", (double x) => Math.Sqrt(x));
        conn.CreateFunction("log", (double x) => Math.Log(x));
        conn.CreateFunction("pow", (double x, double y) => Math.Pow(x, y));

        foreach (var formula in formulas)
        {
            Console.Write($"   Formula {formula.Id}: {formula.Targil}");
            if (formula.Tnai != null) Console.Write($" [if: {formula.Tnai}]");

            var results = new List<ResultEntry>();

            try
            {
                // בניית שאילתת SQL דינמית
                string sqlExpr;
                if (formula.Tnai != null)
                {
                    var sqlCond = FormulaHelper.ConditionToSql(formula.Tnai);
                    sqlExpr = $"CASE WHEN {sqlCond} THEN {formula.Targil} ELSE {formula.FalseTargil ?? "0"} END";
                }
                else
                    sqlExpr = formula.Targil;

                var sql = $"SELECT data_id, ({sqlExpr}) as result FROM t_data";
                var sw = Stopwatch.StartNew();

                using var cmd = new SqliteCommand(sql, conn);
                using var reader = cmd.ExecuteReader();
                int count = 0;
                while (reader.Read())
                {
                    var dataId = reader.GetInt32(0);
                    var result = reader.IsDBNull(1) ? double.NaN : reader.GetDouble(1);
                    if (count < sampleSize)
                        results.Add(new ResultEntry(dataId, formula.Id, MethodName, Math.Round(result, 6)));
                    count++;
                }

                sw.Stop();
                logs.Add(new LogEntry(formula.Id, formula.Targil, MethodName, sw.Elapsed.TotalSeconds));
                allResults[$"{MethodName}_{formula.Id}"] = results;
                Console.WriteLine($" -> {sw.Elapsed.TotalSeconds:F3}s");
            }
            catch (Exception ex)
            {
                Console.WriteLine($" -> Error: {ex.Message}");
                logs.Add(new LogEntry(formula.Id, formula.Targil, MethodName, -1));
            }
        }
    }
}
