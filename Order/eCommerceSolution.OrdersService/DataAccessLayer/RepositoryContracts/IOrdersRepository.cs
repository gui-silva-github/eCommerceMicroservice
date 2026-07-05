using eCommerce.OrdersMicroservice.DataAccessLayer.Entities;
using MongoDB.Driver;

namespace eCommerce.OrdersMicroservice.DataAccessLayer.RepositoryContracts
{
    public interface IOrdersRepository
    {
        Task<IEnumerable<Order>> GetOrders();

        Task<Order?> GetOrderByOrderID(Guid orderID);

        Task<IEnumerable<Order?>> GetOrdersByProductID(Guid productID);

        Task<IEnumerable<Order?>> GetOrdersByUserID(Guid userID);

        Task<IEnumerable<Order?>> GetOrdersByOrderDate(DateTime orderDate);

        Task<Order?> AddOrder(Order order);

        Task<Order?> UpdateOrder(Order order);

        Task<bool> DeleteOrder(Guid orderID);
    }
}
