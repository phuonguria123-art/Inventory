import { TransferProductModel } from "./transfer-product.model";

export class TransferInventoryModel {
    warehouseFromId!: string;
    warehouseToId!: string;
    reference?: string;
    notes?: string;
    products!: TransferProductModel[]
}