import { Component, inject, OnInit, signal } from '@angular/core';
import { CurrencyPipe } from '@angular/common';
import { Router, RouterModule } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatDividerModule } from '@angular/material/divider';
import { MatIconModule } from '@angular/material/icon';
import { CartService } from '../../services/cart.service';
import { OrdersService } from '../../services/orders.service';
import { UsersService } from '../../services/users.service';
import { getApiErrorMessage } from '../../utils/api-error.util';

@Component({
  selector: 'app-cart',
  imports: [
    CurrencyPipe,
    RouterModule,
    MatButtonModule,
    MatCardModule,
    MatDividerModule,
    MatIconModule,
  ],
  templateUrl: './cart.html',
  styleUrl: './cart.css',
})
export class Cart implements OnInit {
  private readonly router = inject(Router);
  readonly cartService = inject(CartService);
  private readonly ordersService = inject(OrdersService);
  readonly usersService = inject(UsersService);

  readonly isSubmitting = signal(false);
  readonly checkoutError = signal<string | null>(null);

  ngOnInit(): void {
    if (!this.usersService.isAuthenticated || this.usersService.isAdmin) {
      this.router.navigate(['/auth', 'login']);
    }
  }

  increaseQuantity(productID: string, currentQuantity: number): void {
    this.cartService.updateQuantity(productID, currentQuantity + 1);
  }

  decreaseQuantity(productID: string, currentQuantity: number): void {
    this.cartService.updateQuantity(productID, currentQuantity - 1);
  }

  removeItem(productID: string): void {
    this.cartService.removeItem(productID);
  }

  checkout(): void {
    if (this.cartService.isEmpty() || this.isSubmitting()) {
      return;
    }

    const userId = this.usersService.currentUserId;
    if (!userId) {
      this.checkoutError.set('Usuário não identificado. Faça login novamente.');
      return;
    }

    this.isSubmitting.set(true);
    this.checkoutError.set(null);

    const request = {
      userID: userId,
      orderDate: new Date().toISOString(),
      orderItems: this.cartService.cartItems().map((item) => ({
        productID: item.productID,
        unitPrice: item.unitPrice,
        quantity: item.quantity,
      })),
    };

    this.ordersService.createOrder(request).subscribe({
      next: () => {
        this.isSubmitting.set(false);
        this.cartService.clear();
        this.router.navigate(['/orders']);
      },
      error: (error) => {
        this.isSubmitting.set(false);
        this.checkoutError.set(getApiErrorMessage(error, 'Não foi possível finalizar o pedido.'));
      },
    });
  }
}
