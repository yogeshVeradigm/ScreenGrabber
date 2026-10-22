namespace DevTestEvidenceCapture.Core.Session;

/// <summary>
/// The most recently used ticket numbers. A "ticket number" can be any identifier the user works
/// with: a JIRA key, an Azure DevOps work item, a ServiceNow number or just a task name.
/// </summary>
public static class RecentTicketNumbers
{
    public const int Max = 10;

    /// <summary>Returns <paramref name="recent"/> with <paramref name="ticketNumber"/> moved to the front.</summary>
    public static List<string> Add(IEnumerable<string> recent, string ticketNumber)
    {
        var trimmed = ticketNumber.Trim();
        return new[] { trimmed }
            .Concat(recent.Where(t => !string.Equals(t, trimmed, StringComparison.OrdinalIgnoreCase)))
            .Take(Max)
            .ToList();
    }
}
