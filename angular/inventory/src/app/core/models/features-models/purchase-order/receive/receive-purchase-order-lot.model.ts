export class ReceivePurchaseOrderLotModel {
    batchNumber: string = '';
    receivedQuantity: number = 0;
    unitCost: number = 0;
    manufactureDate: Date | null = null;
    expiryDate: Date | null = null;
    location: string | null = null;
}