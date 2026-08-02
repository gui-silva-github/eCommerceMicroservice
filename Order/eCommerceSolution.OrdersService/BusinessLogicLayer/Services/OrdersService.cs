using AutoMapper;
using eCommerce.OrdersMicroservice.BusinessLogicLayer.DTO;
using eCommerce.OrdersMicroservice.BusinessLogicLayer.Exceptions;
using eCommerce.OrdersMicroservice.BusinessLogicLayer.HttpClients;
using eCommerce.OrdersMicroservice.BusinessLogicLayer.ServiceContracts;
using eCommerce.OrdersMicroservice.DataAccessLayer.Entities;
using eCommerce.OrdersMicroservice.DataAccessLayer.RepositoryContracts;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;

namespace eCommerce.OrdersMicroservice.BusinessLogicLayer.Services
{
    public class OrdersService : IOrdersService
    {
        private readonly IValidator<OrderAddRequest> _orderAddRequestValidator;
        private readonly IValidator<OrderItemAddRequest> _orderItemAddRequestValidator;
        private readonly IValidator<OrderUpdateRequest> _orderUpdateRequestValidator;
        private readonly IValidator<OrderItemUpdateRequest> _orderItemUpdateRequestValidator;
        private readonly IMapper _mapper;
        private readonly IOrdersRepository _ordersRepository;
        private readonly IUsersMicroserviceClient _usersMicroserviceClient;
        private readonly IProductsMicroserviceClient _productsMicroserviceClient;
        private readonly ILogger<OrdersService> _logger;

        public OrdersService(
            IOrdersRepository ordersRepository,
            IMapper mapper,
            IValidator<OrderAddRequest> orderAddRequestValidator,
            IValidator<OrderItemAddRequest> orderItemAddRequestValidator,
            IValidator<OrderUpdateRequest> orderUpdateRequestValidator,
            IValidator<OrderItemUpdateRequest> orderItemUpdateRequestValidator,
            IUsersMicroserviceClient usersMicroserviceClient,
            IProductsMicroserviceClient productsMicroserviceClient,
            ILogger<OrdersService> logger)
        {
            _orderAddRequestValidator = orderAddRequestValidator;
            _orderItemAddRequestValidator = orderItemAddRequestValidator;
            _orderUpdateRequestValidator = orderUpdateRequestValidator;
            _orderItemUpdateRequestValidator = orderItemUpdateRequestValidator;
            _mapper = mapper;
            _ordersRepository = ordersRepository;
            _usersMicroserviceClient = usersMicroserviceClient;
            _productsMicroserviceClient = productsMicroserviceClient;
            _logger = logger;
        }

        public async Task<OrderResponse?> AddOrder(OrderAddRequest orderAddRequest)
        {
            if (orderAddRequest == null)
            {
                throw new ArgumentNullException(nameof(orderAddRequest));
            }

            await ValidateAsync(_orderAddRequestValidator, orderAddRequest);

            foreach (OrderItemAddRequest orderItemAddRequest in orderAddRequest.OrderItems)
            {
                await ValidateAsync(_orderItemAddRequestValidator, orderItemAddRequest);
            }

            Order orderInput = _mapper.Map<Order>(orderAddRequest);
            CalculateOrderTotals(orderInput);

            Order? addedOrder = await _ordersRepository.AddOrder(orderInput);

            if (addedOrder == null)
            {
                return null;
            }

            OrderResponse orderResponse = _mapper.Map<OrderResponse>(addedOrder);
            return await EnrichOrderResponseAsync(orderResponse);
        }

        public async Task<bool> DeleteOrder(Guid orderID)
        {
            Order? existingOrder = await _ordersRepository.GetOrderByOrderID(orderID);

            if (existingOrder == null)
            {
                return false;
            }

            return await _ordersRepository.DeleteOrder(orderID);
        }

        public async Task<OrderResponse?> GetOrderByOrderID(Guid orderID)
        {
            Order? order = await _ordersRepository.GetOrderByOrderID(orderID);

            if (order == null)
            {
                return null;
            }

            OrderResponse orderResponse = _mapper.Map<OrderResponse>(order);
            return await EnrichOrderResponseAsync(orderResponse);
        }

        public async Task<List<OrderResponse?>> GetOrders()
        {
            IEnumerable<Order?> orders = await _ordersRepository.GetOrders();
            return await EnrichOrdersAsync(orders);
        }

        public async Task<List<OrderResponse?>> GetOrdersByOrderDate(DateTime orderDate)
        {
            IEnumerable<Order?> orders = await _ordersRepository.GetOrdersByOrderDate(orderDate);
            return await EnrichOrdersAsync(orders);
        }

