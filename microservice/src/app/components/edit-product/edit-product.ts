import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { UsersService } from '../../services/users.service';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
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
  getApiErrorMessage,
  parseApiError,
} from '../../utils/api-error.util';

@Component({
  selector: 'app-edit-product',
  imports: [
    ReactiveFormsModule,
    MatButtonModule,
    MatIconModule,
    RouterModule,
    MatCardModule,
    MatOptionModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
  ],
  templateUrl: './edit-product.html',
  styleUrl: './edit-product.css',
})
export class EditProduct implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly productsService = inject(ProductsService);
  private readonly router = inject(Router);
  private readonly activatedRoute = inject(ActivatedRoute);
  readonly usersService = inject(UsersService);

  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly saveError = signal<string | null>(null);
  readonly saving = signal(false);

  editProductForm: FormGroup = this.fb.group({
    productID: ['', Validators.required],
    productName: ['', Validators.required],
    category: ['', Validators.required],
    unitPrice: [0],
    quantityInStock: [0],
  });

  ngOnInit(): void {
    const productID = this.activatedRoute.snapshot.paramMap.get('productID') ?? '';

    if (!productID) {
      this.loading.set(false);
      this.error.set('ID do produto inválido.');
      return;
    }

    const cached = this.productsService.catalog().find((p) => p.productID === productID);
    if (cached) {
      this.fillForm(cached);
      this.loading.set(false);
    }

    this.productsService.getProductByProductID(productID).subscribe({
      next: (response) => {
        this.fillForm(response);
        this.loading.set(false);
        this.error.set(null);
      },
      error: () => {
        if (!cached) {
          this.loading.set(false);
          this.error.set('Produto não encontrado ou serviço indisponível.');
        }
      },
    });
  }

  update(): void {
    if (!this.editProductForm.valid || this.saving()) {
      return;
    }

    this.saving.set(true);
    this.saveError.set(null);
    clearApiFieldErrors(this.editProductForm);

    this.productsService.updateProduct(this.editProductForm.getRawValue()).subscribe({
      next: () => {
        this.saving.set(false);
        this.router.navigate(['/admin', 'products']);
      },
      error: (error) => {
        this.saving.set(false);

        const parsed = parseApiError(error);
        applyApiFieldErrors(this.editProductForm, parsed.errors, {
          ProductName: 'productName',
          Category: 'category',
          UnitPrice: 'unitPrice',
          QuantityInStock: 'quantityInStock',
        });
        this.saveError.set(getApiErrorMessage(error, 'Erro ao salvar as alterações. Tente novamente.'));
      },
    });
  }

  private fillForm(product: ProductResponse): void {
    this.editProductForm.patchValue({
      productID: product.productID,
      productName: product.productName,
      category: product.category,
      unitPrice: product.unitPrice,
      quantityInStock: product.quantityInStock,
    });
  }

  get productNameFormControl(): FormControl {
    return this.editProductForm.get('productName') as FormControl;
  }

  get categoryFormControl(): FormControl {
    return this.editProductForm.get('category') as FormControl;
  }
}
