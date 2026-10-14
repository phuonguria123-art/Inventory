import { ReserveProductModel } from "./reserve-product.model";

export class ReservationInventoryModel {
    warehouseId!: string;
    reference!: string;
    expiresAt?: string | null;
    isCancel!: boolean;
    products!: ReserveProductModel[];
}