        public async Task<List<OrderResponse?>> GetOrdersByProductID(Guid productID)
        {
            IEnumerable<Order?> orders = await _ordersRepository.GetOrdersByProductID(productID);
            return await EnrichOrdersAsync(orders);
        }

        public async Task<List<OrderResponse?>> GetOrdersByUserID(Guid userID)
        {
            IEnumerable<Order?> orders = await _ordersRepository.GetOrdersByUserID(userID);
            return await EnrichOrdersAsync(orders);
        }

        public async Task<OrderResponse?> UpdateOrder(Guid orderId, OrderUpdateRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);

            if (orderId != request.OrderID)
                throw new BusinessException("O ID informado na rota deve ser igual ao ID enviado no corpo da requisição.");

            var existingOrder = await _ordersRepository.GetOrderByOrderID(request.OrderID);

            if (existingOrder is null)
                throw new BusinessException("Pedido não encontrado.");

            await ValidateAsync(_orderUpdateRequestValidator, request);

            foreach (var orderItem in request.OrderItems)
                await ValidateAsync(_orderItemUpdateRequestValidator, orderItem);

            var order = _mapper.Map<Order>(request);

            CalculateOrderTotals(order);

            var updatedOrder = await _ordersRepository.UpdateOrder(order);

            if (updatedOrder is null)
            {
                return null;
            }

            OrderResponse orderResponse = _mapper.Map<OrderResponse>(updatedOrder);
            return await EnrichOrderResponseAsync(orderResponse);
        }

        private async Task<List<OrderResponse?>> EnrichOrdersAsync(IEnumerable<Order?> orders)
        {
            List<OrderResponse?> enrichedOrders = new();

            foreach (Order? order in orders)
            {
                if (order == null)
                {
                    enrichedOrders.Add(null);
                    continue;
                }

                OrderResponse orderResponse = _mapper.Map<OrderResponse>(order);
                enrichedOrders.Add(await EnrichOrderResponseAsync(orderResponse));
            }

            return enrichedOrders;
        }

        /// <summary>
        /// Enriquece o pedido com dados do UsersService e ProductsService via HttpClient (comunicação síncrona).
        /// Falhas externas não bloqueiam a leitura — mantém IDs e deixa campos enriquecidos nulos.
        /// </summary>
        private async Task<OrderResponse> EnrichOrderResponseAsync(OrderResponse orderResponse)
        {
            string? personName = null;
            string? email = null;

            try
            {
                UserDTO? user = await _usersMicroserviceClient.GetUserByUserID(orderResponse.UserID);

                if (user != null)
                {
                    personName = user.PersonName;
                    email = user.Email;
                }
            }
            catch (ExternalServiceUnavailableException ex)
            {
                _logger.LogWarning(
                    ex,
                    "Não foi possível enriquecer usuário {UserID} do pedido {OrderID}.",
                    orderResponse.UserID,
                    orderResponse.OrderID);
            }

            List<OrderItemResponse> enrichedItems = new();

            foreach (OrderItemResponse item in orderResponse.OrderItems ?? new List<OrderItemResponse>())
            {
                string? productName = null;
                string? category = null;

                try
                {
                    ProductDTO? product = await _productsMicroserviceClient.GetProductByProductID(item.ProductID);

                    if (product != null)
                    {
                        productName = product.ProductName;
                        category = product.Category;
                    }
                }
                catch (ExternalServiceUnavailableException ex)
                {
                    _logger.LogWarning(
                        ex,
                        "Não foi possível enriquecer produto {ProductID} do pedido {OrderID}.",
                        item.ProductID,
                        orderResponse.OrderID);
                }

                enrichedItems.Add(item with
                {
                    ProductName = productName,
                    Category = category
                });
            }

            return orderResponse with
            {
                PersonName = personName,
                Email = email,
                OrderItems = enrichedItems
            };
        }

        private static void CalculateOrderTotals(Order order)
        {
            foreach (OrderItem orderItem in order.OrderItems)
            {
                orderItem.TotalPrice = orderItem.Quantity * orderItem.UnitPrice;
            }

            order.TotalBill = order.OrderItems.Sum(temp => temp.TotalPrice);
        }

        private static async Task ValidateAsync<T>(IValidator<T> validator, T instance)
        {
            ValidationResult validationResult = await validator.ValidateAsync(instance);

            if (!validationResult.IsValid)
            {
                throw new ValidationException(validationResult.Errors);
            }
        }
    }
}
