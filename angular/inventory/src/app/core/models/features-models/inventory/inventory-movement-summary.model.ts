export interface InventoryMovementByMonthModel {
  year: number;
  month: number;
  totalStockIn: number;
  totalStockOut: number;
}

export interface InventoryMovementSummaryModel {
  productId: string;
  fromDate: string;
  toDate: string;
  items: InventoryMovementByMonthModel[];
}
