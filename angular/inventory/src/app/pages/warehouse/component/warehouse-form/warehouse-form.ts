import { Component, inject } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { WarehouseService } from '../../service/warehouse.service';
import { WarehouseCreateModel } from '../../../../core/models/features-models/warehouse/warehouse-create.model';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { WarehouseUpdateModel } from '../../../../core/models/features-models/warehouse/warehouse-update.model';
import { WarehouseModel } from '../../../../core/models/features-models/warehouse/warehouse.model';

@Component({
  imports: [ReactiveFormsModule, RouterLink],
  selector: 'app-warehouse-form',
  styleUrl: './warehouse-form.scss',
  templateUrl: './warehouse-form.html',
})
export class WarehouseForm {
  private readonly warehouseService = inject(WarehouseService);
  createModel: WarehouseCreateModel = new WarehouseCreateModel();
  updateModel: WarehouseUpdateModel = new WarehouseUpdateModel();
  warehouseModel: WarehouseModel = new WarehouseModel();
  warehouseId: string | null = null;
  mode: 'create' | 'edit' | 'view' = 'create';

  warehouseForm = new FormGroup({
    name: new FormControl('', { nonNullable: true, validators: Validators.required }),
    code: new FormControl('', { nonNullable: true, validators: Validators.required }),
    phone: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.pattern(/^0[0-9]{9}$/)] }),
    address: new FormControl('', { nonNullable: true, validators: Validators.required }),
    description: new FormControl('', { nonNullable: true }),
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
      return;
    }

    this.mapFormToModel();

    if (this.warehouseId) {
      this.warehouseService.updateWarehouse(this.warehouseId, this.updateModel).subscribe({
        next: () => this.router.navigateByUrl('/warehouse'),
        error: (err) => console.error(err)
      });
    }
    else {
      this.warehouseService.createWarehouse(this.createModel).subscribe({
        next: (res) => this.router.navigate(['/warehouse/detail', res.id]),
        error: (err) => console.error(err)
      });
    }
  }

  getWarehouseDetail(id: string): void {
    this.warehouseService.getWarehouseById(id).subscribe({
      next: (res) => {
        this.warehouseModel = res;
        this.setModelToForm();
        if (this.mode === 'view') {
          this.warehouseForm.disable();
        }
      },
      error: (err) => console.error(err)
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
    const formValue = this.warehouseForm.getRawValue();

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
