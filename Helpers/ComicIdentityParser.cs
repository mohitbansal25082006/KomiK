using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Komik.Models;

namespace Komik.Helpers;

public enum ComicNumberKind
{
    None,
    Issue,
    Chapter,
    Volume,
    Part,
    Annual,
    Special
}

/// <summary>
/// What a comic file "is": its series, the creators credited in its name or metadata, and its
/// volume / issue / chapter / part numbers. Shared by series grouping, duplicate detection and reading
/// statistics so they all agree.
/// </summary>
public sealed class ComicIdentity
{
    public string SeriesName { get; init; } = string.Empty;

    /// <summary>Compact comparison key: lowercase letters and digits only, no leading "the", accents removed.</summary>
    public string SeriesKey { get; init; } = string.Empty;

    /// <summary>Key of an alternate title given after " - " (e.g. a translated title), or empty.</summary>
    public string AltTitleKey { get; init; } = string.Empty;

    public double? Volume { get; init; }
    public double? Issue { get; init; }
    public int? Year { get; init; }
    public ComicNumberKind Kind { get; init; }

    /// <summary>Creators credited in the file name ("[Circle (Artist)] Title") or in metadata (writers/artists).</summary>
    public IReadOnlyList<string> Creators { get; init; } = Array.Empty<string>();

    public IReadOnlyList<string> CreatorKeys => Creators.Select(ComicIdentityParser.MakeKey).Where(k => k.Length > 0).Distinct().ToList();

    /// <summary>Creators credited in the title itself (not metadata), used to tell same-named works by different people apart.</summary>
    public IReadOnlyList<string> TitleCreators { get; init; } = Array.Empty<string>();

    /// <summary>The cleaned title without release tags, used for fuzzy comparisons.</summary>
    public string CleanTitle { get; init; } = string.Empty;

    /// <summary>True when the series/number came from ComicInfo-style metadata rather than the file name.</summary>
    public bool FromMetadata { get; init; }

    public bool HasNumber => Issue.HasValue || Volume.HasValue;

    /// <summary>Position in a continuation: explicit number, or 1 for an un-numbered first book.</summary>
    public double Position => Issue ?? Volume ?? 1;

    /// <summary>Identity of this exact book (series + volume + kind + number), or null when no number was found.</summary>
    public string? BookKey
    {
        get
        {
            if (!HasNumber || SeriesKey.Length == 0) return null;
            string kind = Kind switch
            {
                ComicNumberKind.Annual => "a",
                ComicNumberKind.Special => "s",
                ComicNumberKind.Part => "p",
                _ => string.Empty
            };
            string vol = Volume.HasValue ? Volume.Value.ToString("0.###", CultureInfo.InvariantCulture) : "-";
            string issue = Issue.HasValue ? Issue.Value.ToString("0.###", CultureInfo.InvariantCulture) : "-";
            return $"{SeriesKey}|v{vol}|{kind}{issue}";
        }
    }

    public string NumberLabel
    {
        get
        {
            var parts = new List<string>();
            if (Volume.HasValue) parts.Add($"Vol. {FormatNumber(Volume.Value)}");
            if (Issue.HasValue)
            {
                parts.Add(Kind switch
                {
                    ComicNumberKind.Chapter => $"Ch. {FormatNumber(Issue.Value)}",
                    ComicNumberKind.Part => $"Part {FormatNumber(Issue.Value)}",
                    ComicNumberKind.Annual => $"Annual {FormatNumber(Issue.Value)}",
                    ComicNumberKind.Special => $"Special {FormatNumber(Issue.Value)}",
                    _ => $"#{FormatNumber(Issue.Value)}"
                });
            }
            return string.Join(" ", parts);
        }
    }

    public static string FormatNumber(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);
}

public static class ComicIdentityParser
{
    private static readonly string[] KnownExtensions =
    {
        ".cbz", ".cbr", ".cb7", ".cbt", ".zip", ".rar", ".7z", ".tar", ".pdf", ".epub"
    };

