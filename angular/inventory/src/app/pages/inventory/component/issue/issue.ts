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
import { finalize } from 'rxjs';
import { getApiErrorMessage } from '../../../../core/helpers/api-error.helper';
import { IssueInventoryModel } from '../../../../core/models/features-models/inventory/issue/issue-inventory.model';

@Component({ imports: [ReactiveFormsModule, RouterLink], selector: 'app-issue', styleUrl: './issue.scss', templateUrl: './issue.html' })
export class Issue {
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
  readonly issueForm = this.fb.group({
    warehouseId: this.fb.nonNullable.control('', Validators.required),
    reference: this.fb.nonNullable.control('', Validators.maxLength(100)),
    notes: this.fb.nonNullable.control('', Validators.maxLength(500)),
    products: this.fb.array<FormGroup>([], Validators.minLength(1))
  });

  ngOnInit(): void { this.loadOptions(); this.addProduct(); }
  get products(): FormArray<FormGroup> { return this.issueForm.controls.products; }
  getLots(i: number): FormArray<FormGroup> { return this.products.at(i).get('lots') as FormArray<FormGroup>; }
  createLotForm(): FormGroup { return this.fb.group({ batchNumber: ['', [Validators.required, Validators.maxLength(100)]], quantity: [1, [Validators.required, Validators.min(1), Validators.pattern(/^\d+$/)]] }); }
  createProductForm(): FormGroup { return this.fb.group({ productId: ['', Validators.required], lots: this.fb.array<FormGroup>([this.createLotForm()], Validators.minLength(1)) }); }
  addProduct(): void { this.products.push(this.createProductForm()); }
  addLot(i: number): void { this.getLots(i).push(this.createLotForm()); }
  removeProduct(i: number): void { if (this.products.length > 1) { this.products.removeAt(i); this.reindexLotOptions(i); } }
  removeLot(i: number, j: number): void { const lots = this.getLots(i); if (lots.length > 1) lots.removeAt(j); }

  isDuplicateProduct(i: number): boolean { const id = this.products.at(i).get('productId')?.value; return !!id && this.products.controls.some((p, x) => x !== i && p.get('productId')?.value === id); }
  isDuplicateLot(i: number, j: number): boolean { const lots = this.getLots(i); const batch = lots.at(j).get('batchNumber')?.value; return !!batch && lots.controls.some((l, x) => x !== j && l.get('batchNumber')?.value === batch); }
  isQuantityExceeded(i: number, j: number): boolean { const selected = this.getSelectedLot(i, j); const quantity = Number(this.getLots(i).at(j).get('quantity')?.value); return !!selected && quantity > selected.quantity; }

  onWarehouseChange(): void {
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
    const businessInvalid = this.products.controls.some((_, i) => this.isDuplicateProduct(i) || this.getLots(i).controls.some((__, j) => this.isDuplicateLot(i, j) || this.isQuantityExceeded(i, j)));
    if (this.issueForm.invalid || businessInvalid) { this.issueForm.markAllAsTouched(); this.notification.warning('Vui lòng kiểm tra sản phẩm, lô và số lượng xuất.'); return; }
    this.isSubmitting.set(true);
    const value = this.issueForm.getRawValue();
    const request: IssueInventoryModel = {
      warehouseId: value.warehouseId,
      reference: value.reference.trim() || undefined,
      notes: value.notes.trim() || undefined,
      products: value.products.map(product => ({
        productId: String(product['productId']),
        lots: (product['lots'] as Array<Record<string, unknown>>).map(lot => ({
          batchNumber: String(lot['batchNumber']).trim(),
          quantity: Number(lot['quantity']),
        })),
      })),
    };
    this.inventoryService.issue(request).pipe(finalize(() => this.isSubmitting.set(false))).subscribe({
      next: () => { this.notification.success('Xuất kho thành công'); this.router.navigate(['/inventory']); },
      error: err => this.notification.error(getApiErrorMessage(err, 'Không thể xuất kho')),
    });
  }
  private loadOptions(): void { this.warehouseService.getList().subscribe({ next: (data) => this.warehouses.set(data) }); this.productService.getProducts(1, 100).subscribe({ next: (data) => this.productOptions.set(data.items) }); }

  private loadLots(productIndex: number): void {
    const warehouseId = this.issueForm.controls.warehouseId.value;
    const productId = this.products.at(productIndex)?.get('productId')?.value;
    if (!warehouseId || !productId) return;

    this.inventoryService.getListLots(warehouseId, productId).subscribe({
      next: (lots) => {
        const currentWarehouseId = this.issueForm.controls.warehouseId.value;
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
