import { Component, inject, input, OnInit, signal } from '@angular/core';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { Router, RouterModule } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatTableModule } from '@angular/material/table';
import { OrdersService } from '../../services/orders.service';
import { UsersService } from '../../services/users.service';
import { OrderResponse } from '../../models/order-response';

@Component({
  selector: 'app-order-detail',
  imports: [
    CurrencyPipe,
    DatePipe,
    RouterModule,
    MatButtonModule,
    MatCardModule,
    MatIconModule,
    MatTableModule,
  ],
  templateUrl: './order-detail.html',
  styleUrl: './order-detail.css',
})
export class OrderDetail implements OnInit {
  readonly orderID = input.required<string>();

  private readonly router = inject(Router);
  readonly usersService = inject(UsersService);
  private readonly ordersService = inject(OrdersService);

  readonly order = signal<OrderResponse | null>(null);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly displayedColumns = ['productID', 'unitPrice', 'quantity', 'totalPrice'];

  ngOnInit(): void {
    if (!this.usersService.isAuthenticated) {
      this.router.navigate(['/auth', 'login']);
      return;
    }

    this.ordersService.getOrderById(this.orderID()).subscribe({
      next: (response) => {
        this.order.set(response);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Pedido não encontrado ou serviço indisponível.');
        this.loading.set(false);
      },
    });
  }
}
