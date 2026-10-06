import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { Product } from './product/product';
import { PermissionGuard } from '../core/guards/permission.guard';
import { Supplier } from './supplier/supplier';
import { Forbidden } from './forbidden/forbidden';

const routes: Routes = [
  {
    path: "",
    component: Product,
    canActivate: [PermissionGuard],
    data: {
      requiredPolicy: 'product.read'
    }
  },
  {
    path: "supplier",
    component: Supplier,

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
