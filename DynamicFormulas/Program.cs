// ============================================================================
// מערכת חישוב נוסחאות דינמיות - Dynamic Formula Calculation System
// 3 שיטות: DataTable.Compute | Roslyn Scripting | SQLite Dynamic SQL
// ============================================================================

using Microsoft.Data.Sqlite;
using DynamicFormulas.Data;
using DynamicFormulas.Methods;
using DynamicFormulas.Models;
using DynamicFormulas.Services;

const string DB_PATH = "dynamic_formulas.db";
const string JSON_OUTPUT = "../DynamicFormulasFronted/report-dashboard/src/assets/results.json";
const int DATA_COUNT = 1_000_000;

Console.OutputEncoding = System.Text.Encoding.UTF8;

// --- מצב הצגת מסד נתונים ---
if (args.Length > 0 && args[0] == "--show-db")
{
    ShowDatabase();
    return;
}

Console.WriteLine("╔══════════════════════════════════════════════════════╗");
Console.WriteLine("║   Dynamic Formula Calculation System                ║");
Console.WriteLine("╚══════════════════════════════════════════════════════╝\n");

var db = new DatabaseManager(DB_PATH, DATA_COUNT);

// שלב 1: יצירת מסד נתונים וטבלאות
Console.WriteLine(">> Step 1: Creating SQLite database and tables...");
db.InitializeDatabase();

// שלב 2: מילוי מיליון רשומות רנדומליות
Console.WriteLine($">> Step 2: Seeding {DATA_COUNT:N0} random rows into t_data...");
db.SeedData();

// שלב 3: מילוי טבלת נוסחאות
Console.WriteLine(">> Step 3: Seeding formulas into t_targil...");
db.SeedFormulas();

// שלב 4: הרצת 3 שיטות חישוב
Console.WriteLine("\n>> Step 4: Running 3 computation methods...\n");
var logs = new List<LogEntry>();
var allResults = new Dictionary<string, List<ResultEntry>>();

new DataTableMethod(db).Run(logs, allResults);
new RoslynMethod(db).Run(logs, allResults);
new SqliteMethod(db).Run(logs, allResults);

// שלב 5: שמירת לוג ותוצאות
Console.WriteLine("\n>> Step 5: Saving results to t_results and t_log...");
db.SaveLogs(logs);
db.SaveResults(allResults);

// שלב 6: השוואת תוצאות
Console.WriteLine("\n>> Step 6: Comparing results across methods...");
ResultComparer.Compare(allResults);

// שלב 7: ייצוא JSON לדוח Angular
Console.WriteLine("\n>> Step 7: Exporting results to JSON for Angular report...");
new JsonExporter(db, JSON_OUTPUT, DATA_COUNT).Export(logs, allResults);

Console.WriteLine("\n✅ Done! You can now run the Angular report dashboard.");

// ============================================================================
// פונקציה להצגת מבנה ונתוני מסד הנתונים (להרצה עם --show-db)
// ============================================================================
void ShowDatabase()
{
    Console.WriteLine("╔══════════════════════════════════════════════════╗");
    Console.WriteLine("║         מבנה מסד הנתונים - SQLite               ║");
    Console.WriteLine("╚══════════════════════════════════════════════════╝\n");

    using var conn = new SqliteConnection($"Data Source={DB_PATH}");
    conn.Open();

    // Schema
    Console.WriteLine("=== מבנה הטבלאות (Schema) ===\n");
    using (var cmd = new SqliteCommand("SELECT sql FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite%'", conn))
    using (var r = cmd.ExecuteReader())
        while (r.Read()) Console.WriteLine(r.GetString(0) + ";\n");

    // t_data
    Console.WriteLine("=== t_data - דגימת 5 רשומות מתוך מיליון ===\n");
    PrintTable(conn, "SELECT * FROM t_data LIMIT 5");
    using (var cmd = new SqliteCommand("SELECT COUNT(*) FROM t_data", conn))
        Console.WriteLine($"  סה\"כ רשומות: {cmd.ExecuteScalar():N0}\n");

    // t_targil
    Console.WriteLine("=== t_targil - כל הנוסחאות ===\n");
    PrintTable(conn, "SELECT * FROM t_targil");

    // t_log
    Console.WriteLine("\n=== t_log - זמני ריצה ===\n");
    PrintTable(conn, "SELECT * FROM t_log");

    // t_results
    Console.WriteLine("\n=== t_results - דגימת 10 תוצאות ===\n");
    PrintTable(conn, "SELECT * FROM t_results LIMIT 10");
    using (var cmd = new SqliteCommand("SELECT COUNT(*) FROM t_results", conn))
        Console.WriteLine($"  סה\"כ תוצאות: {cmd.ExecuteScalar():N0}");
}

void PrintTable(SqliteConnection conn, string sql)
{
    using var cmd = new SqliteCommand(sql, conn);
    using var r = cmd.ExecuteReader();

    var cols = Enumerable.Range(0, r.FieldCount).Select(r.GetName).ToArray();
    var widths = cols.Select(c => c.Length).ToArray();
    var rows = new List<string[]>();

    while (r.Read())
    {
        var row = new string[r.FieldCount];
        for (int i = 0; i < r.FieldCount; i++)
        {
            row[i] = r.IsDBNull(i) ? "NULL" : r.GetValue(i)?.ToString() ?? "";
            if (row[i].Length > widths[i]) widths[i] = Math.Min(row[i].Length, 25);
        }
        rows.Add(row);
    }

    Console.WriteLine(string.Join(" | ", cols.Select((c, i) => c.PadRight(widths[i]))));
    Console.WriteLine(new string('-', cols.Select((_, i) => widths[i]).Sum() + (cols.Length - 1) * 3));
    foreach (var row in rows)
        Console.WriteLine(string.Join(" | ", row.Select((v, i) => v.Length > widths[i] ? v[..widths[i]] : v.PadRight(widths[i]))));
    Console.WriteLine($"({rows.Count} rows)");
}
