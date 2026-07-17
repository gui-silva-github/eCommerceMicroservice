using eCommerce.OrdersMicroservice.BusinessLogicLayer.DTO;
using eCommerce.OrdersMicroservice.BusinessLogicLayer.ServiceContracts;
using Microsoft.AspNetCore.Mvc;

namespace eCommerce.OrdersMicroservice.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrdersController : ControllerBase
    {
        private readonly IOrdersService _ordersService;

        public OrdersController(IOrdersService ordersService)
        {
            _ordersService = ordersService;
        }

        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<OrderResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Get()
        {
            List<OrderResponse?> orders = await _ordersService.GetOrders();
            return Ok(orders);
        }

        [HttpGet("search/orderid/{orderID}")]
        [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetOrderByID(Guid orderID)
        {
            OrderResponse? order = await _ordersService.GetOrderByOrderID(orderID);

            return order is null
                ? NotFound(new ApiErrorResponse("Pedido não encontrado.", type: "NotFound"))
                : Ok(order);
        }

        [HttpGet("search/productid/{productID}")]
        [ProducesResponseType(typeof(IEnumerable<OrderResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetOrdersByProductID(Guid productID)
        {
            List<OrderResponse?> orders = await _ordersService.GetOrdersByProductID(productID);
            return Ok(orders);
        }

        [HttpGet("search/userid/{userID}")]
        [ProducesResponseType(typeof(IEnumerable<OrderResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetOrdersByUserID(Guid userID)
        {
            List<OrderResponse?> orders = await _ordersService.GetOrdersByUserID(userID);
            return Ok(orders);
        }

        [HttpGet("search/orderDate/{orderDate}")]
        [ProducesResponseType(typeof(IEnumerable<OrderResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetOrdersByOrderDate(DateTime orderDate)
        {
            List<OrderResponse?> orders = await _ordersService.GetOrdersByOrderDate(orderDate);
            return Ok(orders);
        }

        [HttpPost]
        [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Post([FromBody] OrderAddRequest orderAddRequest)
        {
            OrderResponse? orderResponse = await _ordersService.AddOrder(orderAddRequest);

            if (orderResponse == null)
            {
                return Problem("Erro ao adicionar pedido!");
            }

            return Created($"api/Orders/search/orderid/{orderResponse.OrderID}", orderResponse);
        }

        [HttpPut("{orderID}")]
        [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Put(Guid orderID, [FromBody] OrderUpdateRequest orderUpdateRequest)
        {
            OrderResponse? orderResponse = await _ordersService.UpdateOrder(orderID, orderUpdateRequest);

            if (orderResponse == null)
            {
                return Problem("Erro ao atualizar pedido!");
            }

            return Ok(orderResponse);
        }

        [HttpDelete("{orderID}")]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(Guid orderID)
        {
            bool isDeleted = await _ordersService.DeleteOrder(orderID);

            if (!isDeleted)
            {
                return NotFound(new ApiErrorResponse("Pedido não encontrado.", Type: "NotFound"));
            }

            return Ok(isDeleted);
        }
    }
}
