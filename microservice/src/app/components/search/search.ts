import { Component, OnInit, inject, signal } from '@angular/core';
import { CurrencyPipe } from '@angular/common';
import { ProductsService } from '../../services/products.service';
import { ProductResponse } from '../../models/product-response';
import { MatButtonModule } from '@angular/material/button';
import { MatDividerModule } from '@angular/material/divider';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { UsersService } from '../../services/users.service';
import { CartService } from '../../services/cart.service';

@Component({
  selector: 'app-search',
  imports: [
    CurrencyPipe,
    MatCardModule,
    MatDividerModule,
    MatButtonModule,
    MatIconModule,
    RouterModule,
  ],
  templateUrl: './search.html',
  styleUrl: './search.css',
})
export class Search implements OnInit {
  private readonly productsService = inject(ProductsService);
  private readonly activatedRoute = inject(ActivatedRoute);
  readonly usersService = inject(UsersService);
  readonly cartService = inject(CartService);

  readonly products = signal<ProductResponse[]>([]);
  readonly searchTerm = signal('');
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly hasResults = () => this.products().length > 0;

  ngOnInit(): void {
    this.activatedRoute.paramMap.subscribe((params) => {
      const term = params.get('str') ?? '';
      this.searchTerm.set(term);
      this.loading.set(true);
      this.error.set(null);

      this.productsService.searchProducts(term).subscribe({
        next: (response) => {
          this.products.set(response);
          this.loading.set(false);
        },
        error: () => {
          this.products.set([]);
          this.loading.set(false);
          this.error.set('Não foi possível realizar a busca. Verifique se o serviço está ativo.');
        },
      });
    });
  }

  addToCart(product: ProductResponse): void {
    if (this.usersService.isAdmin) {
      return;
    }

    this.cartService.addProduct(product);
  }
}
