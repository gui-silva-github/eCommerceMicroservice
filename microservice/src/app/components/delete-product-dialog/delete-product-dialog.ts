import { Component, inject } from '@angular/core';
import { CommonModule, CurrencyPipe } from '@angular/common';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { ProductResponse } from '../../models/product-response';
import { ProductsService } from '../../services/products.service';
import { getApiErrorMessage } from '../../utils/api-error.util';

export interface DeleteProductDialogData {
  product: ProductResponse;
}

@Component({
  selector: 'app-delete-product-dialog',
  imports: [CommonModule, MatDialogModule, MatButtonModule, MatIconModule, CurrencyPipe],
  templateUrl: './delete-product-dialog.html',
  styleUrl: './delete-product-dialog.css',
})
export class DeleteProductDialog {
  private readonly dialogRef = inject(MatDialogRef<DeleteProductDialog, boolean>);
  private readonly productsService = inject(ProductsService);
  readonly data = inject<DeleteProductDialogData>(MAT_DIALOG_DATA);

  isDeleting = false;
  errorMessage = '';

  cancel(): void {
    this.dialogRef.close(false);
  }

  confirm(): void {
    this.isDeleting = true;
    this.errorMessage = '';

    this.productsService.deleteProduct(this.data.product.productID).subscribe({
      next: (deleted) => {
        this.isDeleting = false;
        if (deleted) {
          this.dialogRef.close(true);
        } else {
          this.errorMessage = 'Não foi possível excluir o produto.';
        }
      },
      error: (error) => {
        this.isDeleting = false;
        this.errorMessage = getApiErrorMessage(error, 'Erro ao excluir o produto. Tente novamente.');
      },
    });
  }
}
