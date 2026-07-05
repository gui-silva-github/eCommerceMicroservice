import { OrderItemAddRequest } from './order-item-add-request';

export interface OrderAddRequest {
  userID: string;
  orderDate: string;
  orderItems: OrderItemAddRequest[];
}
