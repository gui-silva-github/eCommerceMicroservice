import { Routes } from '@angular/router';
import { Register } from './components/register/register';
import { Login } from './components/login/login';
import { ShowCase } from './components/show-case/show-case';
import { Search } from './components/search/search';
import { Products } from './components/products/products';
import { EditProduct } from './components/edit-product/edit-product';
import { NewProduct } from './components/new-product/new-product';
import { Cart } from './components/cart/cart';
import { Orders } from './components/orders/orders';
import { OrderDetail } from './components/order-detail/order-detail';
import { AdminOrders } from './components/admin-orders/admin-orders';

export const routes: Routes = [
  { path: '', redirectTo: 'products/showcase', pathMatch: 'full' },
  { path: 'auth/register', component: Register },
  { path: 'auth/login', component: Login },
  { path: 'products/showcase', component: ShowCase },
  { path: 'products/search/:str', component: Search },
  { path: 'products/search', component: Search },
  { path: 'products/create', component: NewProduct },
  { path: 'products/edit/:productID', component: EditProduct },
  { path: 'admin/products', component: Products },
  { path: 'admin/orders', component: AdminOrders },
  { path: 'cart', component: Cart },
  { path: 'orders', component: Orders },
  { path: 'orders/:orderID', component: OrderDetail },
  { path: '**', redirectTo: 'products/showcase' },
];
