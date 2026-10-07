import { Routes } from '@angular/router';
import { Register } from './auth/register/register';
import { Login } from './auth/login/login';
import { AuthGuard } from './core/guards/auth.guard';
import { Layout } from './layout/layout';
import { Product } from './pages/product/product';
import { Supplier } from './pages/supplier/supplier';

export const routes: Routes = [
  {
    path: 'login',
    component: Login,
  },
  {
    path: 'register',
    component: Register,
  },
  {
    path: '',
    component: Layout,
    canActivate: [AuthGuard],
    children: [
      {
        path: '',
        loadChildren: () => import('./pages/pages-module').then((m) => m.PagesModule),
      },
    ]
  }

];
