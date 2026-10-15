import { Component, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { ProductCreateModel } from '../../../../core/models/features-models/product/product-create.model';
import { ProductUpdateModel } from '../../../../core/models/features-models/product/product-update.model';
import { SupplierModel } from '../../../../core/models/features-models/supplier/supplier.model';
import { NotificationService } from '../../../../core/services/notification.service';
import { SupplierService } from '../../../supplier/service/supplier.service';
import { ProductService } from '../../service/product.service';
import { finalize } from 'rxjs';
import { getApiErrorMessage } from '../../../../core/helpers/api-error.helper';

@Component({
  selector: 'app-product-form',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './product-form.html',
  styleUrl: './product-form.scss',
})
export class ProductForm {
  private readonly productService = inject(ProductService);
  private readonly supplierService = inject(SupplierService);
  private readonly notification = inject(NotificationService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  readonly suppliers = signal<SupplierModel[]>([]);
  readonly isLoading = signal(false);
  readonly isSubmitting = signal(false);
  productId: string | null = null;
  mode: 'create' | 'edit' | 'view' = 'create';

  readonly productForm = new FormGroup({
    name: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.maxLength(200), Validators.pattern(/.*\S.*/)] }),
    code: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.maxLength(50), Validators.pattern(/.*\S.*/)] }),
    imgUrl: new FormControl('', { nonNullable: true, validators: [Validators.maxLength(2000), Validators.pattern(/^(https?:\/\/.*)?$/i)] }),
    origin: new FormControl('', { nonNullable: true, validators: Validators.maxLength(200) }),
    weight: new FormControl(0, { nonNullable: true, validators: [Validators.min(0)] }),
    unitPrice: new FormControl(0, { nonNullable: true, validators: [Validators.required, Validators.min(0)] }),
    supplierId: new FormControl('', { nonNullable: true }),
  });

  ngOnInit(): void {
    this.mode = this.route.snapshot.data['mode'] ?? 'create';
    this.loadSuppliers();

    if (this.mode !== 'create') {
      this.productId = this.route.snapshot.paramMap.get('id');
      if (this.productId) this.loadProduct(this.productId);
    }
  }

  onSubmit(): void {
    if (this.mode === 'view' || this.productForm.invalid) {
      this.productForm.markAllAsTouched();
      return;
    }

    const value = this.productForm.getRawValue();
    const request: ProductCreateModel | ProductUpdateModel = {
      name: value.name.trim(),
      code: value.code.trim(),
      imgUrl: value.imgUrl.trim() || null,
      origin: value.origin.trim() || null,
      weight: value.weight,
      unitPrice: value.unitPrice,
      supplierId: value.supplierId || null,
    };

    this.isSubmitting.set(true);

    if (this.productId) {
      this.productService.updateProduct(this.productId, request).pipe(finalize(() => this.isSubmitting.set(false))).subscribe({
        next: () => {
          this.notification.success('Cập nhật sản phẩm thành công');
          this.router.navigate(['/product']);
        },
        error: (error) => {
          this.notification.error(getApiErrorMessage(error, 'Không thể cập nhật sản phẩm'));
        },
      });
      return;
    }

    this.productService.createProduct(request).pipe(finalize(() => this.isSubmitting.set(false))).subscribe({
      next: (createdProduct) => {
        this.notification.success('Tạo sản phẩm thành công');
        this.router.navigate(['/product/detail', createdProduct.id]);
      },
      error: (error) => {
        this.notification.error(getApiErrorMessage(error, 'Không thể tạo sản phẩm'));
      },
    });
  }

  enableEdit(): void {
    if (this.productId) this.router.navigate(['/product/edit', this.productId]);
  }

  private loadProduct(id: string): void {
    this.isLoading.set(true);
    this.productService.getProductById(id).pipe(finalize(() => this.isLoading.set(false))).subscribe({
      next: (product) => {
        this.productForm.patchValue({
          name: product.name,
          code: product.code,
          imgUrl: product.imgUrl ?? '',
          origin: product.origin ?? '',
          weight: product.weight,
          unitPrice: product.unitPrice,
          supplierId: product.supplierId ?? '',
        });
        if (this.mode === 'view') this.productForm.disable();
      },
      error: error => this.notification.error(getApiErrorMessage(error, 'Không thể tải thông tin sản phẩm')),
    });
  }

  private loadSuppliers(): void {
    this.supplierService.getList().subscribe({
      next: (suppliers) => this.suppliers.set(suppliers.filter((supplier) => supplier.isActive)),
      error: error => this.notification.error(getApiErrorMessage(error, 'Không thể tải danh sách nhà cung cấp')),
    });
  }
}
