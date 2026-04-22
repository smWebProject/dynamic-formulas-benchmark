using DynamicFormulas.Models;

namespace DynamicFormulas.Services;

/// <summary>השוואת תוצאות בין 3 השיטות - וידוא שכולן מחזירות אותם ערכים</summary>
public static class ResultComparer
{
    private static readonly string[] Methods = ["DataTable.Compute", "Roslyn", "SQLite"];

    public static void Compare(Dictionary<string, List<ResultEntry>> allResults)
    {
        var formulaIds = allResults.Values
            .SelectMany(v => v.Select(r => r.TargilId))
            .Distinct().OrderBy(x => x).ToList();

        bool allMatch = true;

        foreach (var fid in formulaIds)
        {
            var byMethod = new Dictionary<string, List<ResultEntry>>();
            foreach (var m in Methods)
            {
                var key = $"{m}_{fid}";
                if (allResults.ContainsKey(key))
                    byMethod[m] = allResults[key];
            }
            if (byMethod.Count < 2) continue;

            var mNames = byMethod.Keys.ToList();
            for (int i = 0; i < mNames.Count - 1; i++)
            {
                for (int j = i + 1; j < mNames.Count; j++)
                {
                    var l1 = byMethod[mNames[i]];
                    var l2 = byMethod[mNames[j]];
                    int min = Math.Min(l1.Count, l2.Count);
                    int mismatches = 0;

                    for (int k = 0; k < min; k++)
                    {
                        if (double.IsNaN(l1[k].Result) || double.IsNaN(l2[k].Result)) continue;
                        if (Math.Abs(l1[k].Result - l2[k].Result) > 0.01) mismatches++;
                    }

                    if (mismatches > 0)
                    {
                        Console.WriteLine($"   WARNING: Formula {fid}: {mismatches} mismatches between {mNames[i]} and {mNames[j]}");
                        allMatch = false;
                    }
                }
            }
        }

        if (allMatch)
            Console.WriteLine("   OK: All results match across all 3 methods!");
    }
}
