import { Injectable, computed, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map, tap } from 'rxjs';
import { environment } from '../../environment';
import { ProductResponse } from '../models/product-response';
import { ProductUpdateRequest } from '../models/product-update-request';
import { NewProductRequest } from '../models/new-product-request';

@Injectable({
  providedIn: 'root',
})
export class ProductsService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.productsMicroserviceUrl.replace(/\/$/, '');

  readonly catalog = signal<ProductResponse[]>([]);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly hasProducts = computed(() => this.catalog().length > 0);

  loadCatalog(): void {
    this.loading.set(true);
    this.error.set(null);

    this.http.get<unknown>(this.baseUrl).subscribe({
      next: (data) => {
        this.catalog.set(this.toProducts(data));
        this.loading.set(false);
      },
      error: () => {
        this.catalog.set([]);
        this.error.set('Não foi possível carregar os produtos. Verifique se o microserviço está ativo.');
        this.loading.set(false);
      },
    });
  }

  searchProducts(searchString: string): Observable<ProductResponse[]> {
    const term = encodeURIComponent(searchString ?? '');
    return this.http
      .get<unknown>(`${this.baseUrl}/search/${term}`)
      .pipe(map((data) => this.toProducts(data)));
  }

  getProductByProductID(productID: string): Observable<ProductResponse> {
    return this.http
      .get<unknown>(`${this.baseUrl}/search/product-id/${productID}`)
      .pipe(map((data) => this.toProduct(data as Record<string, unknown>)));
  }

  updateProduct(productUpdateRequest: ProductUpdateRequest): Observable<ProductResponse> {
    return this.http.put<unknown>(this.baseUrl, productUpdateRequest).pipe(
      map((data) => this.toProduct(data as Record<string, unknown>)),
      tap(() => this.loadCatalog()),
    );
  }

  deleteProduct(productID: string): Observable<boolean> {
    return this.http.delete<boolean>(`${this.baseUrl}/${productID}`).pipe(
      tap((deleted) => {
        if (deleted) {
          this.catalog.update((items) => items.filter((p) => p.productID !== productID));
        }
      }),
    );
  }

  createProduct(newProductRequest: NewProductRequest): Observable<ProductResponse> {
    return this.http.post<unknown>(this.baseUrl, newProductRequest).pipe(
      map((data) => this.toProduct(data as Record<string, unknown>)),
      tap(() => this.loadCatalog()),
    );
  }

  private toProducts(data: unknown): ProductResponse[] {
    if (!data) {
      return [];
    }

    if (!Array.isArray(data)) {
      return [this.toProduct(data as Record<string, unknown>)];
    }

    return data
      .filter((item) => item != null)
      .map((item) => this.toProduct(item as Record<string, unknown>));
  }

  private toProduct(raw: Record<string, unknown>): ProductResponse {
    return {
      productID: String(raw['productID'] ?? raw['ProductID'] ?? ''),
      productName: String(raw['productName'] ?? raw['ProductName'] ?? ''),
      category: String(raw['category'] ?? raw['Category'] ?? ''),
      unitPrice: Number(raw['unitPrice'] ?? raw['UnitPrice'] ?? 0),
      quantityInStock: Number(raw['quantityInStock'] ?? raw['QuantityInStock'] ?? 0),
    };
  }
}
