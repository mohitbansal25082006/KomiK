using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace Komik.Helpers;

/// <summary>
/// Natural sort comparer that orders numbers logically (e.g. page 2 comes before page 10).
/// Uses Windows native StrCmpLogicalW with a robust regex-based managed fallback.
/// </summary>
public sealed class NaturalSortComparer : IComparer<string>
{
    public static NaturalSortComparer Instance { get; } = new();

    [DllImport("shlwapi.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
    private static extern int StrCmpLogicalW(string? psz1, string? psz2);

    private static readonly bool HasShlwapi = OperatingSystem.IsWindows();

    public int Compare(string? x, string? y)
    {
        if (ReferenceEquals(x, y)) return 0;
        if (x is null) return -1;
        if (y is null) return 1;

        if (HasShlwapi)
        {
            try
            {
                return StrCmpLogicalW(x, y);
            }
            catch
            {
                // Fall through to managed fallback
            }
        }

        return FallbackCompare(x, y);
    }

    private static int FallbackCompare(string x, string y)
    {
        var regex = new Regex(@"(\d+)|(\D+)");
        var xMatches = regex.Matches(x);
        var yMatches = regex.Matches(y);

        int count = Math.Min(xMatches.Count, yMatches.Count);
        for (int i = 0; i < count; i++)
        {
            string xPart = xMatches[i].Value;
            string yPart = yMatches[i].Value;

            if (ulong.TryParse(xPart, out var xNum) && ulong.TryParse(yPart, out var yNum))
            {
                int cmp = xNum.CompareTo(yNum);
                if (cmp != 0) return cmp;
            }
            else
            {
                int cmp = string.Compare(xPart, yPart, StringComparison.OrdinalIgnoreCase);
                if (cmp != 0) return cmp;
            }
        }

        return xMatches.Count.CompareTo(yMatches.Count);
    }
}
