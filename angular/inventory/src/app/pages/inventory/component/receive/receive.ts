import { Component, inject, signal } from '@angular/core';
import { FormArray, FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { ProductModel } from '../../../../core/models/features-models/product/product.model';
import { WarehouseModel } from '../../../../core/models/features-models/warehouse/warehouse.model';
import { NotificationService } from '../../../../core/services/notification.service';
import { ProductService } from '../../../product/service/product.service';
import { WarehouseService } from '../../../warehouse/service/warehouse.service';
import { InventoryService } from '../../service/inventory.service';

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
  readonly isSubmitting = signal(false);
  readonly receiveForm = this.fb.group({
    warehouseId: this.fb.nonNullable.control('', Validators.required),
    purchaseOrderId: this.fb.control<string | null>(null),
    reference: this.fb.nonNullable.control('', Validators.maxLength(100)),
    notes: this.fb.nonNullable.control('', Validators.maxLength(500)),
    products: this.fb.array<FormGroup>([], Validators.minLength(1)),
  });

  ngOnInit(): void { this.loadOptions(); this.addProduct(); }
  get products(): FormArray<FormGroup> { return this.receiveForm.controls.products; }
  getLots(i: number): FormArray<FormGroup> { return this.products.at(i).get('lots') as FormArray<FormGroup>; }
  createLotForm(): FormGroup { return this.fb.group({ batchNumber: ['', [Validators.required, Validators.maxLength(100)]], quantity: [1, [Validators.required, Validators.min(1)]], unitCost: [0, [Validators.required, Validators.min(0)]], manufactureDate: [null as string | null], expiryDate: [null as string | null], location: ['', Validators.maxLength(200)] }); }
  createProductForm(): FormGroup { return this.fb.group({ productId: ['', Validators.required], lots: this.fb.array<FormGroup>([this.createLotForm()], Validators.minLength(1)) }); }
  addProduct(): void { this.products.push(this.createProductForm()); }
  addLot(i: number): void { this.getLots(i).push(this.createLotForm()); }
  removeProduct(i: number): void { this.products.removeAt(i); }
  removeLot(i: number, j: number): void { this.getLots(i).removeAt(j); }

  submit(): void {
    if (this.receiveForm.invalid) { this.receiveForm.markAllAsTouched(); return; }
    this.isSubmitting.set(true);
    const value = this.receiveForm.getRawValue();
    this.inventoryService.receive({ ...value, purchaseOrderId: value.purchaseOrderId || null, reference: value.reference.trim() || null, notes: value.notes.trim() || null } as any).subscribe({
      next: () => { this.isSubmitting.set(false); this.notification.success('Nhập kho thành công'); this.router.navigate(['/inventory']); },
      error: () => { this.isSubmitting.set(false); this.notification.error('Không thể nhập kho'); },
    });
  }

  private loadOptions(): void {
    this.warehouseService.getList().subscribe({ next: (data) => this.warehouses.set(data) });
    this.productService.getProducts(1, 100).subscribe({ next: (data) => this.productOptions.set(data.items) });
  }
}
