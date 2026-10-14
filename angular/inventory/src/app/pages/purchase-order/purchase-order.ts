import { CurrencyPipe, DatePipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { PurchaseOrderService } from './service/purchase-order.service';
import { PurchaseOrderModel } from '../../core/models/features-models/purchase-order/purchase-order.model';
import { PurchaseOrderStatus } from '../../core/enum/purchase-order-status.enum';
import { NotificationService } from '../../core/services/notification.service';
import { RouterLink } from '@angular/router';
import { SupplierModel } from '../../core/models/features-models/supplier/supplier.model';
import { WarehouseModel } from '../../core/models/features-models/warehouse/warehouse.model';
import { SupplierService } from '../supplier/service/supplier.service';
import { WarehouseService } from '../warehouse/service/warehouse.service';

@Component({
  imports: [RouterLink, FormsModule, DatePipe, CurrencyPipe],
  selector: 'app-purchase-order',
  styleUrl: './purchase-order.scss',
  templateUrl: './purchase-order.html',
})
export class PurchaseOrder {
  private readonly purchaseOrderService = inject(PurchaseOrderService);
  private readonly notification = inject(NotificationService);
  private readonly supplierService = inject(SupplierService);
  private readonly warehouseService = inject(WarehouseService);
  readonly purchaseOrders = signal<PurchaseOrderModel[]>([]);
  readonly pageNumber = signal(1);
  readonly pageSize = signal(10);
  readonly totalCount = signal(0);
  readonly totalPages = signal(0);
  readonly suppliers = signal<SupplierModel[]>([]);
  readonly warehouses = signal<WarehouseModel[]>([]);
  status = '';
  supplierId = '';
  warehouseId = '';
  createDate = '';
  readonly isLoading = signal(false);
  readonly statuses = [
    { value: PurchaseOrderStatus.Draft, label: 'Nháp' },
    { value: PurchaseOrderStatus.Sent, label: 'Đã gửi' },
    { value: PurchaseOrderStatus.Confirmed, label: 'Đã xác nhận' },
    { value: PurchaseOrderStatus.Shipping, label: 'Đang giao' },
    { value: PurchaseOrderStatus.PartiallyReceived, label: 'Nhận một phần' },
    { value: PurchaseOrderStatus.Received, label: 'Đã nhận' },
    { value: PurchaseOrderStatus.Cancelled, label: 'Đã hủy' }
  ];

  ngOnInit() {
    this.loadOptions();
    this.getListOrder();
  }
  getListOrder() {
    this.isLoading.set(true);
    const status = this.status === '' ? undefined : Number(this.status) as PurchaseOrderStatus;
    const date = this.createDate ? new Date(this.createDate) : undefined;
    this.purchaseOrderService.getList(this.pageNumber(), this.pageSize(), status, this.supplierId || undefined, this.warehouseId || undefined, date).subscribe({
      next: (result) => {
        this.purchaseOrders.set(result.items);
        this.totalCount.set(result.totalCount);
        this.totalPages.set(result.totalPages);
        this.isLoading.set(false);
      },
      error: () => {
        this.isLoading.set(false);
        this.notification.error('Không thể tải danh sách đơn mua hàng');
      }
    })

  }
  changePage(page: number): void {
    if (page < 1 || page > this.totalPages() || page === this.pageNumber()) return;
    this.pageNumber.set(page);
    this.getListOrder();
  }
  applyFilters(): void { this.pageNumber.set(1); this.getListOrder(); }
  clearFilters(): void { this.status = ''; this.supplierId = ''; this.warehouseId = ''; this.createDate = ''; this.applyFilters(); }
  getStatusLabel(status: PurchaseOrderStatus): string { return this.statuses.find(item => item.value === status)?.label ?? 'Không xác định'; }
  getSupplierName(id: string): string { const item = this.suppliers().find(x => x.id === id); return item ? `${item.code} - ${item.name}` : id; }
  getWarehouseName(id: string): string { const item = this.warehouses().find(x => x.id === id); return item ? `${item.code} - ${item.name}` : id; }
  private loadOptions(): void {
    this.supplierService.getList().subscribe({ next: data => this.suppliers.set(data) });
    this.warehouseService.getList().subscribe({ next: data => this.warehouses.set(data) });
  }

}
