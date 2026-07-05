using eCommerce.OrdersMicroservice.BusinessLogicLayer.DTO;
using FluentValidation;

namespace eCommerce.OrdersMicroservice.BusinessLogicLayer.Validators
{
    public class OrderItemUpdateRequestValidator : AbstractValidator<OrderItemUpdateRequest>
    {
        public OrderItemUpdateRequestValidator()
        {
            RuleFor(temp => temp.ProductID)
                .NotEmpty().WithMessage("Produto ID não pode ser vazio");
            RuleFor(temp => temp.UnitPrice)
                .NotEmpty().WithMessage("Preço Unitário não pode ser vazio")
                .GreaterThan(0).WithMessage("Preço Unitário não pode ser menor ou igual a 0");
            RuleFor(temp => temp.Quantity)
                .NotEmpty().WithMessage("Quantidade não pode ser vazia")
                .GreaterThan(0).WithMessage("Quantidade não pode ser menor ou igual a zero");
        }
    }
}
