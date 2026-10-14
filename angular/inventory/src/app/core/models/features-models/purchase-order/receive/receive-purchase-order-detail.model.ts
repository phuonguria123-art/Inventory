import { ReceivePurchaseOrderLotModel } from "./receive-purchase-order-lot.model";

export class ReceivePurchaseOrderDetailModel {
    purchaseOrderDetailId!: string;
    lots!: ReceivePurchaseOrderLotModel[];
}