using AutoMapper;
using eCommerce.OrdersMicroservice.BusinessLogicLayer.DTO;
using eCommerce.OrdersMicroservice.BusinessLogicLayer.Exceptions;
using eCommerce.OrdersMicroservice.BusinessLogicLayer.ServiceContracts;
using eCommerce.OrdersMicroservice.DataAccessLayer.Entities;
using eCommerce.OrdersMicroservice.DataAccessLayer.RepositoryContracts;
using FluentValidation;
using FluentValidation.Results;

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

        public OrdersService(
            IOrdersRepository ordersRepository,
            IMapper mapper,
            IValidator<OrderAddRequest> orderAddRequestValidator,
            IValidator<OrderItemAddRequest> orderItemAddRequestValidator,
            IValidator<OrderUpdateRequest> orderUpdateRequestValidator,
            IValidator<OrderItemUpdateRequest> orderItemUpdateRequestValidator)
        {
            _orderAddRequestValidator = orderAddRequestValidator;
            _orderItemAddRequestValidator = orderItemAddRequestValidator;
            _orderUpdateRequestValidator = orderUpdateRequestValidator;
            _orderItemUpdateRequestValidator = orderItemUpdateRequestValidator;
            _mapper = mapper;
            _ordersRepository = ordersRepository;
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

            return _mapper.Map<OrderResponse>(addedOrder);
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
            
            return order != null ? _mapper.Map<OrderResponse>(order) : null;
        }

        public async Task<List<OrderResponse?>> GetOrders()
        {
            IEnumerable<Order?> orders = await _ordersRepository.GetOrders();
            IEnumerable<OrderResponse?> orderResponses = _mapper.Map<IEnumerable<OrderResponse?>>(orders);
            return orderResponses.ToList();
        }

        public async Task<List<OrderResponse?>> GetOrdersByOrderDate(DateTime orderDate)
        {
            IEnumerable<Order?> orders = await _ordersRepository.GetOrdersByOrderDate(orderDate);
            IEnumerable<OrderResponse?> orderResponses = _mapper.Map<IEnumerable<OrderResponse?>>(orders);
            return orderResponses.ToList();
        }

        public async Task<List<OrderResponse?>> GetOrdersByProductID(Guid productID)
        {
            IEnumerable<Order?> orders = await _ordersRepository.GetOrdersByProductID(productID);
            IEnumerable<OrderResponse?> orderResponses = _mapper.Map<IEnumerable<OrderResponse?>>(orders);
            return orderResponses.ToList();
        }

        public async Task<List<OrderResponse?>> GetOrdersByUserID(Guid userID)
        {
            IEnumerable<Order?> orders = await _ordersRepository.GetOrdersByUserID(userID);
            IEnumerable<OrderResponse?> orderResponses = _mapper.Map<IEnumerable<OrderResponse?>>(orders);
            return orderResponses.ToList();
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

            return updatedOrder is null
                ? null
                : _mapper.Map<OrderResponse>(updatedOrder);
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
