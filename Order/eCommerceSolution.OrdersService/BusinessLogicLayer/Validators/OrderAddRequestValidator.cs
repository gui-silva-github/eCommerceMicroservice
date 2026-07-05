using eCommerce.OrdersMicroservice.BusinessLogicLayer.DTO;
using FluentValidation;

namespace eCommerce.OrdersMicroservice.BusinessLogicLayer.Validators
{
    public class OrderAddRequestValidator : AbstractValidator<OrderAddRequest>
    {
        public OrderAddRequestValidator()
        {
            RuleFor(temp => temp.UserID)
                .NotEmpty().WithMessage("User ID não pode ser vazio");
            RuleFor(temp => temp.OrderDate)
                .NotEmpty().WithMessage("Data do Pedido não pode ser vazia");
            RuleFor(temp => temp.OrderItems)
                .NotEmpty().WithMessage("Itens do Pedido não pode ser vazio");
        }
    }
}
