import { Component, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { SupplierCreateModel } from '../../../../core/models/features-models/supplier/supplier-create.model';
import { SupplierUpdateModel } from '../../../../core/models/features-models/supplier/supplier-update.model';
import { NotificationService } from '../../../../core/services/notification.service';
import { SupplierService } from '../../service/supplier.service';

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
  supplierId: string | null = null;
  mode: 'create' | 'edit' | 'view' = 'create';

  readonly supplierForm = new FormGroup({
    name: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    code: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    phone: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.pattern(/^[0-9+() .-]{8,20}$/)],
    }),
    email: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.email] }),
    address: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    contactPerson: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
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
      this.supplierService.updateSupplier(this.supplierId, request).subscribe({
        next: () => {
          this.isSubmitting.set(false);
          this.notification.success('Cập nhật nhà cung cấp thành công');
          this.router.navigate(['/supplier']);
        },
        error: () => {
          this.isSubmitting.set(false);
          this.notification.error('Không thể cập nhật nhà cung cấp');
        },
      });
      return;
    }

    this.supplierService.createSupplier(request).subscribe({
      next: (createdSupplier) => {
        this.isSubmitting.set(false);
        this.notification.success('Tạo nhà cung cấp thành công');
        this.router.navigate(['/supplier/detail', createdSupplier.id]);
      },
      error: () => {
        this.isSubmitting.set(false);
        this.notification.error('Không thể tạo nhà cung cấp');
      },
    });
  }

  enableEdit(): void {
    if (this.supplierId) this.router.navigate(['/supplier/edit', this.supplierId]);
  }

  private loadSupplier(id: string): void {
    this.supplierService.getSupplierById(id).subscribe({
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
      error: () => this.notification.error('Không thể tải thông tin nhà cung cấp'),
    });
  }
}
