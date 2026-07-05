using eCommerce.OrdersMicroservice.BusinessLogicLayer.DTO;

namespace eCommerce.OrdersMicroservice.BusinessLogicLayer.ServiceContracts
{
    public interface IOrdersService
    {
        Task<List<OrderResponse?>> GetOrders();

        Task<OrderResponse?> GetOrderByOrderID(Guid orderID);

        Task<List<OrderResponse?>> GetOrdersByProductID(Guid productID);

        Task<List<OrderResponse?>> GetOrdersByUserID(Guid userID);

        Task<List<OrderResponse?>> GetOrdersByOrderDate(DateTime orderDate);

        Task<OrderResponse?> AddOrder(OrderAddRequest orderAddRequest);

        Task<OrderResponse?> UpdateOrder(Guid orderID, OrderUpdateRequest orderUpdateRequest);

        Task<bool> DeleteOrder(Guid orderID);
    }
}
