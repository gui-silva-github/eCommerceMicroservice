using eCommerce.Core.DTO;
using FluentValidation;

namespace eCommerce.Core.Validators
{
    public class RegisterRequestValidator : AbstractValidator<RegisterRequest> 
    {
        public RegisterRequestValidator()
        {
            RuleFor(temp => temp.Email)
                .NotEmpty().WithMessage("Email é obrigatório.")
                .EmailAddress().WithMessage("Formato de email inválido.");

            RuleFor(temp => temp.Password)
                .NotEmpty().WithMessage("Senha é obrigatória.");

            RuleFor(request => request.PersonName)
                .NotEmpty().WithMessage("O nome da pessoa não pode ser vazio.")
                .Length(1, 50).WithMessage("O nome da pessoa deve possuir de 1 a 50 caracteres.");

            RuleFor(request => request.Gender)
                .IsInEnum().WithMessage("Gênero inválido.");
        }
    }
}
