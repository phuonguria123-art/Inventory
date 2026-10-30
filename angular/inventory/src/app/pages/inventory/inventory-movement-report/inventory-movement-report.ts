import { Component, computed, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { getApiErrorMessage } from '../../../core/helpers/api-error.helper';
import { InventoryMovementSummaryModel } from '../../../core/models/features-models/inventory/inventory-movement-summary.model';
import { ProductModel } from '../../../core/models/features-models/product/product.model';
import { NotificationService } from '../../../core/services/notification.service';
import { ProductService } from '../../product/service/product.service';
import { InventoryService } from '../service/inventory.service';

@Component({
  selector: 'app-inventory-movement-report',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './inventory-movement-report.html',
  styleUrl: './inventory-movement-report.scss',
})
export class InventoryMovementReport {
  private readonly inventoryService = inject(InventoryService);
  private readonly productService = inject(ProductService);
  private readonly notification = inject(NotificationService);

  readonly products = signal<ProductModel[]>([]);
  readonly report = signal<InventoryMovementSummaryModel | null>(null);
  readonly isLoadingProducts = signal(false);
  readonly isLoadingReport = signal(false);
  readonly maxMovement = computed(() => {
    const values = this.report()?.items.flatMap(item => [item.totalStockIn, item.totalStockOut]) ?? [];
    return Math.max(1, ...values);
  });

  readonly form = new FormGroup({
    productId: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    startDate: new FormControl(this.firstDayOfCurrentYear(), { nonNullable: true, validators: [Validators.required] }),
    endDate: new FormControl(this.toDateInput(new Date()), { nonNullable: true, validators: [Validators.required] }),
  });

  ngOnInit(): void {
    this.loadProducts();
  }

  loadReport(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const { productId, startDate, endDate } = this.form.getRawValue();
    if (startDate > endDate) {
      this.notification.error('Ngày bắt đầu không được sau ngày kết thúc');
      return;
    }

    this.isLoadingReport.set(true);
    this.inventoryService.getMovementByMonthReport(productId, startDate, endDate)
      .pipe(finalize(() => this.isLoadingReport.set(false)))
      .subscribe({
        next: result => this.report.set(result),
        error: error => this.notification.error(
          getApiErrorMessage(error, 'Không thể tải báo cáo nhập/xuất')
        ),
      });
  }

  barWidth(value: number): number {
    return value <= 0 ? 0 : Math.max(3, value / this.maxMovement() * 100);
  }

  private loadProducts(): void {
    this.isLoadingProducts.set(true);
    this.productService.getProducts(1, 20)
      .pipe(finalize(() => this.isLoadingProducts.set(false)))
      .subscribe({
        next: result => this.products.set(result.items),
        error: error => this.notification.error(
          getApiErrorMessage(error, 'Không thể tải danh sách sản phẩm')
        ),
      });
  }

  private firstDayOfCurrentYear(): string {
    return `${new Date().getFullYear()}-01-01`;
  }

  private toDateInput(date: Date): string {
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');
    return `${year}-${month}-${day}`;
  }
}
