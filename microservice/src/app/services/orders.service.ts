import { computed, inject, Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { map, Observable, tap } from 'rxjs';
import { environment } from '../../environment';
import { OrderAddRequest } from '../models/order-add-request';
import { OrderItemResponse } from '../models/order-item-response';
import { OrderResponse } from '../models/order-response';

@Injectable({
  providedIn: 'root',
})
export class OrdersService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.ordersMicroserviceUrl.replace(/\/$/, '');

  readonly orders = signal<OrderResponse[]>([]);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly hasOrders = computed(() => this.orders().length > 0);

  getOrders(): Observable<OrderResponse[]> {
    return this.http.get<unknown>(this.baseUrl).pipe(map((data) => this.toOrders(data)));
  }

  getOrderById(orderID: string): Observable<OrderResponse> {
    return this.http
      .get<unknown>(`${this.baseUrl}/search/orderid/${orderID}`)
      .pipe(map((data) => this.toOrder(data as Record<string, unknown>)));
  }

  getOrdersByUserId(userID: string): Observable<OrderResponse[]> {
    return this.http
      .get<unknown>(`${this.baseUrl}/search/userid/${userID}`)
      .pipe(map((data) => this.toOrders(data)));
  }

  createOrder(request: OrderAddRequest): Observable<OrderResponse> {
    return this.http
      .post<unknown>(this.baseUrl, request)
      .pipe(map((data) => this.toOrder(data as Record<string, unknown>)));
  }

  loadOrdersByUser(userID: string): void {
    this.loading.set(true);
    this.error.set(null);

    this.getOrdersByUserId(userID).subscribe({
      next: (orders) => {
        this.orders.set(this.sortByDateDesc(orders));
        this.loading.set(false);
      },
      error: () => {
        this.orders.set([]);
        this.error.set('Não foi possível carregar os pedidos. Verifique se o microserviço de Orders está ativo.');
        this.loading.set(false);
      },
    });
  }

  loadAllOrders(): void {
    this.loading.set(true);
    this.error.set(null);

    this.getOrders().subscribe({
      next: (orders) => {
        this.orders.set(this.sortByDateDesc(orders));
        this.loading.set(false);
      },
      error: () => {
        this.orders.set([]);
        this.error.set('Não foi possível carregar os pedidos. Verifique se o microserviço de Orders está ativo.');
        this.loading.set(false);
      },
    });
  }

  private sortByDateDesc(orders: OrderResponse[]): OrderResponse[] {
    return [...orders].sort(
      (first, second) => new Date(second.orderDate).getTime() - new Date(first.orderDate).getTime(),
    );
  }

  private toOrders(data: unknown): OrderResponse[] {
    if (!data) {
      return [];
    }

    if (!Array.isArray(data)) {
      return [this.toOrder(data as Record<string, unknown>)];
    }

    return data
      .filter((item) => item != null)
      .map((item) => this.toOrder(item as Record<string, unknown>));
  }

  private toOrder(raw: Record<string, unknown>): OrderResponse {
    const orderItemsRaw = raw['orderItems'] ?? raw['OrderItems'] ?? [];

    return {
      orderID: String(raw['orderID'] ?? raw['OrderID'] ?? ''),
      userID: String(raw['userID'] ?? raw['UserID'] ?? ''),
      totalBill: Number(raw['totalBill'] ?? raw['TotalBill'] ?? 0),
      orderDate: String(raw['orderDate'] ?? raw['OrderDate'] ?? ''),
      orderItems: this.toOrderItems(orderItemsRaw),
    };
  }

  private toOrderItems(data: unknown): OrderItemResponse[] {
    if (!Array.isArray(data)) {
      return [];
    }

    return data.map((item) => {
      const raw = item as Record<string, unknown>;
      return {
        productID: String(raw['productID'] ?? raw['ProductID'] ?? ''),
        unitPrice: Number(raw['unitPrice'] ?? raw['UnitPrice'] ?? 0),
        quantity: Number(raw['quantity'] ?? raw['Quantity'] ?? 0),
        totalPrice: Number(raw['totalPrice'] ?? raw['TotalPrice'] ?? 0),
      };
    });
  }
}
