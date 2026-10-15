import { Component, inject, signal } from '@angular/core';
import { AbstractControl, FormArray, FormBuilder, FormGroup, ReactiveFormsModule, ValidationErrors, ValidatorFn, Validators } from '@angular/forms';
import { PurchaseOrderService } from '../../service/purchase-order.service';
import { PurchaseOrderModel } from '../../../../core/models/features-models/purchase-order/purchase-order.model';
import { NotificationService } from '../../../../core/services/notification.service';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { ReceivePurchaseOrderModel } from '../../../../core/models/features-models/purchase-order/receive/receive-purchase-order.model';
import { ProductService } from '../../../product/service/product.service';
import { ProductModel } from '../../../../core/models/features-models/product/product.model';
import { PurchaseOrderStatus } from '../../../../core/enum/purchase-order-status.enum';
import { finalize, forkJoin } from 'rxjs';
import { getApiErrorMessage } from '../../../../core/helpers/api-error.helper';
import { ReceivePurchaseOrderLotModel } from '../../../../core/models/features-models/purchase-order/receive/receive-purchase-order-lot.model';

const receiveLotDateValidator: ValidatorFn = (control: AbstractControl): ValidationErrors | null => {
  const manufactureDate = control.get('manufactureDate')?.value as string | null;
  const expiryDate = control.get('expiryDate')?.value as string | null;
  return manufactureDate && expiryDate && expiryDate < manufactureDate ? { expiryBeforeManufacture: true } : null;
};

