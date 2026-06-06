using eCommerce.DataAccessLayer.Entities;
using System.Linq.Expressions;

namespace eCommerce.DataAccessLayer.RepositoryContracts
{
    public interface IProductsRepository
    {
        Task<IEnumerable<Product>> GetProducts();

        Task<IEnumerable<Product?>> GetProductsByCondition(Expression<Func<Product, bool>> conditionExpression);

        Task<Product?> GetProductByCondition(Expression<Func<Product, bool>> conditionExpression);

        Task<Product?> AddProduct(Product product);

        Task<Product?> UpdateProduct(Product product);

        Task<bool> DeleteProduct(Guid productID);
    }
}