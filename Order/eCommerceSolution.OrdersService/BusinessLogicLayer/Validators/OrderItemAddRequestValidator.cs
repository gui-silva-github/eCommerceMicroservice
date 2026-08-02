using eCommerce.OrdersMicroservice.BusinessLogicLayer.DTO;
using eCommerce.OrdersMicroservice.BusinessLogicLayer.HttpClients;
using FluentValidation;

namespace eCommerce.OrdersMicroservice.BusinessLogicLayer.Validators
{
    public class OrderItemAddRequestValidator : AbstractValidator<OrderItemAddRequest>
    {
        private readonly IProductsMicroserviceClient _productsMicroserviceClient;

        public OrderItemAddRequestValidator(IProductsMicroserviceClient productsMicroserviceClient)
        {
            _productsMicroserviceClient = productsMicroserviceClient;

            RuleFor(temp => temp.ProductID)
                .NotEmpty().WithMessage("Produto ID não pode ser vazio")
                .MustAsync(ProductExistsAsync).WithMessage("Produto ID inválido — produto não encontrado no ProductsService.");

            RuleFor(temp => temp.UnitPrice)
                .NotEmpty().WithMessage("Preço Unitário não pode ser vazio")
                .GreaterThan(0).WithMessage("Preço Unitário não pode ser menor ou igual a 0");

            RuleFor(temp => temp.Quantity)
                .NotEmpty().WithMessage("Quantidade não pode ser vazia")
                .GreaterThan(0).WithMessage("Quantidade não pode ser menor ou igual a 0");
        }

        private async Task<bool> ProductExistsAsync(Guid productID, CancellationToken cancellationToken)
        {
            ProductDTO? product = await _productsMicroserviceClient.GetProductByProductID(productID);
            return product != null;
        }
    }
}
