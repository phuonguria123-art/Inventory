export interface InventoryModel {
    id: string;
    warehouseId: string;
    productId: string;
    minStock: number;
    maxStock: number;
    quantityOnHand: number;
    reservedQuantity: number;
    availableQuantity: number;
    lastUpdate: string;
    warehouseCode: string;
    warehouseName: string;
    productCode: string;
    productName: string;
}