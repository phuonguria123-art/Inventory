export class InventoryItemModel {
    id!: string;
    inventoryId!: string;
    quantity!: number;
    unitCost!: number;
    batchNumber: string = '';
    manufactureDate?: string | null;
    expiryDate?: string | null;
    dateReceived!: string;
    location: string = '';
}