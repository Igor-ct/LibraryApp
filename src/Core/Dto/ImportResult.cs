namespace Core.Dto;

public sealed record ImportResult<T>(
    IReadOnlyList<T> Items,
    IReadOnlyList<string> Errors)
{
    public int Total => Items.Count + Errors.Count;
    public double ErrorPercentage => Total == 0 ? 0 : (double)Errors.Count / Total * 100;
    public string Statistics =>
        $"Статистика: усього {Total} | прийнято {Items.Count} | пропущено {Errors.Count} | {ErrorPercentage:F1}% помилок";
}