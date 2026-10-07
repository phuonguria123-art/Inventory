import { Component, inject, signal } from '@angular/core';
import { WarehouseService } from './service/warehouse.service';
import { WarehouseModel } from '../../core/models/features-models/warehouse/warehouse.model';
import { RouterLink } from '@angular/router';

@Component({
  imports: [RouterLink],
  selector: 'app-warehouse',
  styleUrl: './warehouse.scss',
  templateUrl: './warehouse.html',
})
export class Warehouse {
  private readonly warehouseService = inject(WarehouseService);
  readonly warehousesModel = signal<WarehouseModel[]>([]);

  ngOnInit(): void {
    this.getListWarehouse();
  }

  getListWarehouse(): void {
    this.warehouseService.getList().subscribe({
      next: (res) => {
        this.warehousesModel.set(res);
      },
      error: (err) => {
        console.error(err);
      }
    });
  }

  onDelete(id: string): void {
    this.warehouseService.deleteWarehouse(id).subscribe({
      next: () => {
        this.getListWarehouse();
      },
      error: (err) => {
        console.error(err);
      }
    });
  }
}
