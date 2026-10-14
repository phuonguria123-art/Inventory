import { Component, inject, signal } from '@angular/core';
import { FormArray, FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AdjustInventoryModel } from '../../../../core/models/features-models/inventory/adjust/adjust-inventory.model';
import { InventoryItemModel } from '../../../../core/models/features-models/inventory-item/inventory-item.model';
import { ProductModel } from '../../../../core/models/features-models/product/product.model';
import { WarehouseModel } from '../../../../core/models/features-models/warehouse/warehouse.model';
import { NotificationService } from '../../../../core/services/notification.service';
import { ProductService } from '../../../product/service/product.service';
import { WarehouseService } from '../../../warehouse/service/warehouse.service';
import { InventoryService } from '../../service/inventory.service';

@Component({
  imports: [ReactiveFormsModule, RouterLink],
  selector: 'app-adjust',
  styleUrl: './adjust.scss',
  templateUrl: './adjust.html'
})
export class Adjust {
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
  readonly adjustForm = this.fb.group({
    warehouseId: this.fb.nonNullable.control('', Validators.required),
    reference: this.fb.nonNullable.control('', Validators.maxLength(100)),
    notes: this.fb.nonNullable.control('', [Validators.required, Validators.maxLength(500)]),
    products: this.fb.array<FormGroup>([], Validators.minLength(1))
  });

  ngOnInit(): void { this.loadOptions(); this.addProduct(); }
  get products(): FormArray<FormGroup> { return this.adjustForm.controls.products; }
  getLots(i: number): FormArray<FormGroup> { return this.products.at(i).get('lots') as FormArray<FormGroup>; }
  createLotForm(): FormGroup { return this.fb.group({ batchNumber: ['', Validators.required], actualQuantity: [0, [Validators.required, Validators.min(0)]] }); }
  createProductForm(): FormGroup { return this.fb.group({ productId: ['', Validators.required], lots: this.fb.array<FormGroup>([this.createLotForm()], Validators.minLength(1)) }); }
  addProduct(): void { this.products.push(this.createProductForm()); }
  addLot(i: number): void { this.getLots(i).push(this.createLotForm()); }
  removeProduct(i: number): void { this.products.removeAt(i); this.reindexLotOptions(i); }
  removeLot(i: number, j: number): void { this.getLots(i).removeAt(j); }

  onWarehouseChange(): void {
    this.lotsByProduct.set({});
    this.products.controls.forEach((_, index) => { this.clearSelectedLots(index); this.loadLots(index); });
  }
  onProductChange(productIndex: number): void { this.clearSelectedLots(productIndex); this.setLotOptions(productIndex, []); this.loadLots(productIndex); }
  getLotOptions(productIndex: number): InventoryItemModel[] { return this.lotsByProduct()[productIndex] ?? []; }
  getSelectedLot(productIndex: number, lotIndex: number): InventoryItemModel | undefined {
    const batchNumber = this.getLots(productIndex).at(lotIndex).get('batchNumber')?.value;
    return this.getLotOptions(productIndex).find(lot => lot.batchNumber === batchNumber);
  }
  getDifference(productIndex: number, lotIndex: number): number | null {
    const selectedLot = this.getSelectedLot(productIndex, lotIndex);
    const actualQuantity = this.getLots(productIndex).at(lotIndex).get('actualQuantity')?.value;
    return selectedLot && actualQuantity !== null && actualQuantity !== '' ? Number(actualQuantity) - selectedLot.quantity : null;
  }

  submit(): void {
    if (this.adjustForm.invalid) { this.adjustForm.markAllAsTouched(); return; }
    this.isSubmitting.set(true);
    const value = this.adjustForm.getRawValue();
    const request = { ...value, reference: value.reference.trim() || undefined, notes: value.notes.trim() } as AdjustInventoryModel;
    this.inventoryService.adjust(request).subscribe({
      next: () => { this.isSubmitting.set(false); this.notification.success('Kiểm kê kho thành công'); this.router.navigate(['/inventory']); },
      error: () => { this.isSubmitting.set(false); this.notification.error('Không thể cập nhật kết quả kiểm kê'); }
    });
  }

  private loadOptions(): void {
    this.warehouseService.getList().subscribe({ next: data => this.warehouses.set(data) });
    this.productService.getProducts(1, 100).subscribe({ next: data => this.productOptions.set(data.items) });
  }
  private loadLots(productIndex: number): void {
    const warehouseId = this.adjustForm.controls.warehouseId.value;
    const productId = this.products.at(productIndex)?.get('productId')?.value;
    if (!warehouseId || !productId) return;
    this.inventoryService.getListLots(warehouseId, productId).subscribe({
      next: lots => {
        if (warehouseId !== this.adjustForm.controls.warehouseId.value || productId !== this.products.at(productIndex)?.get('productId')?.value) return;
        this.setLotOptions(productIndex, lots);
      },
      error: () => this.notification.error('Không thể tải danh sách lô của sản phẩm')
    });
  }
  private clearSelectedLots(productIndex: number): void { this.getLots(productIndex).controls.forEach(lot => lot.get('batchNumber')?.setValue('')); }
  private setLotOptions(productIndex: number, lots: InventoryItemModel[]): void { this.lotsByProduct.update(current => ({ ...current, [productIndex]: lots })); }
  private reindexLotOptions(removedIndex: number): void {
    const next: Record<number, InventoryItemModel[]> = {};
    Object.entries(this.lotsByProduct()).forEach(([key, value]) => { const index = Number(key); if (index < removedIndex) next[index] = value; if (index > removedIndex) next[index - 1] = value; });
    this.lotsByProduct.set(next);
  }
}
