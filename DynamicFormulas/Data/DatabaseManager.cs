using Microsoft.Data.Sqlite;
using DynamicFormulas.Models;
using System.Diagnostics;

namespace DynamicFormulas.Data;

/// <summary>ניהול מסד הנתונים - יצירת טבלאות, מילוי נתונים, טעינה ושמירה</summary>
public class DatabaseManager
{
    private readonly string _dbPath;
    private readonly int _dataCount;

    public DatabaseManager(string dbPath, int dataCount)
    {
        _dbPath = dbPath;
        _dataCount = dataCount;
    }

    private SqliteConnection OpenConnection()
    {
        var conn = new SqliteConnection($"Data Source={_dbPath}");
        conn.Open();
        return conn;
    }

    /// <summary>יצירת כל הטבלאות מחדש</summary>
    public void InitializeDatabase()
    {
        using var conn = OpenConnection();
        var sql = @"
            DROP TABLE IF EXISTS t_results;
            DROP TABLE IF EXISTS t_log;
            DROP TABLE IF EXISTS t_targil;
            DROP TABLE IF EXISTS t_data;

            CREATE TABLE t_data (
                data_id INTEGER PRIMARY KEY,
                a REAL NOT NULL,
                b REAL NOT NULL,
                c REAL NOT NULL,
                d REAL NOT NULL
            );

            CREATE TABLE t_targil (
                targil_id INTEGER PRIMARY KEY AUTOINCREMENT,
                targil TEXT NOT NULL,
                tnai TEXT,
                targil_false TEXT
            );

            CREATE TABLE t_results (
                resultsl_id INTEGER PRIMARY KEY AUTOINCREMENT,
                data_id INTEGER NOT NULL,
                targil_id INTEGER NOT NULL,
                method TEXT NOT NULL,
                result REAL,
                FOREIGN KEY (data_id) REFERENCES t_data(data_id),
                FOREIGN KEY (targil_id) REFERENCES t_targil(targil_id)
            );

            CREATE TABLE t_log (
                log_id INTEGER PRIMARY KEY AUTOINCREMENT,
                targil_id INTEGER NOT NULL,
                method TEXT NOT NULL,
                run_time REAL NOT NULL,
                FOREIGN KEY (targil_id) REFERENCES t_targil(targil_id)
            );
        ";
        using var cmd = new SqliteCommand(sql, conn);
        cmd.ExecuteNonQuery();
        Console.WriteLine("   OK: Tables created (t_data, t_targil, t_results, t_log)");
    }

    /// <summary>מילוי מיליון רשומות רנדומליות בטבלת t_data</summary>
    public void SeedData()
    {
        var sw = Stopwatch.StartNew();
        using var conn = OpenConnection();

        using (var checkCmd = new SqliteCommand("SELECT COUNT(*) FROM t_data", conn))
        {
            var count = Convert.ToInt64(checkCmd.ExecuteScalar());
            if (count >= _dataCount)
            {
                Console.WriteLine($"   OK: t_data already has {count:N0} rows, skipping...");
                return;
            }
        }

        var rng = new Random(42); // seed קבוע לשחזוריות
        using var transaction = conn.BeginTransaction();
        using var cmd = new SqliteCommand(
            "INSERT INTO t_data (data_id, a, b, c, d) VALUES (@id, @a, @b, @c, @d)", conn, transaction);
        cmd.Parameters.Add("@id", SqliteType.Integer);
        cmd.Parameters.Add("@a", SqliteType.Real);
        cmd.Parameters.Add("@b", SqliteType.Real);
        cmd.Parameters.Add("@c", SqliteType.Real);
        cmd.Parameters.Add("@d", SqliteType.Real);

        for (int i = 1; i <= _dataCount; i++)
        {
            cmd.Parameters["@id"].Value = i;
            cmd.Parameters["@a"].Value = Math.Round(rng.NextDouble() * 99 + 1, 2);
            cmd.Parameters["@b"].Value = Math.Round(rng.NextDouble() * 99 + 1, 2);
            cmd.Parameters["@c"].Value = Math.Round(rng.NextDouble() * 99 + 1, 2);
            cmd.Parameters["@d"].Value = Math.Round(rng.NextDouble() * 99 + 1, 2);
            cmd.ExecuteNonQuery();

            if (i % 200_000 == 0)
                Console.WriteLine($"   ... {i:N0}/{_dataCount:N0} rows");
        }

        transaction.Commit();
        sw.Stop();
        Console.WriteLine($"   OK: {_dataCount:N0} rows inserted ({sw.Elapsed.TotalSeconds:F1}s)");
    }

