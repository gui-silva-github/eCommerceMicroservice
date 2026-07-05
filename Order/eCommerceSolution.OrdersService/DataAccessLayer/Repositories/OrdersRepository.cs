using eCommerce.OrdersMicroservice.DataAccessLayer.Entities;
using eCommerce.OrdersMicroservice.DataAccessLayer.RepositoryContracts;
using MongoDB.Driver;

namespace eCommerce.OrdersMicroservice.DataAccessLayer.Repositories
{
    public class OrdersRepository : IOrdersRepository
    {
        private readonly IMongoCollection<Order> _orders;
        private readonly string collectionName = "orders";

        public OrdersRepository(IMongoDatabase mongoDatabase)
        {
            _orders = mongoDatabase.GetCollection<Order>(collectionName);
        }

        public async Task<Order?> AddOrder(Order order)
        {
            order.OrderID = Guid.NewGuid();
            order._id = order.OrderID;

            foreach (OrderItem orderItem in order.OrderItems)
            {
                orderItem._id = Guid.NewGuid();
            }

            await _orders.InsertOneAsync(order);
            return order;
        }

        public async Task<bool> DeleteOrder(Guid orderID)
        {
            FilterDefinition<Order> filter = Builders<Order>.Filter.Eq(temp => temp.OrderID, orderID);

            Order? existingOrder = (await _orders.FindAsync(filter)).FirstOrDefault();

            if (existingOrder == null)
            {
                return false;
            }

            DeleteResult deleteResult = await _orders.DeleteOneAsync(filter);

            return deleteResult.DeletedCount > 0;
        }

        public async Task<Order?> GetOrderByOrderID(Guid orderID)
        {
            FilterDefinition<Order> filter = Builders<Order>.Filter.Eq(temp => temp.OrderID, orderID);
            return (await _orders.FindAsync(filter)).FirstOrDefault();
        }

        public async Task<IEnumerable<Order>> GetOrders()
        {
            return (await _orders.FindAsync(Builders<Order>.Filter.Empty)).ToList();
        }

        public async Task<IEnumerable<Order?>> GetOrdersByProductID(Guid productID)
        {
            FilterDefinition<Order> filter = Builders<Order>.Filter.ElemMatch(
                temp => temp.OrderItems,
                Builders<OrderItem>.Filter.Eq(tempProduct => tempProduct.ProductID, productID));

            return (await _orders.FindAsync(filter)).ToList();
        }

        public async Task<IEnumerable<Order?>> GetOrdersByUserID(Guid userID)
        {
            FilterDefinition<Order> filter = Builders<Order>.Filter.Eq(temp => temp.UserID, userID);
            return (await _orders.FindAsync(filter)).ToList();
        }

        public async Task<IEnumerable<Order?>> GetOrdersByOrderDate(DateTime orderDate)
        {
            FilterDefinition<Order> filter = Builders<Order>.Filter.Eq(
                temp => temp.OrderDate.ToString("yyyy-MM-dd"),
                orderDate.ToString("yyyy-MM-dd"));

            return (await _orders.FindAsync(filter)).ToList();
        }

        public async Task<Order?> UpdateOrder(Order order)
        {
            FilterDefinition<Order> filter = Builders<Order>.Filter.Eq(temp => temp.OrderID, order.OrderID);

            Order? existingOrder = (await _orders.FindAsync(filter)).FirstOrDefault();

            if (existingOrder == null)
            {
                return null;
            }

            order._id = existingOrder._id;

            await _orders.ReplaceOneAsync(filter, order);

            return order;
        }
    }
}
