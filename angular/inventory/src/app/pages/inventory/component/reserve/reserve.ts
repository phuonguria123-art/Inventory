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
import { finalize, forkJoin } from 'rxjs';
import { getApiErrorMessage } from '../../../../core/helpers/api-error.helper';

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
  createProductForm(): FormGroup { return this.fb.group({ productId: ['', Validators.required], quantity: [1, [Validators.required, Validators.min(1), Validators.pattern(/^\d+$/)]] }); }
  addProduct(): void { this.products.push(this.createProductForm()); }
  removeProduct(i: number): void { if (this.products.length > 1) this.products.removeAt(i); }
  isDuplicateProduct(i: number): boolean { const id = this.products.at(i).get('productId')?.value; return !!id && this.products.controls.some((p, x) => x !== i && p.get('productId')?.value === id); }
  submit(): void {
    const value = this.reserveForm.getRawValue();
    const expiryTime = value.expiresAt ? new Date(value.expiresAt).getTime() : Number.NaN;
    const expiryInvalid = !value.isCancel && (!Number.isFinite(expiryTime) || expiryTime <= Date.now());
    const hasDuplicates = this.products.controls.some((_, i) => this.isDuplicateProduct(i));
    if (this.reserveForm.invalid || expiryInvalid || hasDuplicates) { this.reserveForm.markAllAsTouched(); this.notification.warning(expiryInvalid ? 'Thời gian hết hạn phải ở tương lai.' : 'Vui lòng kiểm tra sản phẩm và số lượng giữ hàng.'); return; }
    this.isSubmitting.set(true);
    const request: ReservationInventoryModel = {
      warehouseId: value.warehouseId,
      reference: value.reference.trim(),
      expiresAt: value.isCancel || !value.expiresAt ? null : new Date(value.expiresAt).toISOString(),
      isCancel: value.isCancel,
      products: value.products.map(product => ({
        productId: String(product['productId']),
        quantity: Number(product['quantity']),
      })),
    };
    this.inventoryService.reservations(request).pipe(finalize(() => this.isSubmitting.set(false))).subscribe({
      next: () => { this.notification.success(value.isCancel ? 'Hủy giữ hàng thành công' : 'Giữ hàng thành công'); this.router.navigate(['/inventory']); },
      error: err => this.notification.error(getApiErrorMessage(err, value.isCancel ? 'Không thể hủy giữ hàng' : 'Không thể giữ hàng'))
    });
  }
  private loadOptions(): void {
    forkJoin({ warehouses: this.warehouseService.getList(), products: this.productService.getProducts(1, 100) }).subscribe({
      next: ({ warehouses, products }) => { this.warehouses.set(warehouses); this.productOptions.set(products.items); },
      error: err => this.notification.error(getApiErrorMessage(err, 'Không thể tải danh sách kho và sản phẩm')),
    });
  }
  private defaultExpiry(): string { const date = new Date(); date.setDate(date.getDate() + 1); return this.toLocalDateTime(date); }
  private toLocalDateTime(date: Date): string { const offset = date.getTimezoneOffset() * 60000; return new Date(date.getTime() - offset).toISOString().slice(0, 16); }
}
