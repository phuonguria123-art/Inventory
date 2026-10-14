import { ReceivePurchaseOrderDetailModel } from "./receive-purchase-order-detail.model";

export class ReceivePurchaseOrderModel {
    purchaseOrderId!: string;
    orderDetails!: ReceivePurchaseOrderDetailModel[]
}