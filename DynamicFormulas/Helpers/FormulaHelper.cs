using System.Data;
using System.Text.RegularExpressions;

namespace DynamicFormulas.Helpers;

/// <summary>עזרים להמרת נוסחאות בין פורמטים שונים</summary>
public static class FormulaHelper
{
    private static readonly System.Globalization.CultureInfo Inv =
        System.Globalization.CultureInfo.InvariantCulture;

    /// <summary>המרת נוסחה לביטוי C# תקין (עבור Roslyn)</summary>
    public static string ToCS(string formula)
    {
        return formula
            .Replace("sqrt(", "Math.Sqrt(")
            .Replace("log(", "Math.Log(")
            .Replace("abs(", "Math.Abs(");
    }

    /// <summary>המרת נוסחה לפורמט DataTable.Compute עם הצבת ערכים</summary>
    public static string ToDT(string formula, double a, double b, double c, double d)
    {
        // שמירה על abs מפני החלפה של 'a'
        var expr = Regex.Replace(formula, @"\babs\(", "ABS_PLACEHOLDER(");
        expr = Regex.Replace(expr, @"\ba\b", a.ToString(Inv));
        expr = Regex.Replace(expr, @"\bb\b", b.ToString(Inv));
        expr = Regex.Replace(expr, @"\bc\b", c.ToString(Inv));
        expr = Regex.Replace(expr, @"\bd\b", d.ToString(Inv));
        expr = expr.Replace("ABS_PLACEHOLDER(", "abs(");

        return ResolveMathFunctions(expr);
    }

    /// <summary>הערכת תנאי עבור DataTable</summary>
    public static bool EvalConditionDT(string condition, double a, double b, double c, double d)
    {
        var expr = Regex.Replace(condition, @"\ba\b", a.ToString(Inv));
        expr = Regex.Replace(expr, @"\bb\b", b.ToString(Inv));
        expr = Regex.Replace(expr, @"\bc\b", c.ToString(Inv));
        expr = Regex.Replace(expr, @"\bd\b", d.ToString(Inv));
        var dt = new DataTable();
        return Convert.ToInt32(dt.Compute($"IIF({expr}, 1, 0)", "")) == 1;
    }

    /// <summary>המרת תנאי לפורמט SQLite (== הופך ל-=)</summary>
    public static string ConditionToSql(string condition)
    {
        return condition.Replace("==", "=");
    }

    /// <summary>חישוב פונקציות מתמטיות ש-DataTable לא תומך בהן</summary>
    private static string ResolveMathFunctions(string expr)
    {
        var dt = new DataTable();

        expr = Regex.Replace(expr, @"sqrt\(([^)]+)\)", m =>
        {
            var val = Convert.ToDouble(dt.Compute(m.Groups[1].Value, ""));
            return Math.Sqrt(val).ToString(Inv);
        });

        expr = Regex.Replace(expr, @"log\(([^)]+)\)", m =>
        {
            var val = Convert.ToDouble(dt.Compute(m.Groups[1].Value, ""));
            return Math.Log(val).ToString(Inv);
        });

        expr = Regex.Replace(expr, @"abs\(([^)]+)\)", m =>
        {
            var val = Convert.ToDouble(dt.Compute(m.Groups[1].Value, ""));
            return Math.Abs(val).ToString(Inv);
        });

        return expr;
    }
}
