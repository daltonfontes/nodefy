namespace Nodefy.Api.Data.Entities;

public class Card
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public double Position { get; set; } = 0.5;
    public DateTimeOffset StageEnteredAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
