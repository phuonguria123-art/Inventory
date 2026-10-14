export class PurchaseOrderDetailModel {
    id: string = '';
    purchaseOrderId: string = '';
    productId: string = '';

    orderedQuantity: number = 0;
    actualReceivedQuantity: number = 0;
    type: string | null = null;

    receiveTime: Date | null = null;
    inventoryId: string | null = null;
    unitPrice: number = 0;
    note: string = '';
}