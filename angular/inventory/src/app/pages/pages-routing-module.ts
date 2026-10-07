import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { Product } from './product/product';
import { PermissionGuard } from '../core/guards/permission.guard';
import { Supplier } from './supplier/supplier';
import { Forbidden } from './forbidden/forbidden';
import { Warehouse } from './warehouse/warehouse';
import { WarehouseForm } from './warehouse/component/warehouse-form/warehouse-form';
import { ProductForm } from './product/component/product-form/product-form';
import { SupplierForm } from './supplier/component/supplier-form/supplier-form';

const routes: Routes = [
  {
    path: "product",
    component: Product,
    canActivate: [PermissionGuard],
    data: {
      requiredPolicy: 'product.read'
    }
  },
  {
    path: "product/create",
    component: ProductForm,
    canActivate: [PermissionGuard],
    data: { requiredPolicy: 'product.create', mode: 'create' }
  },
  {
    path: "product/edit/:id",
    component: ProductForm,
    canActivate: [PermissionGuard],
    data: { requiredPolicy: 'product.update', mode: 'edit' }
  },
  {
    path: "product/detail/:id",
    component: ProductForm,
    canActivate: [PermissionGuard],
    data: { requiredPolicy: 'product.read', mode: 'view' }
  },
  {
    path: "supplier",
    component: Supplier,
    canActivate: [PermissionGuard],
    data: { requiredPolicy: 'supplier.read' }
  },
  {
    path: "supplier/create",
    component: SupplierForm,
    canActivate: [PermissionGuard],
    data: { requiredPolicy: 'supplier.create', mode: 'create' }
  },
  {
    path: "supplier/edit/:id",
    component: SupplierForm,
    canActivate: [PermissionGuard],
    data: { requiredPolicy: 'supplier.update', mode: 'edit' }
  },
  {
    path: "supplier/detail/:id",
    component: SupplierForm,
    canActivate: [PermissionGuard],
    data: { requiredPolicy: 'supplier.read', mode: 'view' }
  },
  {
    path: "warehouse",
    component: Warehouse,
    canActivate: [PermissionGuard],
    data: {
      requiredPolicy: 'warehouse.read',
    },
  },
  {
    path: "warehouse/create",
    component: WarehouseForm,
    canActivate: [PermissionGuard],

    data: { requiredPolicy: 'warehouse.create', mode: 'create' }
  },
  {
    path: "warehouse/edit/:id",
    component: WarehouseForm,
    canActivate: [PermissionGuard],

    data: { requiredPolicy: 'warehouse.update', mode: 'edit' }
  },
  {
    path: "warehouse/detail/:id",
    component: WarehouseForm,
    canActivate: [PermissionGuard],
    data: { requiredPolicy: 'warehouse.read', mode: 'view' }
  },
  {
    path: "403",
    component: Forbidden
  }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule],
})
export class PagesRoutingModule { }
