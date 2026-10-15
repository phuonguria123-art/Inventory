import { Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { InventoryModel } from '../../core/models/features-models/inventory/inventory.model';
import { NotificationService } from '../../core/services/notification.service';
import { InventoryService } from './service/inventory.service';
import { getApiErrorMessage } from '../../core/helpers/api-error.helper';

@Component({
  imports: [RouterLink],
  selector: 'app-inventory',
  styleUrl: './inventory.scss',
  templateUrl: './inventory.html',
})
export class Inventory {
  private readonly inventoryService = inject(InventoryService);
  private readonly notification = inject(NotificationService);

  readonly inventories = signal<InventoryModel[]>([]);
  readonly pageNumber = signal(1);
  readonly pageSize = signal(10);
  readonly totalCount = signal(0);
  readonly totalPages = signal(0);
  readonly isLoading = signal(false);

  warehouseId?: string;
  productId?: string;
  warehouseSearch?: string;
  productSearch?: string;
  sortBy?: string;
  sortDescending = true;

  ngOnInit(): void {
    this.getListInventory();
  }

  getListInventory(): void {
    this.isLoading.set(true);
    this.inventoryService.getList(
      this.pageNumber(), this.pageSize(), this.warehouseId, this.productId,
      this.warehouseSearch, this.productSearch, this.sortBy, this.sortDescending,
    ).subscribe({
      next: (result) => {
        this.inventories.set(result.items);
        this.totalCount.set(result.totalCount);
        this.totalPages.set(result.totalPages);
        this.isLoading.set(false);
      },
      error: (err) => {
        this.isLoading.set(false);
        this.notification.error(getApiErrorMessage(err, 'Không thể tải danh sách tồn kho'));
      },
    });
  }

  changePage(page: number): void {
    if (page < 1 || page > this.totalPages() || page === this.pageNumber()) return;
    this.pageNumber.set(page);
    this.getListInventory();
  }
}
