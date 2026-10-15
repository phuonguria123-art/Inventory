import { Component, computed, inject, signal } from '@angular/core';
import { FormArray, FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { WarehouseService } from '../../../warehouse/service/warehouse.service';
import { ProductService } from '../../../product/service/product.service';
import { WarehouseModel } from '../../../../core/models/features-models/warehouse/warehouse.model';
import { ProductModel } from '../../../../core/models/features-models/product/product.model';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { PurchaseOrderService } from '../../service/purchase-order.service';
import { NotificationService } from '../../../../core/services/notification.service';
import { PurchaseOrderCreateModel } from '../../../../core/models/features-models/purchase-order/purchase-order-create.model';
import { SupplierService } from '../../../supplier/service/supplier.service';
import { SupplierModel } from '../../../../core/models/features-models/supplier/supplier.model';
import { PurchaseOrderModel } from '../../../../core/models/features-models/purchase-order/purchase-order.model';
import { PurchaseOrderUpdateModel } from '../../../../core/models/features-models/purchase-order/purchase-order-update.model';
import { PurchaseOrderStatus } from '../../../../core/enum/purchase-order-status.enum';
import { getApiErrorMessage } from '../../../../core/helpers/api-error.helper';
import { finalize, forkJoin } from 'rxjs';

@Component({
  imports: [ReactiveFormsModule, RouterLink],
  selector: 'app-purchase-order-form',
  styleUrl: './purchase-order-form.scss',
  templateUrl: './purchase-order-form.html',
})
export class PurchaseOrderForm {
  private readonly fb = inject(FormBuilder);
  private readonly warehouseService = inject(WarehouseService);
  private readonly supplierService = inject(SupplierService);
  private readonly productService = inject(ProductService);
  private readonly purchaseOrderService = inject(PurchaseOrderService);
  private readonly notification = inject(NotificationService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  readonly warehouses = signal<WarehouseModel[]>([]);
  readonly suppliers = signal<SupplierModel[]>([]);
  readonly productOptions = signal<ProductModel[]>([]);
  readonly isSubmitting = signal(false);
  readonly isLoading = signal(false);
  readonly isLoadingOptions = signal(false);
  readonly currentOrder = signal<PurchaseOrderModel | null>(null);
  purchaseOrderId: string | null = null;
  mode: 'create' | 'edit' | 'view' = 'create';
  readonly pageTitle = computed(() => this.mode === 'create' ? 'Tạo đơn mua hàng' : this.mode === 'edit' ? 'Cập nhật đơn mua hàng' : 'Chi tiết đơn mua hàng');

  readonly orderForm = this.fb.group({
    supplierId: this.fb.nonNullable.control('', Validators.required),
    expectedReceiveDate: this.fb.nonNullable.control('', Validators.required),
    receivingWarehouseId: this.fb.nonNullable.control('', Validators.required),
    freight: [0, [Validators.min(0)]],
    type: [''],
    totalWeight: [0, [Validators.min(0)]],
    products: this.fb.array<FormGroup>([], Validators.minLength(1))
  });
  ngOnInit(): void {
    this.mode = this.route.snapshot.data['mode'] ?? 'create';
    this.loadOptions(); this.addProduct();
    if (this.mode !== 'create') {
      this.purchaseOrderId = this.route.snapshot.paramMap.get('id');
      if (this.purchaseOrderId) this.loadPurchaseOrder(this.purchaseOrderId);
    }
  }
  get products(): FormArray<FormGroup> { return this.orderForm.controls.products; }
  createProductForm(): FormGroup {
    return this.fb.group({
      productId: ['', Validators.required],
      orderedQuantity: [1, [Validators.required, Validators.min(1), Validators.pattern(/^\d+$/)]],
      unitPrice: [0, [Validators.required, Validators.min(0)]],
      type: [''],
      note: ['', Validators.maxLength(500)],
      id: [null as string | null]
    });
  }

  addProduct(): void { this.products.push(this.createProductForm()); }
  removeProduct(i: number): void { if (this.products.length > 1) this.products.removeAt(i); }
  isDuplicateProduct(i: number): boolean { const id = this.products.at(i).get('productId')?.value; return !!id && this.products.controls.some((p, x) => x !== i && p.get('productId')?.value === id); }
  getLineTotal(i: number): number { const row = this.products.at(i).getRawValue(); return Number(row.orderedQuantity || 0) * Number(row.unitPrice || 0); }
  getSubTotal(): number { return this.products.controls.reduce((total, _, index) => total + this.getLineTotal(index), 0); }
  getGrandTotal(): number { return this.getSubTotal() + Number(this.orderForm.controls.freight.value || 0); }
  enableEdit(): void { if (this.purchaseOrderId) this.router.navigate(['/purchase-orders/edit', this.purchaseOrderId]); }
  canEdit(): boolean { return this.currentOrder()?.status === PurchaseOrderStatus.Draft; }

  private readonly statusLabels: Partial<Record<PurchaseOrderStatus, string>> = {
    [PurchaseOrderStatus.Draft]: 'Xác nhận gửi',
    [PurchaseOrderStatus.Sent]: 'NCC nhận đơn',
    [PurchaseOrderStatus.Confirmed]: 'Đã vận chuyển',
    [PurchaseOrderStatus.Shipping]: 'Nhận hàng'
  };

  private loadOptions(): void {
    this.isLoadingOptions.set(true);

    forkJoin({
      warehouses: this.warehouseService.getList(),
      products: this.productService.getProducts(1, 100),
      suppliers: this.supplierService.getList(),
    })
      .pipe(finalize(() => this.isLoadingOptions.set(false)))
      .subscribe({
        next: result => {
          this.warehouses.set(result.warehouses);
          this.productOptions.set(result.products.items);
          this.suppliers.set(result.suppliers);
        },
        error: error => {
          this.notification.error(
            getApiErrorMessage(
              error,
              'Không thể tải dữ liệu cho biểu mẫu'
            )
          );
        },
      });
  }

  submit(): void {
    const hasDuplicateProducts = this.products.controls.some((_, i) => this.isDuplicateProduct(i));
    if (this.orderForm.invalid || hasDuplicateProducts) {
      this.orderForm.markAllAsTouched();
      this.notification.warning(hasDuplicateProducts ? 'Một sản phẩm đang bị thêm trùng trong đơn.' : 'Vui lòng kiểm tra các trường chưa hợp lệ.');
      return;
    }
    this.isSubmitting.set(true);
    const value = this.orderForm.getRawValue();
    const common = { supplierId: value.supplierId, expectedReceiveDate: new Date(value.expectedReceiveDate), receivingWarehouseId: value.receivingWarehouseId, freight: value.freight, type: value.type?.trim() || null, totalWeight: value.totalWeight };
    const requestCreate: PurchaseOrderCreateModel = { ...common, products: value.products.map(product => ({ productId: product['productId'], orderedQuantity: Number(product['orderedQuantity']), unitPrice: Number(product['unitPrice']), type: product['type']?.trim() || null, note: product['note']?.trim() || '' })) };
    const requestUpdate: PurchaseOrderUpdateModel = { ...common, orderDetails: value.products.map(product => ({ id: product['id'] || null, productId: product['productId'], orderedQuantity: Number(product['orderedQuantity']), unitPrice: Number(product['unitPrice']), type: product['type']?.trim() || null, note: product['note']?.trim() || '' })) };

    if (this.purchaseOrderId) {
      this.purchaseOrderService.update(this.purchaseOrderId, requestUpdate).pipe(finalize(() => this.isSubmitting.set(false))).subscribe({
        next: () => {
          this.notification.success('Cập nhật đơn mua hàng thành công');
          this.router.navigate(['/purchase-orders/detail', this.purchaseOrderId]);
        },
        error: (err) => {
          this.notification.error(getApiErrorMessage(err, 'Không thể cập nhật đơn mua hàng'));
        }
      });
    }
    else {
      this.purchaseOrderService.create(requestCreate).pipe(finalize(() => this.isSubmitting.set(false))).subscribe({
        next: () => {
          this.notification.success('Tạo đơn mua hàng thành công');
          this.router.navigate(['/purchase-orders']);
        },
        error: (error) => {
          this.notification.error(getApiErrorMessage(error, 'Không thể tạo đơn mua hàng'));
        }
      });
    }

  }

  updateStatus(status: PurchaseOrderStatus) {
    if (this.purchaseOrderId === null) {
      return;
    }
    this.isSubmitting.set(true);
    this.purchaseOrderService.updateStatus(this.purchaseOrderId, status).pipe(finalize(() => this.isSubmitting.set(false))).subscribe({
      next: () => {
        this.notification.success('Cập nhật trạng thái đơn mua hàng thành công');
        this.loadPurchaseOrder(this.purchaseOrderId!);
      },
      error: (err) => this.notification.error(getApiErrorMessage(err, 'Không thể cập nhật trạng thái đơn mua hàng'))
    })
  }
  cancelOrder(): void {
    if (!this.purchaseOrderId || !window.confirm('Bạn có chắc muốn hủy đơn mua hàng này?')) return;
    this.isSubmitting.set(true);
    this.purchaseOrderService.cancel(this.purchaseOrderId).pipe(finalize(() => this.isSubmitting.set(false))).subscribe({
      next: () => { this.notification.success('Hủy đơn mua hàng thành công'); this.loadPurchaseOrder(this.purchaseOrderId!); },
      error: err => this.notification.error(getApiErrorMessage(err, 'Không thể hủy đơn mua hàng'))
    });
  }
  canCancel(): boolean {
    const status = this.currentOrder()?.status;
    return status !== undefined && status !== PurchaseOrderStatus.Received && status !== PurchaseOrderStatus.Cancelled;
  }

  loadPurchaseOrder(id: string) {
    this.isLoading.set(true);
    this.purchaseOrderService.getById(id).pipe(finalize(() => this.isLoading.set(false))).subscribe({
      next: (result) => {
        this.currentOrder.set(result);
        if (this.mode === 'edit' && result.status !== PurchaseOrderStatus.Draft) {
          this.notification.error('Chỉ đơn mua hàng ở trạng thái nháp mới được cập nhật');
          this.router.navigate(['/purchase-orders/detail', id]);
          return;
        }
        this.partModelToForm(result);
        if (this.mode === 'view') this.orderForm.disable();
      },
      error: err => this.notification.error(getApiErrorMessage(err, 'Không thể tải thông tin đơn mua hàng')),
    })
  }

  partModelToForm(model: PurchaseOrderModel): void {
    this.orderForm.patchValue({
      supplierId: model.supplierId,
      expectedReceiveDate: model.expectedReceiveDate
        ? new Date(model.expectedReceiveDate).toISOString().split('T')[0]
        : '',
      receivingWarehouseId: model.receivingWarehouseId,
      freight: model.freight,
      type: model.type,
      totalWeight: model.totalWeight
    });

    this.products.clear();
    model.orderDetailDto.forEach(item => {
      this.products.push(
        this.fb.group({
          productId: [item.productId, Validators.required],
          orderedQuantity: [
            item.orderedQuantity,
            [Validators.required, Validators.min(1), Validators.pattern(/^\d+$/)]
          ],
          unitPrice: [
            item.unitPrice,
            [Validators.required, Validators.min(1)]
          ],
          type: [item.type ?? ''],
          note: [item.note ?? '', Validators.maxLength(500)],
          id: [item.id]
        })
      );
    });
  }
  public PurchaseOrderStatus = PurchaseOrderStatus;

}
