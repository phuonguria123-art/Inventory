export class PurchaseOrderDetailCreateModel {
    productId: string = '';
    orderedQuantity: number = 0;
    type: string | null = null;
    unitPrice: number = 0;
    note: string = '';
}