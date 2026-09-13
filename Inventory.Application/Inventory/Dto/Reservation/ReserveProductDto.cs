namespace Inventory.Application.Inventory.Dto.Reservation;

public sealed class ReserveProductDto
{
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
}
