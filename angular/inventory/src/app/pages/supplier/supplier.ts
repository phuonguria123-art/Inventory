import { Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { SupplierModel } from '../../core/models/features-models/supplier/supplier.model';
import { NotificationService } from '../../core/services/notification.service';
import { SupplierService } from './service/supplier.service';

@Component({
  imports: [RouterLink],
  selector: 'app-supplier',
  styleUrl: './supplier.scss',
  templateUrl: './supplier.html',
})
export class Supplier {
  private readonly supplierService = inject(SupplierService);
  private readonly notification = inject(NotificationService);

  readonly suppliers = signal<SupplierModel[]>([]);
  readonly isLoading = signal(false);

  ngOnInit(): void {
    this.getSuppliers();
  }

  getSuppliers(): void {
    this.isLoading.set(true);
    this.supplierService.getList().subscribe({
      next: (suppliers) => {
        this.suppliers.set(suppliers);
        this.isLoading.set(false);
      },
      error: () => {
        this.isLoading.set(false);
        this.notification.error('Không thể tải danh sách nhà cung cấp');
      },
    });
  }

  onDelete(supplier: SupplierModel): void {
    if (!window.confirm(`Bạn có chắc muốn xóa nhà cung cấp "${supplier.name}"?`)) return;

    this.supplierService.deleteSupplier(supplier.id).subscribe({
      next: () => {
        this.notification.success('Xóa nhà cung cấp thành công');
        this.getSuppliers();
      },
      error: () => this.notification.error('Không thể xóa nhà cung cấp'),
    });
  }
}
