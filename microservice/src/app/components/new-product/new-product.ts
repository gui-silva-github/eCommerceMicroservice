import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { UsersService } from '../../services/users.service';
import { Router, RouterModule } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatCardModule } from '@angular/material/card';
import { MatOptionModule } from '@angular/material/core';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { ProductsService } from '../../services/products.service';
import { ProductResponse } from '../../models/product-response';
import {
  applyApiFieldErrors,
  clearApiFieldErrors,
  getApiError,
  getApiErrorMessage,
  hasApiError,
  parseApiError,
} from '../../utils/api-error.util';

@Component({
  selector: 'app-new-product',
  imports: [
    CommonModule, ReactiveFormsModule, MatButtonModule, MatIconModule, MatToolbarModule, RouterModule, MatCardModule,
    MatOptionModule, MatFormFieldModule, MatInputModule, MatSelectModule,
  ],
  templateUrl: './new-product.html',
  styleUrl: './new-product.css',
})
export class NewProduct {
  readonly errorMessage = signal<string | null>(null);
  readonly isSubmitting = signal(false);

  newProductForm: FormGroup;

  constructor(private fb: FormBuilder, public usersService: UsersService, private productsService: ProductsService, private router: Router) {
    this.newProductForm = this.fb.group({
      productName: ['', [Validators.required]],
      category: ['', [Validators.required]],
      unitPrice: [''],
      quantityInStock: [''],
    });
  }

  create(): void {
    if (!this.newProductForm.valid || this.isSubmitting()) {
      this.newProductForm.markAllAsTouched();
      return;
    }

    this.isSubmitting.set(true);
    this.errorMessage.set(null);
    clearApiFieldErrors(this.newProductForm);

    this.productsService.createProduct(this.newProductForm.value).subscribe({
      next: (response: ProductResponse) => {
        this.isSubmitting.set(false);
        if (response) {
          this.router.navigate(['/admin', 'products']);
        }
      },
      error: (error) => {
        this.isSubmitting.set(false);

        const parsed = parseApiError(error);
        applyApiFieldErrors(this.newProductForm, parsed.errors, {
          ProductName: 'productName',
          Category: 'category',
          UnitPrice: 'unitPrice',
          QuantityInStock: 'quantityInStock',
        });
        this.errorMessage.set(getApiErrorMessage(error, 'Não foi possível criar o produto.'));
      },
    });
  }

  get productNameFormControl(): FormControl {
    return this.newProductForm.get('productName') as FormControl;
  }

  get categoryFormControl(): FormControl {
    return this.newProductForm.get('category') as FormControl;
  }

  get unitPriceFormControl(): FormControl {
    return this.newProductForm.get('unitPrice') as FormControl;
  }

  get quantityInStockFormControl(): FormControl {
    return this.newProductForm.get('quantityInStock') as FormControl;
  }

  protected readonly hasApiError = hasApiError;
  protected readonly getApiError = getApiError;
}
