using eCommerce.BusinessLogicLayer.DTO;
using FluentValidation;

namespace eCommerce.BusinessLogicLayer.Validators
{
    public class ProductAddRequestValidator : AbstractValidator<ProductAddRequest>
    {
        public ProductAddRequestValidator()
        {
            RuleFor(temp => temp.ProductName)
                .NotEmpty().WithMessage("Nome não pode ser vazio");
            RuleFor(temp => temp.Category)
                .NotNull().WithMessage("Categoria não pode ser vazia")
                .IsInEnum().WithMessage("Categoria inválida");
            RuleFor(temp => temp.UnitPrice)
                .InclusiveBetween(0, double.MaxValue).WithMessage($"Preço Unitário deve estar entre 0 a {double.MaxValue}");
            RuleFor(temp => temp.QuantityInStock)
                .InclusiveBetween(0, int.MaxValue).WithMessage($"Quantidade em Estoque deve estar entre 0 a {int.MaxValue}");
        }
    }
}