    /// <summary>מילוי טבלת נוסחאות עם מגוון סוגים</summary>
    public void SeedFormulas()
    {
        using var conn = OpenConnection();

        var formulas = new (string targil, string? tnai, string? falseTargil)[]
        {
            // נוסחאות פשוטות
            ("a + b", null, null),
            ("c * 2", null, null),
            ("a - b", null, null),
            ("d / 4", null, null),
            // נוסחאות מורכבות
            ("(a + b) * 8", null, null),
            ("sqrt(c * c + d * d)", null, null),
            ("log(b) + c", null, null),
            ("abs(d - b)", null, null),
            // נוסחאות עם תנאים
            ("b * 2", "a > 5", "b / 2"),
            ("a + 1", "b < 10", "d - 1"),
            ("1", "a == c", "0"),
        };

        using var cmd = new SqliteCommand(
            "INSERT INTO t_targil (targil, tnai, targil_false) VALUES (@t, @tn, @tf)", conn);
        cmd.Parameters.Add("@t", SqliteType.Text);
        cmd.Parameters.Add("@tn", SqliteType.Text);
        cmd.Parameters.Add("@tf", SqliteType.Text);

        foreach (var (targil, tnai, falseTargil) in formulas)
        {
            cmd.Parameters["@t"].Value = targil;
            cmd.Parameters["@tn"].Value = (object?)tnai ?? DBNull.Value;
            cmd.Parameters["@tf"].Value = (object?)falseTargil ?? DBNull.Value;
            cmd.ExecuteNonQuery();
        }
        Console.WriteLine($"   OK: {formulas.Length} formulas inserted into t_targil");
    }

    /// <summary>טעינת כל הנוסחאות מהמסד</summary>
    public List<FormulaRecord> LoadFormulas()
    {
        using var conn = OpenConnection();
        using var cmd = new SqliteCommand("SELECT targil_id, targil, tnai, targil_false FROM t_targil", conn);
        using var reader = cmd.ExecuteReader();
        var list = new List<FormulaRecord>();
        while (reader.Read())
            list.Add(new FormulaRecord(
                reader.GetInt32(0), reader.GetString(1),
                reader.IsDBNull(2) ? null : reader.GetString(2),
                reader.IsDBNull(3) ? null : reader.GetString(3)));
        return list;
    }

    /// <summary>טעינת כל שורות הנתונים מהמסד</summary>
    public List<DataRecord> LoadData()
    {
        using var conn = OpenConnection();
        using var cmd = new SqliteCommand("SELECT data_id, a, b, c, d FROM t_data", conn);
        using var reader = cmd.ExecuteReader();
        var list = new List<DataRecord>(_dataCount);
        while (reader.Read())
            list.Add(new DataRecord(
                reader.GetInt32(0), reader.GetDouble(1), reader.GetDouble(2),
                reader.GetDouble(3), reader.GetDouble(4)));
        return list;
    }

    /// <summary>שמירת רשומות לוג בטבלת t_log</summary>
    public void SaveLogs(List<LogEntry> logs)
    {
        using var conn = OpenConnection();
        using var tx = conn.BeginTransaction();
        using var cmd = new SqliteCommand(
            "INSERT INTO t_log (targil_id, method, run_time) VALUES (@tid, @m, @rt)", conn, tx);
        cmd.Parameters.Add("@tid", SqliteType.Integer);
        cmd.Parameters.Add("@m", SqliteType.Text);
        cmd.Parameters.Add("@rt", SqliteType.Real);

        foreach (var log in logs)
        {
            cmd.Parameters["@tid"].Value = log.TargilId;
            cmd.Parameters["@m"].Value = log.Method;
            cmd.Parameters["@rt"].Value = log.RunTimeSeconds;
            cmd.ExecuteNonQuery();
        }
        tx.Commit();
        Console.WriteLine($"   OK: {logs.Count} log entries saved to t_log");
    }

    /// <summary>שמירת תוצאות חישוב בטבלת t_results</summary>
    public void SaveResults(Dictionary<string, List<ResultEntry>> allResults)
    {
        using var conn = OpenConnection();
        using var tx = conn.BeginTransaction();
        using var cmd = new SqliteCommand(
            "INSERT INTO t_results (data_id, targil_id, method, result) VALUES (@did, @tid, @m, @r)", conn, tx);
        cmd.Parameters.Add("@did", SqliteType.Integer);
        cmd.Parameters.Add("@tid", SqliteType.Integer);
        cmd.Parameters.Add("@m", SqliteType.Text);
        cmd.Parameters.Add("@r", SqliteType.Real);

        int total = 0;
        foreach (var kvp in allResults)
        {
            foreach (var entry in kvp.Value)
            {
                cmd.Parameters["@did"].Value = entry.DataId;
                cmd.Parameters["@tid"].Value = entry.TargilId;
                cmd.Parameters["@m"].Value = entry.Method;
                cmd.Parameters["@r"].Value = double.IsNaN(entry.Result) ? DBNull.Value : entry.Result;
                cmd.ExecuteNonQuery();
                total++;
            }
        }
        tx.Commit();
        Console.WriteLine($"   OK: {total:N0} result entries saved to t_results");
    }

    /// <summary>פתיחת חיבור ציבורי (עבור SQLite Dynamic SQL)</summary>
    public SqliteConnection GetOpenConnection()
    {
        return OpenConnection();
    }
}
