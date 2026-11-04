import { Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { InventoryModel } from '../../core/models/features-models/inventory/inventory.model';
import { NotificationService } from '../../core/services/notification.service';
import { InventoryService } from './service/inventory.service';
import { getApiErrorMessage } from '../../core/helpers/api-error.helper';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { PermissionService } from '../../core/services/permission.service';

@Component({
  imports: [RouterLink, ReactiveFormsModule],
  selector: 'app-inventory',
  styleUrl: './inventory.scss',
  templateUrl: './inventory.html',
})
export class Inventory {
  private readonly inventoryService = inject(InventoryService);
  private readonly notification = inject(NotificationService);
  private readonly permissionService = inject(PermissionService);

  readonly inventories = signal<InventoryModel[]>([]);
  readonly pageNumber = signal(1);
  readonly pageSize = signal(10);
  readonly totalCount = signal(0);
  readonly totalPages = signal(0);
  readonly isLoading = signal(false);
  readonly isSavingStockLevels = signal(false);
  readonly selectedInventory = signal<InventoryModel | null>(null);

  readonly stockLevelsForm = new FormGroup({
    minStock: new FormControl<number | null>(null, {
      validators: [Validators.required, Validators.min(0)],
    }),
    maxStock: new FormControl<number | null>(null, {
      validators: [Validators.required, Validators.min(1)],
    }),
  });

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

  canConfigureStockLevels(): boolean {
    return this.permissionService.getGrantedPolicy('inventory.adjust');
  }

  openStockLevelsForm(inventory: InventoryModel): void {
    this.selectedInventory.set(inventory);
    this.stockLevelsForm.reset({
      minStock: inventory.minStock,
      maxStock: inventory.maxStock,
    });
  }

  closeStockLevelsForm(): void {
    if (this.isSavingStockLevels()) return;
    this.selectedInventory.set(null);
    this.stockLevelsForm.reset();
  }

  saveStockLevels(): void {
    const inventory = this.selectedInventory();
    if (!inventory) return;

    this.stockLevelsForm.markAllAsTouched();
    if (this.stockLevelsForm.invalid) return;

    const { minStock, maxStock } = this.stockLevelsForm.getRawValue();
    if (minStock === null || maxStock === null) return;
    if (maxStock <= minStock) {
      this.stockLevelsForm.controls.maxStock.setErrors({ notGreaterThanMin: true });
      return;
    }

    this.isSavingStockLevels.set(true);
    this.inventoryService.updateStockLevels(inventory.id, { minStock, maxStock }).subscribe({
      next: () => {
        this.isSavingStockLevels.set(false);
        this.selectedInventory.set(null);
        this.notification.success('Đã cập nhật mức tồn tối thiểu và tối đa.');
        this.getListInventory();
      },
      error: (err) => {
        this.isSavingStockLevels.set(false);
        this.notification.error(getApiErrorMessage(err, 'Không thể cập nhật mức tồn kho.'));
      },
    });
  }
}
