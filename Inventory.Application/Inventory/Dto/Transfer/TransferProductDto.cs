namespace Inventory.Application.Inventory.Dto.Transfer;

public sealed class TransferProductDto
{
    public Guid ProductId { get; set; }
    public List<TransferLotDto> Lots { get; set; } = [];
}
