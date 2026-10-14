import { Component, inject, signal } from '@angular/core';
import { FormArray, FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { ProductModel } from '../../../../core/models/features-models/product/product.model';
import { WarehouseModel } from '../../../../core/models/features-models/warehouse/warehouse.model';
import { NotificationService } from '../../../../core/services/notification.service';
import { ProductService } from '../../../product/service/product.service';
import { WarehouseService } from '../../../warehouse/service/warehouse.service';
import { InventoryService } from '../../service/inventory.service';
import { InventoryItemModel } from '../../../../core/models/features-models/inventory-item/inventory-item.model';

@Component({ imports: [ReactiveFormsModule, RouterLink], selector: 'app-transfer', styleUrl: './transfer.scss', templateUrl: './transfer.html' })
export class Transfer {
  private readonly fb = inject(FormBuilder);
  private readonly inventoryService = inject(InventoryService);
  private readonly warehouseService = inject(WarehouseService);
  private readonly productService = inject(ProductService);
  private readonly notification = inject(NotificationService);
  private readonly router = inject(Router);
  readonly warehouses = signal<WarehouseModel[]>([]);
  readonly productOptions = signal<ProductModel[]>([]);
  readonly lotsByProduct = signal<Record<number, InventoryItemModel[]>>({});
  readonly isSubmitting = signal(false);
  readonly transferForm = this.fb.group({ sourceWarehouseId: this.fb.nonNullable.control('', Validators.required), destinationWarehouseId: this.fb.nonNullable.control('', Validators.required), reference: this.fb.nonNullable.control('', Validators.maxLength(100)), notes: this.fb.nonNullable.control('', Validators.maxLength(500)), products: this.fb.array<FormGroup>([], Validators.minLength(1)) });

  ngOnInit(): void { this.loadOptions(); this.addProduct(); }
  get products(): FormArray<FormGroup> { return this.transferForm.controls.products; }
  getLots(i: number): FormArray<FormGroup> { return this.products.at(i).get('lots') as FormArray<FormGroup>; }
  createLotForm(): FormGroup { return this.fb.group({ batchNumber: ['', [Validators.required, Validators.maxLength(100)]], quantity: [1, [Validators.required, Validators.min(1)]], location: ['', Validators.maxLength(200)] }); }
  createProductForm(): FormGroup { return this.fb.group({ productId: ['', Validators.required], lots: this.fb.array<FormGroup>([this.createLotForm()], Validators.minLength(1)) }); }
  addProduct(): void { this.products.push(this.createProductForm()); }
  addLot(i: number): void { this.getLots(i).push(this.createLotForm()); }
  removeProduct(i: number): void { this.products.removeAt(i); this.reindexLotOptions(i); }
  removeLot(i: number, j: number): void { this.getLots(i).removeAt(j); }

  onSourceWarehouseChange(): void {
    this.lotsByProduct.set({});
    this.products.controls.forEach((_, index) => {
      this.clearSelectedLots(index);
      this.loadLots(index);
    });
  }

  onProductChange(productIndex: number): void {
    this.clearSelectedLots(productIndex);
    this.setLotOptions(productIndex, []);
    this.loadLots(productIndex);
  }

  getLotOptions(productIndex: number): InventoryItemModel[] {
    return this.lotsByProduct()[productIndex] ?? [];
  }

  getSelectedLot(productIndex: number, lotIndex: number): InventoryItemModel | undefined {
    const batchNumber = this.getLots(productIndex).at(lotIndex).get('batchNumber')?.value;
    return this.getLotOptions(productIndex).find((lot) => lot.batchNumber === batchNumber);
  }

  submit(): void {
    if (this.transferForm.invalid) { this.transferForm.markAllAsTouched(); return; }
    const value = this.transferForm.getRawValue();
    if (value.sourceWarehouseId === value.destinationWarehouseId) { this.notification.warning('Kho nguồn và kho đích phải khác nhau'); return; }
    this.isSubmitting.set(true);
    this.inventoryService.transfer({ warehouseFromId: value.sourceWarehouseId, warehouseToId: value.destinationWarehouseId, reference: value.reference.trim() || undefined, notes: value.notes.trim() || undefined, products: value.products } as any).subscribe({
      next: () => { this.isSubmitting.set(false); this.notification.success('Chuyển kho thành công'); this.router.navigate(['/inventory']); },
      error: () => { this.isSubmitting.set(false); this.notification.error('Không thể chuyển kho'); },
    });
  }
  private loadOptions(): void { this.warehouseService.getList().subscribe({ next: (data) => this.warehouses.set(data) }); this.productService.getProducts(1, 100).subscribe({ next: (data) => this.productOptions.set(data.items) }); }

  private loadLots(productIndex: number): void {
    const warehouseId = this.transferForm.controls.sourceWarehouseId.value;
    const productId = this.products.at(productIndex)?.get('productId')?.value;
    if (!warehouseId || !productId) return;

    this.inventoryService.getListLots(warehouseId, productId).subscribe({
      next: (lots) => {
        const currentWarehouseId = this.transferForm.controls.sourceWarehouseId.value;
        const currentProductId = this.products.at(productIndex)?.get('productId')?.value;
        if (warehouseId !== currentWarehouseId || productId !== currentProductId) return;
        const today = new Date();
        today.setHours(0, 0, 0, 0);
        this.setLotOptions(productIndex, lots.filter((lot) =>
          lot.quantity > 0 && (!lot.expiryDate || new Date(lot.expiryDate) >= today)
        ));
      },
      error: () => this.notification.error('Không thể tải danh sách lô của sản phẩm'),
    });
  }

  private clearSelectedLots(productIndex: number): void {
    this.getLots(productIndex).controls.forEach((lot) => lot.get('batchNumber')?.setValue(''));
  }

  private setLotOptions(productIndex: number, lots: InventoryItemModel[]): void {
    this.lotsByProduct.update((current) => ({ ...current, [productIndex]: lots }));
  }

  private reindexLotOptions(removedIndex: number): void {
    const next: Record<number, InventoryItemModel[]> = {};
    Object.entries(this.lotsByProduct()).forEach(([key, value]) => {
      const index = Number(key);
      if (index < removedIndex) next[index] = value;
      if (index > removedIndex) next[index - 1] = value;
    });
    this.lotsByProduct.set(next);
  }
}
