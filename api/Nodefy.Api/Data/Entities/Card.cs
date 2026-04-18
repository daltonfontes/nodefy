namespace Nodefy.Api.Data.Entities;

public class Card
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public double Position { get; set; } = 0.5;
    public DateTimeOffset StageEnteredAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    public decimal? MonetaryValue { get; set; }
    public Guid PipelineId { get; set; }
    public Guid StageId { get; set; }
    public Guid? AssigneeId { get; set; }
    public DateTimeOffset? CloseDate { get; set; }
    public DateTimeOffset? ArchivedAt { get; set; }
}
