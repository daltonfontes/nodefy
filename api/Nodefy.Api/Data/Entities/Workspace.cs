namespace Nodefy.Api.Data.Entities;

public class Workspace
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string Slug { get; set; } = "";
    public string Currency { get; set; } = "BRL";          // D-03
    public bool CurrencyLocked { get; set; } = false;       // D-04
    public DateTimeOffset CreatedAt { get; set; }
}