@Component({
  imports: [ReactiveFormsModule, RouterLink],
  selector: 'app-purchase-order-receive',
  styleUrl: './purchase-order-receive.scss',
  templateUrl: './purchase-order-receive.html',
})
export class PurchaseOrderReceive {
  private readonly purchaseOrderService = inject(PurchaseOrderService);
  private readonly fb = inject(FormBuilder);
  private readonly notification = inject(NotificationService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly productService = inject(ProductService);

  readonly currentOrder = signal<PurchaseOrderModel | null>(null);
  readonly products = signal<ProductModel[]>([]);
  readonly isLoading = signal(false);
  readonly isSubmitting = signal(false);
  purchaseOrderReceiveModel: ReceivePurchaseOrderModel = new ReceivePurchaseOrderModel();
  purchaseOrderId: string | null = null;
  readonly receiveForm = this.fb.group({
    purchaseOrderId: this.fb.nonNullable.control('', Validators.required),
    orderDetails: this.fb.array<FormGroup>([], Validators.minLength(1))
  });
  ngOnInit(): void {
    this.purchaseOrderId = this.route.snapshot.paramMap.get('id');
    if (this.purchaseOrderId) {
      this.loadData(this.purchaseOrderId);
    }
  }
  get orderDetails(): FormArray<FormGroup> {
    return this.receiveForm.controls.orderDetails;
  }
  getLots(i: number): FormArray<FormGroup> { return this.orderDetails.at(i).get('lots') as FormArray<FormGroup>; }
  createLotForm(): FormGroup {
    return this.fb.group({
      batchNumber: ['', [Validators.required, Validators.maxLength(100)]],
      receivedQuantity: [1, [Validators.required, Validators.min(1), Validators.pattern(/^\d+$/)]],
      unitCost: [0, [Validators.required, Validators.min(0)]],
      manufactureDate: [null as string | null],
      expiryDate: [null as string | null],
      location: ['', Validators.maxLength(200)]
    }, { validators: receiveLotDateValidator });
  }
  addLot(i: number): void { this.getLots(i).push(this.createLotForm()); }
  removeLot(i: number, j: number): void { const lots = this.getLots(i); if (lots.length > 1) lots.removeAt(j); }
  isDuplicateLot(i: number, j: number): boolean { const lots = this.getLots(i); const batch = String(lots.at(j).get('batchNumber')?.value ?? '').trim().toLocaleLowerCase(); return !!batch && lots.controls.some((l, x) => x !== j && String(l.get('batchNumber')?.value ?? '').trim().toLocaleLowerCase() === batch); }
  getReceivedQuantity(i: number): number { return this.getLots(i).controls.reduce((total, lot) => total + Number(lot.get('receivedQuantity')?.value || 0), 0); }
  getDifference(i: number): number { return this.getReceivedQuantity(i) - Number(this.orderDetails.at(i).get('orderedQuantity')?.value || 0); }
  getProductName(id: string): string { const product = this.products().find(item => item.id === id); return product ? `${product.code} - ${product.name}` : id; }


  private loadData(id: string): void {
    this.isLoading.set(true);
    forkJoin({ order: this.purchaseOrderService.getById(id), products: this.productService.getProducts(1, 100) }).pipe(finalize(() => this.isLoading.set(false))).subscribe({
      next: ({ order: result, products }) => {
        this.products.set(products.items);
        if (![PurchaseOrderStatus.Sent, PurchaseOrderStatus.Confirmed, PurchaseOrderStatus.Shipping].includes(result.status)) {
          this.notification.error('Đơn mua hàng không ở trạng thái cho phép nhận hàng');
          this.router.navigate(['/purchase-orders/detail', id]);
          return;
        }
        this.currentOrder.set(result);
        this.partModelToForm(result);
      },
      error: err => this.notification.error(getApiErrorMessage(err, 'Không thể tải dữ liệu nhận hàng')),
    })
  }
  partModelToForm(model: PurchaseOrderModel): void {
    this.receiveForm.patchValue({
      purchaseOrderId: model.id,
    });

    this.orderDetails.clear();
    model.orderDetailDto.forEach(item => {
      this.orderDetails.push(
        this.fb.group({
          purchaseOrderDetailId: [item.id, Validators.required],
          productId: [item.productId, Validators.required],
          orderedQuantity: [item.orderedQuantity, Validators.required],
          lots: this.fb.array<FormGroup>([this.createLotForm()], Validators.minLength(1))
        })
      );

    });
  }
  submit() {
    const hasDuplicateLots = this.orderDetails.controls.some((_, i) => this.getLots(i).controls.some((__, j) => this.isDuplicateLot(i, j)));
    if (this.receiveForm.invalid || hasDuplicateLots) { this.receiveForm.markAllAsTouched(); this.notification.warning(hasDuplicateLots ? 'Mã lô bị trùng trong cùng sản phẩm.' : 'Vui lòng kiểm tra các trường chưa hợp lệ.'); return; }
    const invalidQuantityIndex = this.orderDetails.controls.findIndex((_, index) => this.getDifference(index) !== 0);
    if (invalidQuantityIndex >= 0) {
      this.notification.error(`Tổng số lượng các lô của sản phẩm ${invalidQuantityIndex + 1} phải bằng số lượng đặt`);
      return;
    }
    this.setFormToModel();
    this.isSubmitting.set(true);
    this.purchaseOrderService.received(this.purchaseOrderReceiveModel).pipe(finalize(() => this.isSubmitting.set(false))).subscribe({
      next: () => { this.notification.success('Nhận đơn mua hàng và nhập kho thành công'); this.router.navigate(['/purchase-orders/detail', this.purchaseOrderId]); },
      error: err => this.notification.error(getApiErrorMessage(err, 'Không thể nhận đơn mua hàng')),
    });
  }
  setFormToModel(): void {
    const value = this.receiveForm.getRawValue();

    this.purchaseOrderReceiveModel.purchaseOrderId =
      value.purchaseOrderId;

    this.purchaseOrderReceiveModel.orderDetails =
      value.orderDetails.map(detail => ({
        purchaseOrderDetailId: detail['purchaseOrderDetailId'],
        lots: (detail['lots'] as Array<Record<string, unknown>>).map((lot): ReceivePurchaseOrderLotModel => ({
          batchNumber: String(lot['batchNumber']).trim(),
          receivedQuantity: Number(lot['receivedQuantity']),
          unitCost: Number(lot['unitCost']),
          manufactureDate: lot['manufactureDate']
            ? new Date(String(lot['manufactureDate']))
            : null,
          expiryDate: lot['expiryDate']
            ? new Date(String(lot['expiryDate']))
            : null,
          location: String(lot['location'] ?? '').trim() || null
        }))
      }));
  }
}
