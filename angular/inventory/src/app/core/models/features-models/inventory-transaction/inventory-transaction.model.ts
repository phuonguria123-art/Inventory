import { InventoryTransactionType } from "../../../enum/inventory-transaction-type.enum";

export class InventoryTransactionModel {
    id!: string;

    warehouseId!: string;
    productId!: string;
    inventoryItemId?: string | null;

    transactionType!: InventoryTransactionType;
    quantity!: number;
    reference!: string;
    notes?: string | null;
    transactionDate!: Date;
    createdByUserId!: string;
}