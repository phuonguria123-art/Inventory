import { DatePipe, NgClass } from '@angular/common';
import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { InventoryTransactionType } from '../../../core/enum/inventory-transaction-type.enum';
import { InventoryTransactionModel } from '../../../core/models/features-models/inventory-transaction/inventory-transaction.model';
import { ProductModel } from '../../../core/models/features-models/product/product.model';
import { WarehouseModel } from '../../../core/models/features-models/warehouse/warehouse.model';
import { NotificationService } from '../../../core/services/notification.service';
import { ProductService } from '../../product/service/product.service';
import { WarehouseService } from '../../warehouse/service/warehouse.service';
import { InventoryService } from '../service/inventory.service';

@Component({ imports: [FormsModule, RouterLink, DatePipe, NgClass], selector: 'app-inventory-transaction', styleUrl: './inventory-transaction.scss', templateUrl: './inventory-transaction.html' })
export class InventoryTransaction implements OnInit {
  private readonly inventoryService = inject(InventoryService);
  private readonly warehouseService = inject(WarehouseService);
  private readonly productService = inject(ProductService);
  private readonly notification = inject(NotificationService);

  warehouseId = '';
  productId = '';
  transactionType = '';
  reference = '';
  readonly warehouses = signal<WarehouseModel[]>([]);
  readonly products = signal<ProductModel[]>([]);
  readonly pageSize = signal(10);
  readonly pageNumber = signal(1);
  readonly inventoryTransactions = signal<InventoryTransactionModel[]>([]);
  readonly totalCount = signal(0);
  readonly totalPages = signal(0);
  readonly isLoading = signal(false);
  readonly transactionTypes = [
    { value: InventoryTransactionType.Receipt, label: 'Nhập kho' },
    { value: InventoryTransactionType.Issue, label: 'Xuất kho' },
    { value: InventoryTransactionType.AdjustmentIncrease, label: 'Điều chỉnh tăng' },
    { value: InventoryTransactionType.AdjustmentDecrease, label: 'Điều chỉnh giảm' },
    { value: InventoryTransactionType.TransferIn, label: 'Chuyển vào' },
    { value: InventoryTransactionType.TransferOut, label: 'Chuyển đi' },
    { value: InventoryTransactionType.Return, label: 'Trả hàng' },
    { value: InventoryTransactionType.Damage, label: 'Hư hỏng' }
  ];

  ngOnInit(): void { this.loadOptions(); this.loadTransactions(); }
  loadTransactions(): void {
    this.isLoading.set(true);
    const type = this.transactionType === '' ? undefined : Number(this.transactionType) as InventoryTransactionType;
    this.inventoryService.getTransactions(this.pageNumber(), this.pageSize(), this.warehouseId || undefined,
      this.productId || undefined, type, this.reference.trim() || undefined).subscribe({
        next: result => { this.inventoryTransactions.set(result.items); this.totalCount.set(result.totalCount); this.totalPages.set(result.totalPages); this.isLoading.set(false); },
        error: () => { this.isLoading.set(false); this.notification.error('Không thể tải lịch sử giao dịch kho.'); }
      });
  }
  applyFilters(): void { this.pageNumber.set(1); this.loadTransactions(); }
  clearFilters(): void { this.warehouseId = ''; this.productId = ''; this.transactionType = ''; this.reference = ''; this.applyFilters(); }
  changePage(page: number): void { if (page < 1 || page > this.totalPages() || page === this.pageNumber()) return; this.pageNumber.set(page); this.loadTransactions(); }
  getWarehouseName(id: string): string { const item = this.warehouses().find(x => x.id === id); return item ? `${item.code} - ${item.name}` : id; }
  getProductName(id: string): string { const item = this.products().find(x => x.id === id); return item ? `${item.code} - ${item.name}` : id; }
  getTypeLabel(type: InventoryTransactionType): string { return this.transactionTypes.find(x => x.value === type)?.label ?? 'Không xác định'; }
  getTypeClass(type: InventoryTransactionType): string { return `type-${type}`; }
  private loadOptions(): void {
    this.warehouseService.getList().subscribe({ next: data => this.warehouses.set(data) });
    this.productService.getProducts(1, 100).subscribe({ next: data => this.products.set(data.items) });
  }
}
