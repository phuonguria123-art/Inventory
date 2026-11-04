export interface InventoryModel {
    id: string;
    warehouseId: string;
    productId: string;
    minStock: number | null;
    maxStock: number | null;
    quantityOnHand: number;
    reservedQuantity: number;
    availableQuantity: number;
    lastUpdate: string;
    warehouseCode: string;
    warehouseName: string;
    productCode: string;
    productName: string;
}

export interface UpdateStockLevelsModel {
    minStock: number;
    maxStock: number;
}