    private static readonly HashSet<string> GenericFolderNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "comics", "comic", "manga", "downloads", "download", "books", "library", "documents", "desktop",
        "new folder", "misc", "other", "unsorted", "read", "to read", "archive", "archives", "files", "temp", "tmp",
        "cbz", "cbr", "pdf", "complete", "completed", "ongoing"
    };

    /// <summary>Bracket contents that are release/format tags, never creator names.</summary>
    private static readonly Regex TagLike = new(
        @"^(?:english|eng|japanese|jpn|chinese|korean|spanish|french|german|russian|portuguese|italian|raw|digital|full[\s-]?colou?r|colou?r(?:ized)?|uncensored|decensored|censored|ongoing|complete(?:d)?|translated|translation|rough translation|ai translation|hd|c2c|webrip|scan(?:ned)?|dl|x\d+|\d+p|oneshot|one[\s-]?shot|\d{4}|c\d+|comic\s?market.*|comiket.*)$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // "site-123456 - Title", "123456 - Title": download IDs in front of the real title.
    private static readonly Regex SourcePrefix = new(
        @"^\s*(?:[A-Za-z][A-Za-z0-9]{1,24}[\s_-]*)?\d{4,}\s*[-–_]\s+",
        RegexOptions.Compiled);

    // "original - [Circle (Artist)] Title": a short category label some downloaders write in front of the
    // bracketed credit. The label is not part of the title, so two copies differing only by it still match.
    private static readonly Regex LabelPrefix = new(
        @"^\s*(?<label>\p{L}[\p{L}' ]{0,28}?)\s+[-–]\s+(?=\[(?<credit>[^\[\]]+)\])",
        RegexOptions.Compiled);

    // Innermost bracket group; applied repeatedly so "[Circle (Artist)]" is removed completely.
    private static readonly Regex BracketGroup = new(@"\(([^()\[\]{}]*)\)|\[([^()\[\]{}]*)\]|\{([^()\[\]{}]*)\}", RegexOptions.Compiled);
    private static readonly Regex EqualsTag = new(@"=[^=\s][^=]{0,30}=", RegexOptions.Compiled);
    private static readonly Regex PlusExtras = new(@"\s\+\s.*$", RegexOptions.Compiled);
    private static readonly Regex YearOnly = new(@"^\s*((?:19|20)\d{2})\s*$", RegexOptions.Compiled);
    private static readonly Regex DotsAsSpaces = new(@"(?<!\d)\.|\.(?!\d)", RegexOptions.Compiled);
    private static readonly Regex Whitespace = new(@"\s+", RegexOptions.Compiled);

    private static readonly Regex VolumeChapter = new(
        @"\b(?:vol(?:ume)?|v)\.?\s*(\d+(?:\.\d+)?)\b[\s\-–_:,.]*(?:ch(?:ap(?:ter)?)?|c|episode|ep)\.?\s*(\d+(?:\.\d+)?)\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex VolumeIssue = new(
        @"\b(?:vol(?:ume)?|v)\.?\s*(\d+(?:\.\d+)?)\b[\s\-–_:,.]*(?:#|no\.?\s*|issue\s*)\s*(\d+(?:\.\d+)?)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex VolumeThenBareNumber = new(
        @"\bv(\d{1,3})\s+(\d{1,4}(?:\.\d+)?)\s*$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex AnnualSpecial = new(
        @"\b(annual|special|one[\s-]?shot)\b(?:\s*(?:#\s*)?(\d+(?:\.\d+)?)\b|\s*$)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex Chapter = new(
        @"(?:\b(?:chapter|chap|ch|episode|ep)\.?\s*|\bc(?=\d{2,4}\b))(\d+(?:\.\d+)?)\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex Part = new(
        @"\b(?:part|pt)\.?\s*(\d+)(?:\s*[-–~]\s*\d+)?\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // Japanese story-arc words: first part, middle part, last part, finale.
    private static readonly Regex PartWord = new(
        @"\b(zenpen|chuuhen|chuhen|kouhen|kohen|kanketsuhen|kanketsu[\s-]?hen)\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex ExplicitIssue = new(
        @"(?:#|\bno\.\s*|\bissue\s*)\s*(\d+(?:\.\d+)?)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex VolumeOnly = new(
        @"\b(?:vol(?:ume)?|tpb|tome|book|bk|v)\.?\s*(\d+(?:\.\d+)?)\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex JapaneseNumber = new(@"第?\s*(\d+)\s*(巻|話)", RegexOptions.Compiled);

    private static readonly Regex NumberRange = new(
        @"^(.*?\S)\s+(\d{1,3})\s*[-–~]\s*\d{1,3}\s*$",
        RegexOptions.Compiled);

    private static readonly Regex RomanNumeral = new(
        @"^(.*?\S)\s+(II|III|IV|V|VI|VII|VIII|IX|X)\s*$",
        RegexOptions.Compiled);

    private static readonly Regex NumberThenSubtitle = new(
        @"^(.*?\S)\s+(\d{1,4}(?:\.\d+)?)\s+[-–:]\s+\S.*$",
        RegexOptions.Compiled);

    private static readonly Regex TrailingNumber = new(
        @"^(.*?\S)[\s\-–_]+(\d{1,4}(?:\.\d+)?)\s*$",
        RegexOptions.Compiled);

    private static readonly Regex Numbers = new(@"\d+(?:\.\d+)?", RegexOptions.Compiled);

    public static ComicIdentity Parse(string? title, string? filePath = null, ComicMetadataEntity? metadata = null)
    {
        string raw = StripKnownExtension(!string.IsNullOrWhiteSpace(title) ? title! : Path.GetFileName(filePath ?? string.Empty));
        if (string.IsNullOrWhiteSpace(raw) && !string.IsNullOrWhiteSpace(filePath))
        {
            raw = StripKnownExtension(Path.GetFileName(filePath));
        }

        raw = raw.Replace('_', ' ');
        raw = StripLabelPrefix(SourcePrefix.Replace(raw, string.Empty));
        var creators = ExtractCreators(raw);
        var titleCreators = creators.ToList();

        var (working, year) = CleanName(raw);
        var parsed = ParseNumbers(working);

        string seriesName = parsed.Series;
        if (string.IsNullOrWhiteSpace(seriesName))
        {
            seriesName = working;
        }

        // "Original Title - Translated Title": the first part names the series, the second is an alias.
        string altKey = string.Empty;
        var split = Regex.Match(seriesName, @"^(.{3,}?)\s+[-–]\s+(.{3,})$");
        if (split.Success)
        {
            seriesName = split.Groups[1].Value;
            altKey = MakeKey(TidySeriesName(StripTrailingNumber(split.Groups[2].Value)));
        }

        seriesName = TidySeriesName(seriesName);

        double? issue = parsed.Issue;
        double? volume = parsed.Volume;
        var kind = parsed.Kind;
        bool fromMetadata = false;

        if (metadata != null && !string.IsNullOrWhiteSpace(metadata.SeriesName))
        {
            seriesName = TidySeriesName(metadata.SeriesName!);
            fromMetadata = true;
        }

        if (metadata != null && TryParseNumber(metadata.IssueNumber, out double metaIssue))
        {
            issue = metaIssue;
            if (kind is ComicNumberKind.None or ComicNumberKind.Volume) kind = ComicNumberKind.Issue;
            fromMetadata = true;
        }

        if (metadata != null && !year.HasValue && !string.IsNullOrWhiteSpace(metadata.ReleaseDate))
        {
            var m = Regex.Match(metadata.ReleaseDate!, @"(19|20)\d{2}");
            if (m.Success) year = int.Parse(m.Value, CultureInfo.InvariantCulture);
        }

        if (metadata != null)
        {
            foreach (var person in SplitPeople(metadata.Writers).Concat(SplitPeople(metadata.Artists)))
            {
                if (!creators.Any(c => MakeKey(c) == MakeKey(person))) creators.Add(person);
            }
        }

        return new ComicIdentity
        {
            SeriesName = seriesName,
            SeriesKey = MakeKey(seriesName),
            AltTitleKey = altKey,
            Issue = issue,
            Volume = volume,
            Year = year,
            Kind = issue.HasValue || volume.HasValue ? (kind == ComicNumberKind.None ? ComicNumberKind.Issue : kind) : ComicNumberKind.None,
            Creators = creators,
            TitleCreators = titleCreators,
            CleanTitle = working,
            FromMetadata = fromMetadata
        };
    }

    public static ComicIdentity Parse(ComicEntity comic, ComicMetadataEntity? metadata = null) =>
        Parse(comic.Title, comic.FilePath, metadata);

    /// <summary>
    /// Creators from the conventional leading credit: "[Circle (Artist)] Title", "[Artist] Title",
    /// "(Event) [Circle] Title" or "(Artist) Title". Both circle and artist are returned when present.
    /// </summary>
    public static List<string> ExtractCreators(string name)
    {
        var result = new List<string>();
        if (string.IsNullOrWhiteSpace(name)) return result;
        string s = name.TrimStart();

        // Skip a leading event/convention tag when a bracketed credit follows: "(C95) [Circle] Title".
        var eventTag = Regex.Match(s, @"^\([^\)]*\)\s*(?=\[)");
        if (eventTag.Success) s = s[eventTag.Length..];

        string? credit = null;
        var bracket = Regex.Match(s, @"^\[([^\]]+)\]");
        if (bracket.Success)
        {
            credit = bracket.Groups[1].Value;
        }
        else
        {
            // "(Artist) Title": only when it is clearly a name (no digits) and a title follows.
            var paren = Regex.Match(s, @"^\(([^\)\d]{2,40})\)\s+\S");
            if (paren.Success) credit = paren.Groups[1].Value;
        }

        if (string.IsNullOrWhiteSpace(credit)) return result;

        var inner = Regex.Match(credit, @"^(.*?)\s*\((.+)\)\s*$");
        var names = new List<string>();
        if (inner.Success)
        {
            names.AddRange(SplitPeople(inner.Groups[2].Value)); // artist(s) first
            names.AddRange(SplitPeople(inner.Groups[1].Value)); // then the circle
        }
        else
        {
            names.AddRange(SplitPeople(credit));
        }

        foreach (var n in names)
        {
            string clean = Whitespace.Replace(n, " ").Trim().Trim('.', '-', ' ');
            if (clean.Length < 2 || TagLike.IsMatch(clean) || MakeKey(clean).Length < 2) continue;
            if (!result.Any(r => MakeKey(r) == MakeKey(clean))) result.Add(clean);
        }

        return result;
    }

    private static IEnumerable<string> SplitPeople(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return Array.Empty<string>();
        return Regex.Split(text, @"\s*(?:,|;|&|\band\b|/|、)\s*", RegexOptions.IgnoreCase)
            .Select(p => p.Trim())
            .Where(p => p.Length > 0);
    }

    /// <summary>Folder name that can act as a series name, or null for generic/root folders.</summary>
    public static string? GetMeaningfulFolderName(string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath)) return null;
        try
        {
            string? dir = Path.GetDirectoryName(filePath);
            if (string.IsNullOrEmpty(dir)) return null;
            string name = Path.GetFileName(dir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            if (string.IsNullOrWhiteSpace(name) || name.EndsWith(':') || GenericFolderNames.Contains(name.Trim())) return null;
            return name.Trim();
        }
        catch
        {
            return null;
        }
    }

    /// <summary>Lowercase letters/digits only; accents removed; "&amp;" = "and"; leading "the" dropped.</summary>
    public static string MakeKey(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;
        string s = RemoveDiacritics(text).ToLowerInvariant().Replace("&", " and ").Trim();
        if (s.StartsWith("the ", StringComparison.Ordinal)) s = s[4..];
        var sb = new StringBuilder(s.Length);
        foreach (char ch in s)
        {
            if (char.IsLetterOrDigit(ch)) sb.Append(ch);
        }
        return sb.ToString();
    }

    /// <summary>The cleaned title (release tags removed) reduced to a comparison key.</summary>
    public static string MakeTitleKey(string? title) => MakeKey(CleanName(StripLabelPrefix(SourcePrefix.Replace(StripKnownExtension(title ?? string.Empty).Replace('_', ' '), string.Empty))).Cleaned);

    /// <summary>
    /// Drops a leading category label ("original - [Circle] Title"). Kept when it could be the title itself:
    /// more than three words, or the bracket after the dash is a year or release tag ("Batman - [2019]").
    /// </summary>
    public static string StripLabelPrefix(string name)
    {
        if (string.IsNullOrEmpty(name)) return name ?? string.Empty;
        var m = LabelPrefix.Match(name);
        if (!m.Success) return name;
        string label = m.Groups["label"].Value.Trim();
        string credit = m.Groups["credit"].Value.Trim();
        if (label.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length > 3) return name;
        if (YearOnly.IsMatch(credit) || TagLike.IsMatch(credit)) return name;
        string rest = name[m.Length..];
        // Something must follow the credit, otherwise the label is the only title there is.
        string afterCredit = Regex.Replace(rest, @"^\s*\[[^\[\]]*\]", string.Empty).Trim();
        return afterCredit.Length >= 2 ? rest : name;
    }

    /// <summary>Numbers that appear in a cleaned title, used to stop "#1" matching "#2" in fuzzy comparisons.</summary>
    public static string NumberSignature(string cleanTitle)
    {
        var numbers = Numbers.Matches(cleanTitle)
            .Select(m => double.TryParse(m.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out double d) ? d : -1)
            .Where(d => d >= 0)
            .Select(d => d.ToString("0.###", CultureInfo.InvariantCulture));
        return string.Join(",", numbers);
    }

    public static string StripKnownExtension(string name)
    {
        if (string.IsNullOrEmpty(name)) return string.Empty;
        foreach (var ext in KnownExtensions)
        {
            if (name.EndsWith(ext, StringComparison.OrdinalIgnoreCase))
            {
                return name[..^ext.Length];
            }
        }
        return name;
    }

    private static (string Cleaned, int? Year) CleanName(string raw)
    {
        string s = raw.Replace('_', ' ');
        if (!s.Contains(' ') && s.Count(c => c == '.') >= 2)
        {
            s = DotsAsSpaces.Replace(s, " ");
        }

        int? year = null;
        for (int pass = 0; pass < 4; pass++)
        {
            string before = s;
            s = BracketGroup.Replace(s, m =>
            {
                string inner = m.Groups[1].Success ? m.Groups[1].Value : m.Groups[2].Success ? m.Groups[2].Value : m.Groups[3].Value;
                var y = YearOnly.Match(inner);
                if (y.Success && year == null)
                {
                    year = int.Parse(y.Groups[1].Value, CultureInfo.InvariantCulture);
                }
                return " ";
            });
            if (s == before) break;
        }
        s = Regex.Replace(s, @"[\[\](){}]", " ");

        s = EqualsTag.Replace(s, " ");
        s = PlusExtras.Replace(s, string.Empty);
        s = ReplaceCircledNumbers(s);

        s = Whitespace.Replace(s, " ").Trim().Trim('-', '–', '_', ',', ':', ' ');
        return (s, year);
    }

    private static string ReplaceCircledNumbers(string s)
    {
        var sb = new StringBuilder(s.Length + 4);
        foreach (char ch in s)
        {
            if (ch >= '①' && ch <= '⑳') sb.Append(' ').Append(ch - '①' + 1);
            else if (ch >= '❶' && ch <= '❿') sb.Append(' ').Append(ch - '❶' + 1);
            else sb.Append(ch);
        }
        return sb.ToString();
    }

    private readonly record struct NumberParse(string Series, double? Volume, double? Issue, ComicNumberKind Kind);

    private static NumberParse ParseNumbers(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return new NumberParse(string.Empty, null, null, ComicNumberKind.None);

        Match m = VolumeChapter.Match(s);
        if (m.Success && TryParseNumber(m.Groups[1].Value, out double vc) && TryParseNumber(m.Groups[2].Value, out double cc))
            return new NumberParse(s[..m.Index], vc, cc, ComicNumberKind.Chapter);

        m = VolumeIssue.Match(s);
        if (m.Success && TryParseNumber(m.Groups[1].Value, out double vi) && TryParseNumber(m.Groups[2].Value, out double ii))
            return new NumberParse(s[..m.Index], vi, ii, ComicNumberKind.Issue);

        m = VolumeThenBareNumber.Match(s);
        if (m.Success && TryParseNumber(m.Groups[1].Value, out double vb) && TryParseNumber(m.Groups[2].Value, out double ib))
            return new NumberParse(s[..m.Index], vb, ib, ComicNumberKind.Issue);

        m = AnnualSpecial.Match(s);
        if (m.Success && m.Index > 0)
        {
            double number = TryParseNumber(m.Groups[2].Value, out double an) ? an : 1;
            var kind = m.Groups[1].Value.StartsWith("annual", StringComparison.OrdinalIgnoreCase) ? ComicNumberKind.Annual : ComicNumberKind.Special;
            var vol = VolumeOnly.Match(s[..m.Index]);
            double? volume = vol.Success && TryParseNumber(vol.Groups[1].Value, out double av) ? av : null;
            string series = vol.Success ? s[..vol.Index] : s[..m.Index];
            return new NumberParse(series, volume, number, kind);
        }

        m = Chapter.Match(s);
        if (m.Success && m.Index > 0 && TryParseNumber(m.Groups[1].Value, out double ch))
            return new NumberParse(s[..m.Index], null, ch, ComicNumberKind.Chapter);

        m = Part.Match(s);
        if (m.Success && m.Index > 0 && TryParseNumber(m.Groups[1].Value, out double pt))
            return new NumberParse(s[..m.Index], null, pt, ComicNumberKind.Part);

        m = PartWord.Match(s);
        if (m.Success && m.Index > 0)
        {
            string word = m.Groups[1].Value.ToLowerInvariant().Replace(" ", string.Empty).Replace("-", string.Empty);
            double position = word switch
            {
                "zenpen" => 1,
                "chuuhen" or "chuhen" => 1.5,
                "kouhen" or "kohen" => 2,
                _ => 3
            };
            return new NumberParse(s[..m.Index], null, position, ComicNumberKind.Part);
        }

        m = ExplicitIssue.Match(s);
        if (m.Success && m.Index > 0 && TryParseNumber(m.Groups[1].Value, out double ei))
        {
            var vol = VolumeOnly.Match(s[..m.Index]);
            double? volume = vol.Success && TryParseNumber(vol.Groups[1].Value, out double evo) ? evo : null;
            string series = vol.Success ? s[..vol.Index] : s[..m.Index];
            return new NumberParse(series, volume, ei, ComicNumberKind.Issue);
        }

        m = JapaneseNumber.Match(s);
        if (m.Success && m.Index > 0 && TryParseNumber(m.Groups[1].Value, out double jn))
        {
            return m.Groups[2].Value == "巻"
                ? new NumberParse(s[..m.Index], jn, null, ComicNumberKind.Volume)
                : new NumberParse(s[..m.Index], null, jn, ComicNumberKind.Chapter);
        }

        m = VolumeOnly.Match(s);
        if (m.Success && m.Index > 0 && TryParseNumber(m.Groups[1].Value, out double vo))
        {
            string after = s[(m.Index + m.Length)..].Trim(' ', '-', '–', ':');
            var bare = Regex.Match(after, @"^(\d{1,4}(?:\.\d+)?)\b");
            if (bare.Success && TryParseNumber(bare.Groups[1].Value, out double vbi))
                return new NumberParse(s[..m.Index], vo, vbi, ComicNumberKind.Issue);
            return new NumberParse(s[..m.Index], vo, null, ComicNumberKind.Volume);
        }

        m = NumberRange.Match(s);
        if (m.Success && TryParseNumber(m.Groups[2].Value, out double rg))
            return new NumberParse(m.Groups[1].Value, null, rg, ComicNumberKind.Issue);

        m = NumberThenSubtitle.Match(s);
        if (m.Success && TryParseNumber(m.Groups[2].Value, out double ns))
            return new NumberParse(m.Groups[1].Value, null, ns, ComicNumberKind.Issue);

        m = TrailingNumber.Match(s);
        if (m.Success && TryParseNumber(m.Groups[2].Value, out double tn))
            return new NumberParse(m.Groups[1].Value, null, tn, ComicNumberKind.Issue);

        m = RomanNumeral.Match(s);
        if (m.Success)
        {
            int roman = m.Groups[2].Value switch
            {
                "II" => 2, "III" => 3, "IV" => 4, "V" => 5, "VI" => 6, "VII" => 7, "VIII" => 8, "IX" => 9, _ => 10
            };
            return new NumberParse(m.Groups[1].Value, null, roman, ComicNumberKind.Issue);
        }

        return new NumberParse(s, null, null, ComicNumberKind.None);
    }

    private static string StripTrailingNumber(string s)
    {
        var parsed = ParseNumbers(s);
        return string.IsNullOrWhiteSpace(parsed.Series) ? s : parsed.Series;
    }

    private static string TidySeriesName(string series)
    {
        string s = Whitespace.Replace(series, " ").Trim();
        s = s.Trim('-', '–', '_', '.', ',', ':', '#', ' ', '…');
        s = Regex.Replace(s, @"\s+(?:vol(?:ume)?|v|no|issue|ch(?:apter)?|part|pt)\.?$", string.Empty, RegexOptions.IgnoreCase).Trim();
        return s;
    }

    private static bool TryParseNumber(string? text, out double value)
    {
        value = 0;
        if (string.IsNullOrWhiteSpace(text)) return false;
        var m = Numbers.Match(text);
        return m.Success && double.TryParse(m.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }

    private static string RemoveDiacritics(string text)
    {
        string normalized = text.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(normalized.Length);
        foreach (char c in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark) sb.Append(c);
        }
        return sb.ToString().Normalize(NormalizationForm.FormC);
    }
}
