import { Component, inject, signal } from '@angular/core';
import { FormArray, FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { ReservationInventoryModel } from '../../../../core/models/features-models/inventory/reservation/reservation.model';
import { ProductModel } from '../../../../core/models/features-models/product/product.model';
import { WarehouseModel } from '../../../../core/models/features-models/warehouse/warehouse.model';
import { NotificationService } from '../../../../core/services/notification.service';
import { ProductService } from '../../../product/service/product.service';
import { WarehouseService } from '../../../warehouse/service/warehouse.service';
import { InventoryService } from '../../service/inventory.service';

@Component({
  imports: [ReactiveFormsModule, RouterLink],
  selector: 'app-reserve',
  styleUrl: './reserve.scss',
  templateUrl: './reserve.html'
})
export class Reserve {
  private readonly fb = inject(FormBuilder);
  private readonly warehouseService = inject(WarehouseService);
  private readonly productService = inject(ProductService);
  private readonly inventoryService = inject(InventoryService);
  private readonly notification = inject(NotificationService);
  private readonly router = inject(Router);

  readonly isSubmitting = signal(false);
  readonly warehouses = signal<WarehouseModel[]>([]);
  readonly productOptions = signal<ProductModel[]>([]);
  readonly minExpiry = this.toLocalDateTime(new Date());
  readonly reserveForm = this.fb.group({
    warehouseId: this.fb.nonNullable.control('', Validators.required),
    reference: this.fb.nonNullable.control('', [Validators.required, Validators.maxLength(100)]),
    expiresAt: this.fb.nonNullable.control(this.defaultExpiry()),
    isCancel: this.fb.nonNullable.control(false),
    products: this.fb.array<FormGroup>([], Validators.minLength(1))
  });

  ngOnInit(): void { this.loadOptions(); this.addProduct(); }
  get products(): FormArray<FormGroup> { return this.reserveForm.controls.products; }
  createProductForm(): FormGroup { return this.fb.group({ productId: ['', Validators.required], quantity: [1, [Validators.required, Validators.min(1)]] }); }
  addProduct(): void { this.products.push(this.createProductForm()); }
  removeProduct(i: number): void { this.products.removeAt(i); }
  submit(): void {
    if (this.reserveForm.invalid) { this.reserveForm.markAllAsTouched(); return; }
    this.isSubmitting.set(true);
    const value = this.reserveForm.getRawValue();
    const request = { ...value, reference: value.reference.trim(), expiresAt: value.isCancel || !value.expiresAt ? null : new Date(value.expiresAt).toISOString() } as ReservationInventoryModel;
    this.inventoryService.reservations(request).subscribe({
      next: () => { this.isSubmitting.set(false); this.notification.success(value.isCancel ? 'Hủy giữ hàng thành công' : 'Giữ hàng thành công'); this.router.navigate(['/inventory']); },
      error: () => { this.isSubmitting.set(false); this.notification.error(value.isCancel ? 'Không thể hủy giữ hàng' : 'Không thể giữ hàng'); }
    });
  }
  private loadOptions(): void {
    this.warehouseService.getList().subscribe({ next: data => this.warehouses.set(data) });
    this.productService.getProducts(1, 100).subscribe({ next: data => this.productOptions.set(data.items) });
  }
  private defaultExpiry(): string { const date = new Date(); date.setDate(date.getDate() + 1); return this.toLocalDateTime(date); }
  private toLocalDateTime(date: Date): string { const offset = date.getTimezoneOffset() * 60000; return new Date(date.getTime() - offset).toISOString().slice(0, 16); }
}
