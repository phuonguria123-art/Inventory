import { AdjustProductModel } from "./adjust-product.model";

export class AdjustInventoryModel {
    warehouseId!: string;
    reference?: string;
    notes!: string;
    products!: AdjustProductModel[]
}