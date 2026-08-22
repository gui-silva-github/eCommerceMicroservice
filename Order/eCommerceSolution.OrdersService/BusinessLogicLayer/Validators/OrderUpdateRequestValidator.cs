using eCommerce.OrdersMicroservice.BusinessLogicLayer.DTO;
using eCommerce.OrdersMicroservice.BusinessLogicLayer.HttpClients;
using FluentValidation;

namespace eCommerce.OrdersMicroservice.BusinessLogicLayer.Validators
{
    public class OrderUpdateRequestValidator : AbstractValidator<OrderUpdateRequest>
    {
        private readonly IUsersMicroserviceClient _usersMicroserviceClient;

        public OrderUpdateRequestValidator(IUsersMicroserviceClient usersMicroserviceClient)
        {
            _usersMicroserviceClient = usersMicroserviceClient;

            RuleFor(temp => temp.OrderID)
                .NotEmpty().WithMessage("Pedido ID não pode ser vazio");

            RuleFor(temp => temp.UserID)
                .NotEmpty().WithMessage("User ID não pode ser vazio")
                .MustAsync(UserExistsAsync).WithMessage("User ID inválido: usuário não encontrado no UsersService.");

            RuleFor(temp => temp.OrderDate)
                .NotEmpty().WithMessage("Data do Pedido não pode ser vazia");

            RuleFor(temp => temp.OrderItems)
                .NotEmpty().WithMessage("Itens do Pedido não pode ser vazio");
        }

        private async Task<bool> UserExistsAsync(Guid userID, CancellationToken cancellationToken)
        {
            UserDTO? user = await _usersMicroserviceClient.GetUserByUserID(userID);
            return user != null;
        }
    }
}
