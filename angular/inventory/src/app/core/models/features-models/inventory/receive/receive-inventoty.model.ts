import { ReceiveProductModel } from "./receive-product.model";

export class ReceiveInventoryModel {
    warehouseId!: string;
    purchaseOrderId?: string | null;
    reference?: string | null;
    notes?: string | null;
    products: ReceiveProductModel[] = [];
}