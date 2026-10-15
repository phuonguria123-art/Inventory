import { Component, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { WarehouseService } from '../../service/warehouse.service';
import { WarehouseCreateModel } from '../../../../core/models/features-models/warehouse/warehouse-create.model';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { WarehouseUpdateModel } from '../../../../core/models/features-models/warehouse/warehouse-update.model';
import { WarehouseModel } from '../../../../core/models/features-models/warehouse/warehouse.model';
import { NotificationService } from '../../../../core/services/notification.service';
import { finalize } from 'rxjs';
import { getApiErrorMessage } from '../../../../core/helpers/api-error.helper';

@Component({
  imports: [ReactiveFormsModule, RouterLink],
  selector: 'app-warehouse-form',
  styleUrl: './warehouse-form.scss',
  templateUrl: './warehouse-form.html',
})
export class WarehouseForm {
  private readonly warehouseService = inject(WarehouseService);
  private readonly notification = inject(NotificationService);
  readonly isLoading = signal(false);
  readonly isSubmitting = signal(false);
  createModel: WarehouseCreateModel = new WarehouseCreateModel();
  updateModel: WarehouseUpdateModel = new WarehouseUpdateModel();
  warehouseModel: WarehouseModel = new WarehouseModel();
  warehouseId: string | null = null;
  mode: 'create' | 'edit' | 'view' = 'create';

  warehouseForm = new FormGroup({
    name: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.maxLength(200), Validators.pattern(/.*\S.*/)] }),
    code: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.maxLength(50), Validators.pattern(/.*\S.*/)] }),
    phone: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.pattern(/^0[0-9]{9}$/)] }),
    address: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.maxLength(500), Validators.pattern(/.*\S.*/)] }),
    description: new FormControl('', { nonNullable: true, validators: Validators.maxLength(1000) }),
    capacity: new FormControl(0, { nonNullable: true, validators: [Validators.required, Validators.min(1)] }),
    isMain: new FormControl(false, { nonNullable: true })
  });

  constructor(
    private route: ActivatedRoute, private router: Router) { }

  ngOnInit(): void {
    this.mode = this.route.snapshot.data['mode'] ?? 'create';
    if (this.mode !== 'create') {
      this.warehouseId = this.route.snapshot.paramMap.get('id');
      if (this.warehouseId) {
        this.getWarehouseDetail(this.warehouseId);
      }
    }
  }

  onSubmit(): void {
    if (this.mode === 'view') {
      return;
    }

    if (this.warehouseForm.invalid) {
      this.warehouseForm.markAllAsTouched();
      this.notification.warning(
        'Vui lòng kiểm tra lại các trường bắt buộc.'
      );
      return;
    }

    this.mapFormToModel();
    this.isSubmitting.set(true);
    if (this.warehouseId) {
      this.updateWarehouse();
    }
    else {
      this.createWarehouse();
    }
  }
  private createWarehouse(): void {
    this.warehouseService.createWarehouse(this.createModel)
      .pipe(finalize(() => this.isSubmitting.set(false)))
      .subscribe({
        next: (res) => { this.notification.success('Tạo kho thành công'); this.router.navigate(['/warehouse/detail', res.id]); },
        error: (err) => this.notification.error(getApiErrorMessage(err, 'Không thể tạo kho'))
      });
  }

  private updateWarehouse(): void {
    if (this.warehouseId == null) {
      return;
    }
    this.warehouseService.updateWarehouse(this.warehouseId, this.updateModel)
      .pipe(finalize(() => this.isSubmitting.set(false)))
      .subscribe({
        next: () => { this.notification.success('Cập nhật kho thành công'); this.router.navigateByUrl('/warehouse'); },
        error: (err) => this.notification.error(getApiErrorMessage(err, 'Không thể cập nhật kho'))
      });
  }
  getWarehouseDetail(id: string): void {
    this.isLoading.set(true);
    this.warehouseService.getWarehouseById(id)
      .pipe(finalize(() => this.isLoading.set(false)))
      .subscribe({
        next: (warehouse) => {
          this.warehouseModel = warehouse;
          this.setModelToForm();
          if (this.mode === 'view') {
            this.warehouseForm.disable();
          }
        },
        error: error => {
          this.notification.error(
            getApiErrorMessage(
              error,
              'Không thể tải thông tin kho'
            )
          );

          this.router.navigate(['/warehouse']);
        },
      });
  }

  enableEdit(): void {
    if (this.warehouseId) {
      this.router.navigate(['/warehouse/edit', this.warehouseId]);
    }
  }

  private setModelToForm(): void {
    this.warehouseForm.patchValue({
      name: this.warehouseModel.name,
      phone: this.warehouseModel.phone,
      code: this.warehouseModel.code,
      capacity: this.warehouseModel.capacity,
      address: this.warehouseModel.address,
      description: this.warehouseModel.description,
      isMain: this.warehouseModel.isMain
    });
  }

  private mapFormToModel(): void {
    const value = this.warehouseForm.getRawValue();
    const formValue = { ...value, name: value.name.trim(), code: value.code.trim(), phone: value.phone.trim(), address: value.address.trim(), description: value.description.trim() };

    if (this.warehouseId) {
      this.updateModel = {
        ...formValue
      };
    }
    else {
      this.createModel = {
        ...formValue
      };
    }
  }
}
