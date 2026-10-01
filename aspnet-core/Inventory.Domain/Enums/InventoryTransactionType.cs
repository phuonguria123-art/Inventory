namespace Inventory.Domain.Enums;

public enum InventoryTransactionType
{

    Receipt = 1,//Nhập kho
    Issue = 2, //Xuất kho
    AdjustmentIncrease = 3, //Điều chỉnh tăng
    AdjustmentDecrease = 4, //Điều chỉnh giảm
    TransferIn = 5, //Nhận chuyển kho
    TransferOut = 6, //Chuyển kho đi
    Return = 7, //Trả lại
    Damage = 8 //Hư hỏng
}
