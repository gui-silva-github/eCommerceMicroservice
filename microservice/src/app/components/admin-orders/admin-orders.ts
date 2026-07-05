import { Component, inject, OnInit } from '@angular/core';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { Router, RouterModule } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTableModule } from '@angular/material/table';
import { OrdersService } from '../../services/orders.service';
import { UsersService } from '../../services/users.service';

@Component({
  selector: 'app-admin-orders',
  imports: [
    CurrencyPipe,
    DatePipe,
    RouterModule,
    MatButtonModule,
    MatIconModule,
    MatTableModule,
  ],
  templateUrl: './admin-orders.html',
  styleUrl: './admin-orders.css',
})
export class AdminOrders implements OnInit {
  private readonly router = inject(Router);
  readonly ordersService = inject(OrdersService);
  readonly usersService = inject(UsersService);

  readonly displayedColumns = ['orderID', 'userID', 'orderDate', 'totalBill', 'items', 'actions'];

  ngOnInit(): void {
    if (!this.usersService.isAuthenticated || !this.usersService.isAdmin) {
      this.router.navigate(['/products', 'showcase']);
      return;
    }

    this.ordersService.loadAllOrders();
  }
}
