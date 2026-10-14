import { Component, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { forkJoin } from 'rxjs';
import { InventoryModel } from '../../core/models/features-models/inventory/inventory.model';
import { NotificationService } from '../../core/services/notification.service';
import { InventoryService } from '../inventory/service/inventory.service';

@Component({
  imports: [RouterLink],
  selector: 'app-dashboard',
  styleUrl: './dashboard.scss',
  templateUrl: './dashboard.html',
})
export class Dashboard {
  private readonly inventoryService = inject(InventoryService);
  private readonly notification = inject(NotificationService);
  readonly lowStockItems = signal<InventoryModel[]>([]);
  readonly excessStockItems = signal<InventoryModel[]>([]);
  readonly isLoading = signal(false);
  readonly totalWarnings = computed(() => this.lowStockItems().length + this.excessStockItems().length);
  readonly lowStockShortage = computed(() => this.lowStockItems().reduce(
    (total, item) => total + Math.max(item.minStock - item.availableQuantity, 0), 0));
  readonly excessQuantity = computed(() => this.excessStockItems().reduce(
    (total, item) => total + Math.max(item.availableQuantity - item.maxStock, 0), 0));

  ngOnInit(): void {
    this.loadStockWarnings();
  }

  loadStockWarnings(): void {
    this.isLoading.set(true);
    forkJoin({
      lowStock: this.inventoryService.getLowStock(),
      excessStock: this.inventoryService.getExcessStock(),
    }).subscribe({
      next: ({ lowStock, excessStock }) => {
        this.lowStockItems.set(lowStock);
        this.excessStockItems.set(excessStock);
        this.isLoading.set(false);
      },
      error: () => {
        this.isLoading.set(false);
        this.notification.error('Không thể tải dữ liệu cảnh báo tồn kho.');
      }
    });
  }
}
