import { Routes } from '@angular/router';
import { Register } from './components/register/register';
import { Login } from './components/login/login';
import { ShowCase } from './components/show-case/show-case';
import { Search } from './components/search/search';
import { Products } from './components/products/products';
import { EditProduct } from './components/edit-product/edit-product';
import { NewProduct } from './components/new-product/new-product';
import { ComingSoon } from './components/coming-soon/coming-soon';

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
  {
    path: 'cart',
    component: ComingSoon,
    data: {
      icon: 'shopping_cart',
      title: 'Carrinho',
      message: 'Seu carrinho de compras aparecerá aqui em breve.',
    },
  },
  {
    path: 'orders',
    component: ComingSoon,
    data: {
      icon: 'receipt_long',
      title: 'Pedidos',
      message: 'O histórico de pedidos ficará disponível nesta área.',
    },
  },
  { path: '**', redirectTo: 'products/showcase' },
];
