import { PurchaseOrderDetailUpdateModel } from "./purchase-order-detail-update.model";

export class PurchaseOrderUpdateModel {
    supplierId: string = '';
    expectedReceiveDate: Date = new Date();
    // Kho nhận hàng
    receivingWarehouseId: string = '';

    freight: number | null = null;
    type: string | null = null;
    totalWeight: number | null = null;
    orderDetails: PurchaseOrderDetailUpdateModel[] = [];
}
