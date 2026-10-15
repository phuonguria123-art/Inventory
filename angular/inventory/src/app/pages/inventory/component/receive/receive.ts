import { Component, inject, signal } from '@angular/core';
import { AbstractControl, FormArray, FormBuilder, FormGroup, ReactiveFormsModule, ValidationErrors, ValidatorFn, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { finalize, forkJoin } from 'rxjs';
import { ReceiveInventoryModel } from '../../../../core/models/features-models/inventory/receive/receive-inventoty.model';
import { ReceiveLotModel } from '../../../../core/models/features-models/inventory/receive/receive-lot.model';
import { ProductModel } from '../../../../core/models/features-models/product/product.model';
import { WarehouseModel } from '../../../../core/models/features-models/warehouse/warehouse.model';
import { NotificationService } from '../../../../core/services/notification.service';
import { ProductService } from '../../../product/service/product.service';
import { WarehouseService } from '../../../warehouse/service/warehouse.service';
import { InventoryService } from '../../service/inventory.service';
import { getApiErrorMessage } from '../../../../core/helpers/api-error.helper';

const GUID_PATTERN = /^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i;
const lotDateValidator: ValidatorFn = (control: AbstractControl): ValidationErrors | null => {
  const manufactureDate = control.get('manufactureDate')?.value as string | null;
  const expiryDate = control.get('expiryDate')?.value as string | null;
  return manufactureDate && expiryDate && expiryDate < manufactureDate ? { expiryBeforeManufacture: true } : null;
};

@Component({ imports: [ReactiveFormsModule, RouterLink], selector: 'app-receive', styleUrl: './receive.scss', templateUrl: './receive.html' })
export class Receive {
  private readonly fb = inject(FormBuilder);
  private readonly inventoryService = inject(InventoryService);
  private readonly warehouseService = inject(WarehouseService);
  private readonly productService = inject(ProductService);
  private readonly notification = inject(NotificationService);
  private readonly router = inject(Router);

  readonly warehouses = signal<WarehouseModel[]>([]);
  readonly productOptions = signal<ProductModel[]>([]);
  readonly isLoadingOptions = signal(false);
  readonly isSubmitting = signal(false);
  readonly receiveForm = this.fb.group({
    warehouseId: this.fb.nonNullable.control('', Validators.required),
    purchaseOrderId: this.fb.control<string | null>(null, Validators.pattern(GUID_PATTERN)),
    reference: this.fb.nonNullable.control('', Validators.maxLength(100)),
    notes: this.fb.nonNullable.control('', Validators.maxLength(500)),
    products: this.fb.array<FormGroup>([], Validators.minLength(1)),
  });

  ngOnInit(): void { this.loadOptions(); this.addProduct(); }
  get products(): FormArray<FormGroup> { return this.receiveForm.controls.products; }
  getLots(i: number): FormArray<FormGroup> { return this.products.at(i).get('lots') as FormArray<FormGroup>; }
  createLotForm(): FormGroup { return this.fb.group({ batchNumber: ['', [Validators.required, Validators.maxLength(100)]], quantity: [1, [Validators.required, Validators.min(1), Validators.pattern(/^\d+$/)]], unitCost: [0, [Validators.required, Validators.min(0)]], manufactureDate: [null as string | null], expiryDate: [null as string | null], location: ['', Validators.maxLength(200)] }, { validators: lotDateValidator }); }
  createProductForm(): FormGroup { return this.fb.group({ productId: ['', Validators.required], lots: this.fb.array<FormGroup>([this.createLotForm()], Validators.minLength(1)) }); }
  addProduct(): void { this.products.push(this.createProductForm()); }
  addLot(i: number): void { this.getLots(i).push(this.createLotForm()); }
  removeProduct(i: number): void { if (this.products.length > 1) this.products.removeAt(i); }
  removeLot(i: number, j: number): void { const lots = this.getLots(i); if (lots.length > 1) lots.removeAt(j); }

  isDuplicateProduct(i: number): boolean {
    const id = this.products.at(i).get('productId')?.value;
    return !!id && this.products.controls.some((product, index) => index !== i && product.get('productId')?.value === id);
  }

  isDuplicateBatch(i: number, j: number): boolean {
    const lots = this.getLots(i);
    const batch = this.normalizeBatch(lots.at(j).get('batchNumber')?.value);
    return !!batch && lots.controls.some((lot, index) => index !== j && this.normalizeBatch(lot.get('batchNumber')?.value) === batch);
  }

  submit(): void {
    const hasDuplicates = this.products.controls.some((_, i) =>
      this.isDuplicateProduct(i) || this.getLots(i).controls.some((__, j) => this.isDuplicateBatch(i, j)));
    if (this.receiveForm.invalid || hasDuplicates) {
      this.receiveForm.markAllAsTouched();
      this.notification.warning(hasDuplicates
        ? 'Một sản phẩm hoặc mã lô đang bị nhập trùng.'
        : 'Vui lòng kiểm tra lại các trường chưa hợp lệ.');
      return;
    }
    this.isSubmitting.set(true);
    const value = this.receiveForm.getRawValue();
    const request: ReceiveInventoryModel = {
      warehouseId: value.warehouseId,
      purchaseOrderId: value.purchaseOrderId?.trim() || null,
      reference: value.reference.trim() || null,
      notes: value.notes.trim() || null,
      products: value.products.map(product => {
        const lots = product['lots'] as ReceiveLotModel[];
        return {
          productId: product['productId'] as string,
          lots: lots.map(lot => ({ ...lot, batchNumber: lot.batchNumber.trim(), location: lot.location?.trim() || null })),
        };
      }),
    };
    this.inventoryService.receive(request).pipe(finalize(() => this.isSubmitting.set(false))).subscribe({
      next: () => { this.notification.success('Nhập kho thành công'); this.router.navigate(['/inventory']); },
      error: (err) => this.notification.error(getApiErrorMessage(err, 'Không thể nhập kho')),
    });
  }

  private normalizeBatch(value: unknown): string { return String(value ?? '').trim().toLocaleLowerCase(); }

  private loadOptions(): void {
    this.isLoadingOptions.set(true);
    forkJoin({ warehouses: this.warehouseService.getList(), products: this.productService.getProducts(1, 100) })
      .pipe(finalize(() => this.isLoadingOptions.set(false)))
      .subscribe({
        next: ({ warehouses, products }) => { this.warehouses.set(warehouses); this.productOptions.set(products.items); },
        error: (err) => this.notification.error(getApiErrorMessage(err, 'Không thể tải danh sách kho và sản phẩm')),
      });
  }
}
