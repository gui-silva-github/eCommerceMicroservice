import { OrderItemResponse } from './order-item-response';

export interface OrderResponse {
  orderID: string;
  userID: string;
  totalBill: number;
  orderDate: string;
  orderItems: OrderItemResponse[];
}
