namespace Core.Reports;

public sealed record LendingReportLine(string BookId, string Title, string ReaderName, int DaysBorrowed);