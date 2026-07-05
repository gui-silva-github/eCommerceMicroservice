import { Component, inject } from '@angular/core';
import { CurrencyPipe } from '@angular/common';
import { ProductsService } from '../../services/products.service';
import { MatButtonModule } from '@angular/material/button';
import { MatDividerModule } from '@angular/material/divider';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { RouterModule } from '@angular/router';
import { UsersService } from '../../services/users.service';
import { ProductResponse } from '../../models/product-response';
import { CartService } from '../../services/cart.service';

@Component({
  selector: 'app-show-case',
  imports: [
    CurrencyPipe,
    MatCardModule,
    MatDividerModule,
    MatButtonModule,
    MatIconModule,
    RouterModule,
  ],
  templateUrl: './show-case.html',
  styleUrl: './show-case.css',
})
export class ShowCase {
  readonly productsService = inject(ProductsService);
  readonly usersService = inject(UsersService);
  readonly cartService = inject(CartService);

  constructor() {
    if (!this.productsService.hasProducts()) {
      this.productsService.loadCatalog();
    }
  }

  addToCart(product: ProductResponse): void {
    if (this.usersService.isAdmin) {
      return;
    }

    this.cartService.addProduct(product);
  }
}
