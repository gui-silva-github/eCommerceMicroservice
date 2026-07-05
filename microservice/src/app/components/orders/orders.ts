import { Component, inject, OnInit } from '@angular/core';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { Router, RouterModule } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { OrdersService } from '../../services/orders.service';
import { UsersService } from '../../services/users.service';

@Component({
  selector: 'app-orders',
  imports: [
    CurrencyPipe,
    DatePipe,
    RouterModule,
    MatButtonModule,
    MatCardModule,
    MatIconModule,
  ],
  templateUrl: './orders.html',
  styleUrl: './orders.css',
})
export class Orders implements OnInit {
  private readonly router = inject(Router);
  readonly ordersService = inject(OrdersService);
  readonly usersService = inject(UsersService);

  ngOnInit(): void {
    if (!this.usersService.isAuthenticated || this.usersService.isAdmin) {
      this.router.navigate(['/auth', 'login']);
      return;
    }

    const userId = this.usersService.currentUserId;
    if (userId) {
      this.ordersService.loadOrdersByUser(userId);
    }
  }

  reloadOrders(): void {
    const userId = this.usersService.currentUserId;
    if (userId) {
      this.ordersService.loadOrdersByUser(userId);
    }
  }
}
