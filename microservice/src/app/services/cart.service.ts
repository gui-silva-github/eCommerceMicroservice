import { computed, inject, Injectable, signal } from '@angular/core';
import { CartItem } from '../models/cart-item';
import { ProductResponse } from '../models/product-response';

@Injectable({
  providedIn: 'root',
})
export class CartService {
  private readonly storageKey = 'ecommerce_cart';
  private readonly items = signal<CartItem[]>(this.loadFromStorage());

  readonly cartItems = this.items.asReadonly();
  readonly itemCount = computed(() => this.items().reduce((total, item) => total + item.quantity, 0));
  readonly totalAmount = computed(() =>
    this.items().reduce((total, item) => total + item.unitPrice * item.quantity, 0),
  );
  readonly isEmpty = computed(() => this.items().length === 0);
  readonly feedbackMessage = signal<string | null>(null);

  addProduct(product: ProductResponse): void {
    this.items.update((currentItems) => {
      const existingItem = currentItems.find((item) => item.productID === product.productID);

      if (existingItem) {
        return currentItems.map((item) =>
          item.productID === product.productID
            ? { ...item, quantity: item.quantity + 1 }
            : item,
        );
      }

      return [
        ...currentItems,
        {
          productID: product.productID,
          productName: product.productName,
          unitPrice: product.unitPrice,
          quantity: 1,
        },
      ];
    });

    this.persist();
    this.showFeedback(`${product.productName} adicionado ao carrinho`);
  }

  updateQuantity(productID: string, quantity: number): void {
    if (quantity <= 0) {
      this.removeItem(productID);
      return;
    }

    this.items.update((currentItems) =>
      currentItems.map((item) => (item.productID === productID ? { ...item, quantity } : item)),
    );
    this.persist();
  }

  removeItem(productID: string): void {
    this.items.update((currentItems) => currentItems.filter((item) => item.productID !== productID));
    this.persist();
  }

  clear(): void {
    this.items.set([]);
    this.persist();
  }

  private showFeedback(message: string): void {
    this.feedbackMessage.set(message);
    window.setTimeout(() => this.feedbackMessage.set(null), 2500);
  }

  private loadFromStorage(): CartItem[] {
    try {
      const stored = localStorage.getItem(this.storageKey);
      if (!stored) {
        return [];
      }

      const parsed = JSON.parse(stored) as CartItem[];
      return Array.isArray(parsed) ? parsed : [];
    } catch {
      return [];
    }
  }

  private persist(): void {
    localStorage.setItem(this.storageKey, JSON.stringify(this.items()));
  }
}
