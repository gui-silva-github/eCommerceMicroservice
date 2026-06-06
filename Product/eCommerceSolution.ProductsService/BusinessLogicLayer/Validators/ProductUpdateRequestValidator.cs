using eCommerce.BusinessLogicLayer.DTO;
using FluentValidation;

namespace eCommerce.BusinessLogicLayer.Validators
{
    public class ProductUpdateRequestValidator : AbstractValidator<ProductUpdateRequest>
    {
        public ProductUpdateRequestValidator()
        {
            RuleFor(temp => temp.ProductID)
                .NotEmpty().WithMessage("O ID do produto não deve ser vazio");
            RuleFor(temp => temp.ProductName)
                .NotEmpty().WithMessage("O nome não pode ser vazio");
            RuleFor(temp => temp.Category)
                .NotNull().WithMessage("A categoria não deve ser vazia")
                .IsInEnum().WithMessage("Categoria inválida");
            RuleFor(temp => temp.UnitPrice)
                .InclusiveBetween(0, double.MaxValue).WithMessage($"Preço Unitário deve estar entre 0 a {double.MaxValue}");
            RuleFor(temp => temp.QuantityInStock)
                .InclusiveBetween(0, int.MaxValue).WithMessage($"Quantity in Stock should between 0 to {int.MaxValue}");
        }
    }
}