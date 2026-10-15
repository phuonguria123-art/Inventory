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
import { Inventory } from './inventory/inventory';
import { InventoryTransaction } from './inventory/inventory-transaction/inventory-transaction';
import { Receive } from './inventory/component/receive/receive';
import { Issue } from './inventory/component/issue/issue';
import { Transfer } from './inventory/component/transfer/transfer';
import { Adjust } from './inventory/component/adjust/adjust';
import { Dashboard } from './dashboard/dashboard';
import { Reserve } from './inventory/component/reserve/reserve';
import { PurchaseOrder } from './purchase-order/purchase-order';
import { PurchaseOrderForm } from './purchase-order/components/purchase-order-form/purchase-order-form';
import { PurchaseOrderReceive } from './purchase-order/components/purchase-order-receive/purchase-order-receive';

const routes: Routes = [
  {
    path: "",
    component: Dashboard,
    canActivate: [PermissionGuard],
    data: { requiredPolicy: 'inventory.read' },
    pathMatch: 'full'
  },
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
    path: "inventory",
    component: Inventory,
    canActivate: [PermissionGuard],
    data: { requiredPolicy: 'inventory.read' }
  },
  {
    path: "transactions",
    component: InventoryTransaction,
    canActivate: [PermissionGuard],
    data: { requiredPolicy: 'inventory.read' }
  },
  {
    path: "inventory/receive",
    component: Receive,
    canActivate: [PermissionGuard],
    data: { requiredPolicy: 'inventory.receive' }
  },
  {
    path: "inventory/issue",
    component: Issue,
    canActivate: [PermissionGuard],
    data: { requiredPolicy: 'inventory.issue' }
  },
  {
    path: "inventory/transfer",
    component: Transfer,
    canActivate: [PermissionGuard],
    data: { requiredPolicy: 'inventory.transfer' }
  },
  {
    path: "inventory/adjust",
    component: Adjust,
    canActivate: [PermissionGuard],
    data: { requiredPolicy: 'inventory.adjust' }
  },
  {
    path: "inventory/reserve",
    component: Reserve,
    canActivate: [PermissionGuard],
    data: { requiredPolicy: 'inventory.issue' }
  },
  {
    path: "purchase-orders",
    component: PurchaseOrder,
    canActivate: [PermissionGuard],
    data: { requiredPolicy: 'order.read' }
  },
  {
    path: 'purchase-orders/create',
    component: PurchaseOrderForm,
    canActivate: [PermissionGuard],
    data: { requiredPolicy: 'order.create' }
  },
  {
    path: 'purchase-orders/detail/:id',
    component: PurchaseOrderForm,
    canActivate: [PermissionGuard],
    data: { requiredPolicy: 'order.read', mode: 'view' }
  },
  {
    path: 'purchase-orders/edit/:id',
    component: PurchaseOrderForm,
    canActivate: [PermissionGuard],
    data: { requiredPolicy: 'order.update', mode: 'edit' }
  },
  {
    path: 'purchase-orders/receive/:id',
    component: PurchaseOrderReceive,
    canActivate: [PermissionGuard],
    data: { requiredPolicy: 'order.receive' }

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
