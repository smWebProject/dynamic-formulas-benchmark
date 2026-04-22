namespace DynamicFormulas.Models;

/// <summary>נוסחה מטבלת t_targil</summary>
public record FormulaRecord(int Id, string Targil, string? Tnai, string? FalseTargil);

/// <summary>שורת נתונים מטבלת t_data</summary>
public record DataRecord(int Id, double A, double B, double C, double D);

/// <summary>רשומת לוג - זמן ריצה לכל נוסחה ושיטה</summary>
public record LogEntry(int TargilId, string FormulaName, string Method, double RunTimeSeconds);

/// <summary>רשומת תוצאה - תוצאת חישוב לכל שורת נתונים</summary>
public record ResultEntry(int DataId, int TargilId, string Method, double Result);
