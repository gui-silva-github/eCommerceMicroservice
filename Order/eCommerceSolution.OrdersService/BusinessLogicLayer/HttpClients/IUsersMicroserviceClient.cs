using eCommerce.OrdersMicroservice.BusinessLogicLayer.DTO;

namespace eCommerce.OrdersMicroservice.BusinessLogicLayer.HttpClients
{
    public interface IUsersMicroserviceClient
    {
        Task<UserDTO?> GetUserByUserID(Guid userID);
    }
}
