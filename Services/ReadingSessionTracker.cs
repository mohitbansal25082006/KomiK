using System;
using System.Collections.Generic;

namespace Komik.Services;

/// <summary>
/// Tracks one reading session: active reading time (idle gaps are capped so a comic left open overnight
/// does not count as hours of reading) and the distinct pages actually viewed.
/// </summary>
public sealed class ReadingSessionTracker
{
    /// <summary>Longest gap between two interactions that still counts as reading time.</summary>
    public static readonly TimeSpan IdleCap = TimeSpan.FromMinutes(3);

    /// <summary>Time on the opening page before it counts as read even without turning the page.</summary>
    public static readonly TimeSpan InitialPageDwell = TimeSpan.FromSeconds(15);

    private readonly Func<DateTime> _utcNow;
    private readonly HashSet<int> _viewedPages = new();
    private DateTime _lastActivityUtc;
    private double _activeSeconds;
    private int _initialPage = -1;

    public ReadingSessionTracker(Func<DateTime>? utcNow = null)
    {
        _utcNow = utcNow ?? (() => DateTime.UtcNow);
    }

    public bool IsActive { get; private set; }
    public long ComicId { get; private set; }
    public long? SessionId { get; set; }
    public DateTime StartUtc { get; private set; }

    public void Start(long comicId, int initialPage)
    {
        IsActive = true;
        ComicId = comicId;
        SessionId = null;
        StartUtc = _utcNow();
        _lastActivityUtc = StartUtc;
        _activeSeconds = 0;
        _viewedPages.Clear();
        _initialPage = initialPage;
        if (initialPage >= 0) _viewedPages.Add(initialPage);
    }

    /// <summary>Counts time since the previous interaction (capped at <see cref="IdleCap"/>).</summary>
    public void RecordActivity()
    {
        if (!IsActive) return;
        DateTime now = _utcNow();
        TimeSpan gap = now - _lastActivityUtc;
        if (gap > TimeSpan.Zero)
        {
            _activeSeconds += Math.Min(gap.TotalSeconds, IdleCap.TotalSeconds);
        }
        _lastActivityUtc = now;
    }

    public void RecordPageView(int pageIndex)
    {
        if (!IsActive || pageIndex < 0) return;
        RecordActivity();
        _viewedPages.Add(pageIndex);
    }

    /// <summary>Active seconds including the (capped) time since the last interaction.</summary>
    public int ActiveSeconds
    {
        get
        {
            if (!IsActive) return (int)Math.Round(_activeSeconds);
            TimeSpan pending = _utcNow() - _lastActivityUtc;
            double pendingSeconds = pending > TimeSpan.Zero ? Math.Min(pending.TotalSeconds, IdleCap.TotalSeconds) : 0;
            return (int)Math.Round(_activeSeconds + pendingSeconds);
        }
    }

    /// <summary>Distinct pages viewed. The page the comic opened on only counts once the reader moved on or lingered.</summary>
    public int PagesRead
    {
        get
        {
            int count = _viewedPages.Count;
            bool onlyInitial = count == 1 && _viewedPages.Contains(_initialPage);
            if (onlyInitial && ActiveSeconds < InitialPageDwell.TotalSeconds) return 0;
            return count;
        }
    }

    public bool HasMeaningfulData => IsActive && (ActiveSeconds >= 5 || PagesRead > 0);

    public DateTime EndUtc => _utcNow();

    public void Stop()
    {
        IsActive = false;
        SessionId = null;
        _viewedPages.Clear();
        _activeSeconds = 0;
        _initialPage = -1;
    }
}

/// <summary>
/// App-wide notification that reading statistics changed (a session was saved), so open stats views can refresh live.
/// </summary>
public static class ReadingStatsNotifier
{
    public static event Action? StatsChanged;

    public static void NotifyChanged() => StatsChanged?.Invoke();
}
