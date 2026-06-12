import { Component, inject } from '@angular/core';
import { CurrencyPipe } from '@angular/common';
import { ProductResponse } from '../../models/product-response';
import { ProductsService } from '../../services/products.service';
import { UsersService } from '../../services/users.service';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { Router, RouterModule } from '@angular/router';
import { MatTableModule } from '@angular/material/table';
import { MatIconModule } from '@angular/material/icon';
import { MatDialog } from '@angular/material/dialog';
import { DeleteProductDialog } from '../delete-product-dialog/delete-product-dialog';

@Component({
  selector: 'app-products',
  imports: [
    CurrencyPipe,
    MatCardModule,
    MatButtonModule,
    RouterModule,
    MatTableModule,
    MatIconModule,
  ],
  templateUrl: './products.html',
  styleUrl: './products.css',
})
export class Products {
  private readonly router = inject(Router);
  private readonly dialog = inject(MatDialog);
  readonly productsService = inject(ProductsService);
  readonly usersService = inject(UsersService);

  displayedColumns: string[] = ['productName', 'category', 'unitPrice', 'quantityInStock', 'actions'];

  constructor() {
    if (!this.productsService.hasProducts()) {
      this.productsService.loadCatalog();
    }
  }

  edit(product: ProductResponse): void {
    this.router.navigate(['/products', 'edit', product.productID]);
  }

  delete(product: ProductResponse): void {
    this.dialog.open(DeleteProductDialog, {
      width: '460px',
      maxWidth: '95vw',
      panelClass: ['app-dialog', 'app-dialog--danger'],
      data: { product },
      autoFocus: false,
    });
  }

  getStockClass(quantity: number | null | undefined): string {
    if (!quantity || quantity <= 0) {
      return 'stock-badge stock-badge--out';
    }
    if (quantity <= 10) {
      return 'stock-badge stock-badge--low';
    }
    return 'stock-badge stock-badge--ok';
  }

  getStockLabel(quantity: number | null | undefined): string {
    if (!quantity || quantity <= 0) {
      return 'Sem estoque';
    }
    if (quantity <= 10) {
      return 'Estoque baixo';
    }
    return 'Em estoque';
  }

  getStockIcon(quantity: number | null | undefined): string {
    if (!quantity || quantity <= 0) {
      return 'error_outline';
    }
    if (quantity <= 10) {
      return 'warning_amber';
    }
    return 'check_circle';
  }
}
