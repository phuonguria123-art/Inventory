import { Component, inject, signal } from '@angular/core';
import { WarehouseService } from './service/warehouse.service';
import { WarehouseModel } from '../../core/models/features-models/warehouse/warehouse.model';
import { RouterLink } from '@angular/router';
import { NotificationService } from '../../core/services/notification.service';
import { getApiErrorMessage } from '../../core/helpers/api-error.helper';
import { finalize } from 'rxjs';

@Component({
  imports: [RouterLink],
  selector: 'app-warehouse',
  styleUrl: './warehouse.scss',
  templateUrl: './warehouse.html',
})
export class Warehouse {
  private readonly warehouseService = inject(WarehouseService);
  readonly warehouses = signal<WarehouseModel[]>([]);
  private readonly notification = inject(NotificationService);
  readonly isLoading = signal(false);
  readonly deletingId = signal<string | null>(null);

  ngOnInit(): void {
    this.loadWarehouse();
  }

  loadWarehouse(): void {
    this.warehouseService.getList()
      .pipe(finalize(() => this.isLoading.set(false)))
      .subscribe({
        next: (res) => {
          this.warehouses.set(res);
        },
        error: (err) => {
          this.notification.error(
            getApiErrorMessage(err, 'Không thể tả danh sách kho')
          )
        }
      });
  }

  onDelete(warehouse: WarehouseModel): void {
    const confirmed = window.confirm(
      `Bạn có chắc muốn xóa kho "${warehouse.name}"`
    );
    if (!confirmed) {
      return;
    }
    this.deletingId.set(warehouse.id);

    this.warehouseService.deleteWarehouse(warehouse.id)
      .pipe(finalize(() => this.deletingId.set(null)))
      .subscribe({
        next: () => {
          this.notification.success('Xóa kho thành công');
          this.loadWarehouse();
        },
        error: (err) => {
          this.notification.error(getApiErrorMessage(err, 'Không thể tải danh sách kho'))
        }
      });
  }
}
