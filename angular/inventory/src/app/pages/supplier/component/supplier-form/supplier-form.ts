import { Component, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { SupplierCreateModel } from '../../../../core/models/features-models/supplier/supplier-create.model';
import { SupplierUpdateModel } from '../../../../core/models/features-models/supplier/supplier-update.model';
import { NotificationService } from '../../../../core/services/notification.service';
import { SupplierService } from '../../service/supplier.service';
import { getApiErrorMessage } from '../../../../core/helpers/api-error.helper';
import { finalize } from 'rxjs';

@Component({
  selector: 'app-supplier-form',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './supplier-form.html',
  styleUrl: './supplier-form.scss',
})
export class SupplierForm {
  private readonly supplierService = inject(SupplierService);
  private readonly notification = inject(NotificationService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  readonly isSubmitting = signal(false);
  readonly isLoading = signal(false);
  supplierId: string | null = null;
  mode: 'create' | 'edit' | 'view' = 'create';

  readonly supplierForm = new FormGroup({
    name: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.maxLength(200), Validators.pattern(/.*\S.*/)] }),
    code: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.maxLength(50), Validators.pattern(/.*\S.*/)] }),
    phone: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.pattern(/^[0-9+() .-]{8,20}$/)],
    }),
    email: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.email, Validators.maxLength(254)] }),
    address: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.maxLength(500), Validators.pattern(/.*\S.*/)] }),
    contactPerson: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.maxLength(200), Validators.pattern(/.*\S.*/)] }),
  });

  ngOnInit(): void {
    this.mode = this.route.snapshot.data['mode'] ?? 'create';
    if (this.mode !== 'create') {
      this.supplierId = this.route.snapshot.paramMap.get('id');
      if (this.supplierId) this.loadSupplier(this.supplierId);
    }
  }

  onSubmit(): void {
    if (this.mode === 'view' || this.supplierForm.invalid) {
      this.supplierForm.markAllAsTouched();
      return;
    }

    const value = this.supplierForm.getRawValue();
    const request: SupplierCreateModel | SupplierUpdateModel = {
      name: value.name.trim(),
      code: value.code.trim(),
      phone: value.phone.trim(),
      email: value.email.trim(),
      address: value.address.trim(),
      contactPerson: value.contactPerson.trim(),
    };

    this.isSubmitting.set(true);
    if (this.supplierId) {
      this.supplierService.updateSupplier(this.supplierId, request).pipe(finalize(() => this.isSubmitting.set(false))).subscribe({
        next: () => {
          this.notification.success('Cập nhật nhà cung cấp thành công');
          this.router.navigate(['/supplier']);
        },
        error: (error) => {
          this.notification.error(getApiErrorMessage(error, 'Không thể cập nhật nhà cung cấp'));
        },
      });
      return;
    }

    this.supplierService.createSupplier(request).pipe(finalize(() => this.isSubmitting.set(false))).subscribe({
      next: (createdSupplier) => {
        this.notification.success('Tạo nhà cung cấp thành công');
        this.router.navigate(['/supplier/detail', createdSupplier.id]);
      },
      error: (err) => {
        this.notification.error(getApiErrorMessage(err, 'Không thể tạo nhà cung cấp'));
      },
    });
  }

  enableEdit(): void {
    if (this.supplierId) this.router.navigate(['/supplier/edit', this.supplierId]);
  }

  private loadSupplier(id: string): void {
    this.isLoading.set(true);
    this.supplierService.getSupplierById(id).pipe(finalize(() => this.isLoading.set(false))).subscribe({
      next: (supplier) => {
        this.supplierForm.patchValue({
          name: supplier.name,
          code: supplier.code,
          phone: supplier.phone,
          email: supplier.email,
          address: supplier.address,
          contactPerson: supplier.contactPerson,
        });
        if (this.mode === 'view') this.supplierForm.disable();
      },
      error: error => this.notification.error(getApiErrorMessage(error, 'Không thể tải thông tin nhà cung cấp')),
    });
  }
}
