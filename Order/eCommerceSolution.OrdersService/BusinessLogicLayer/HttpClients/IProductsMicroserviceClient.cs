using eCommerce.OrdersMicroservice.BusinessLogicLayer.DTO;

namespace eCommerce.OrdersMicroservice.BusinessLogicLayer.HttpClients
{
    public interface IProductsMicroserviceClient
    {
        Task<ProductDTO?> GetProductByProductID(Guid productID);
    }
}
