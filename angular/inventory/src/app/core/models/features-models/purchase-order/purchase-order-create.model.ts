import { PurchaseOrderDetailCreateModel } from "./purchase-order-detail-create.model";
import { PurchaseOrderDetailModel } from "./purchase-order-detail.model";

export class PurchaseOrderCreateModel {

    supplierId: string = '';

    expectedReceiveDate: Date = new Date();
    // Kho nhận hàng
    receivingWarehouseId: string = '';

    freight: number | null = null;
    type: string | null = null;
    totalWeight: number | null = null;
    products: PurchaseOrderDetailCreateModel[] = [];
}