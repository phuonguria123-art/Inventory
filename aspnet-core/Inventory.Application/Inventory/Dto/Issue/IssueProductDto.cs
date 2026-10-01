namespace Inventory.Application.Inventory.Dto.Issue;

public sealed class IssueProductDto
{
    public Guid ProductId { get; set; }
    public List<IssueLotDto> Lots { get; set; } = [];
}
