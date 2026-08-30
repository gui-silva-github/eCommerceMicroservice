using AutoMapper;
using eCommerce.BusinessLogicLayer.DTO;
using eCommerce.BusinessLogicLayer.Exceptions;
using eCommerce.BusinessLogicLayer.RabbitMQ;
using eCommerce.BusinessLogicLayer.ServiceContracts;
using eCommerce.DataAccessLayer.Entities;
using eCommerce.DataAccessLayer.RepositoryContracts;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace eCommerce.BusinessLogicLayer.Services
{
    public class ProductsService : IProductsService
    {
        private const string AllProductsCacheKey = "all";
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() }
        };

        private readonly IValidator<ProductAddRequest> _productAddRequestValidator;
        private readonly IValidator<ProductUpdateRequest> _productUpdateRequestValidator;
        private readonly IMapper _mapper;
        private readonly IProductsRepository _productsRepository;
        private readonly IDistributedCache _distributedCache;
        private readonly IRabbitMQPublisher _rabbitMQPublisher;
        private readonly ILogger<ProductsService> _logger;

        public ProductsService(
            IValidator<ProductAddRequest> productAddRequestValidator,
            IValidator<ProductUpdateRequest> productUpdateRequestValidator,
            IMapper mapper,
            IProductsRepository productsRepository,
            IDistributedCache distributedCache,
            IRabbitMQPublisher rabbitMQPublisher,
            ILogger<ProductsService> logger)
        {
            _productAddRequestValidator = productAddRequestValidator;
            _productUpdateRequestValidator = productUpdateRequestValidator;
            _mapper = mapper;
            _productsRepository = productsRepository;
            _distributedCache = distributedCache;
            _rabbitMQPublisher = rabbitMQPublisher;
            _logger = logger;
        }

        public async Task<ProductResponse?> AddProduct(ProductAddRequest productAddRequest)
        {
            if (productAddRequest == null)
            {
                throw new ArgumentNullException(nameof(productAddRequest));
            }

            await ValidateAsync(_productAddRequestValidator, productAddRequest);

            Product productInput = _mapper.Map<Product>(productAddRequest);
            Product? addedProduct = await _productsRepository.AddProduct(productInput);

            if (addedProduct == null)
            {
                return null;
            }

            ProductResponse addedProductResponse = _mapper.Map<ProductResponse>(addedProduct);
            await InvalidateProductCacheAsync(addedProductResponse.ProductID);
            PublishProductEvent(addedProductResponse, "created");
            return addedProductResponse;
        }

        public async Task<bool> DeleteProduct(Guid productID)
        {
            Product? existingProduct = await _productsRepository.GetProductByCondition(temp => temp.ProductID == productID);

            if (existingProduct == null)
            {
                return false;
            }

            bool isDeleted = await _productsRepository.DeleteProduct(productID);
            if (isDeleted)
            {
                await InvalidateProductCacheAsync(productID);
                PublishProductDeleted(productID);
            }

            return isDeleted;
        }

        public async Task<ProductResponse?> GetProductByCondition(Expression<Func<Product, bool>> conditionExpression)
        {
            Product? product = await _productsRepository.GetProductByCondition(conditionExpression);
            if (product == null)
            {
                return null;
            }

            ProductResponse productResponse = _mapper.Map<ProductResponse>(product);
            return productResponse;
        }

        public async Task<ProductResponse?> GetProductByProductID(Guid productID)
        {
            string cacheKey = $"{productID}";
            ProductResponse? cached = await TryGetCacheAsync<ProductResponse>(cacheKey);
            if (cached != null)
            {
                return cached;
            }

            ProductResponse? product = await GetProductByCondition(temp => temp.ProductID == productID);
            if (product != null)
            {
                await TrySetCacheAsync(cacheKey, product);
            }

            return product;
        }

        public async Task<List<ProductResponse?>> GetProducts()
        {
            List<ProductResponse?>? cached = await TryGetCacheAsync<List<ProductResponse?>>(AllProductsCacheKey);
            if (cached != null)
            {
                return cached;
            }

            IEnumerable<Product?> products = await _productsRepository.GetProducts();

            IEnumerable<ProductResponse?> productResponses = _mapper.Map<IEnumerable<ProductResponse>>(products);
            List<ProductResponse?> result = productResponses.ToList();
            await TrySetCacheAsync(AllProductsCacheKey, result);
            return result;
        }

        public async Task<List<ProductResponse?>> GetProductsByCondition(Expression<Func<Product, bool>> conditionExpression)
        {
            IEnumerable<Product?> products = await _productsRepository.GetProductsByCondition(conditionExpression);

            IEnumerable<ProductResponse?> productResponses = _mapper.Map<IEnumerable<ProductResponse?>>(products);
            return productResponses.ToList();
        }

        public async Task<ProductResponse?> UpdateProduct(ProductUpdateRequest productUpdateRequest)
        {
            Product? existingProduct = await _productsRepository.GetProductByCondition(temp => temp.ProductID == productUpdateRequest.ProductID);

            if (existingProduct == null)
            {
                throw new BusinessException("ID do produto inválido.");
            }

            await ValidateAsync(_productUpdateRequestValidator, productUpdateRequest);

            Product product = _mapper.Map<Product>(productUpdateRequest);

            Product? updatedProduct = await _productsRepository.UpdateProduct(product);

            ProductResponse? updatedProductResponse = _mapper.Map<ProductResponse>(updatedProduct);
            await InvalidateProductCacheAsync(productUpdateRequest.ProductID);
            if (updatedProductResponse != null)
            {
                PublishProductEvent(updatedProductResponse, "updated");
            }
            return updatedProductResponse;
        }

        private async Task<T?> TryGetCacheAsync<T>(string cacheKey)
        {
            try
            {
                string? cachedEntity = await _distributedCache.GetStringAsync(cacheKey);
                if (!string.IsNullOrWhiteSpace(cachedEntity))
                {
                    return JsonSerializer.Deserialize<T>(cachedEntity, JsonOptions);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Redis indisponível ao ler a chave {CacheKey}.", cacheKey);
            }

            return default;
        }

        private async Task TrySetCacheAsync<T>(string cacheKey, T value)
        {
            try
            {
                DistributedCacheEntryOptions options = new()
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(60)
                };

                await _distributedCache.SetStringAsync(
                    cacheKey,
                    JsonSerializer.Serialize(value, JsonOptions),
                    options);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Redis indisponível ao gravar a chave {CacheKey}.", cacheKey);
            }
        }

        private void PublishProductEvent(ProductResponse product, string eventType)
        {
            _rabbitMQPublisher.Publish(
                $"product.{eventType}",
                new ProductEventMessage(
                    product.ProductID,
                    product.ProductName,
                    product.Category.ToString(),
                    product.UnitPrice,
                    product.QuantityInStock,
                    eventType));
        }

        private void PublishProductDeleted(Guid productID)
        {
            _rabbitMQPublisher.Publish(
                "product.deleted",
                new ProductEventMessage(productID, null, null, null, null, "deleted"));
        }

        private async Task InvalidateProductCacheAsync(Guid productID)
        {
            try
            {
                await _distributedCache.RemoveAsync($"{productID}");
                await _distributedCache.RemoveAsync(AllProductsCacheKey);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Redis indisponível ao invalidar cache do produto {ProductID}.", productID);
            }
        }

        private static async Task ValidateAsync<T>(IValidator<T> validator, T instance)
        {
            ValidationResult validationResult = await validator.ValidateAsync(instance);

            if (!validationResult.IsValid)
            {
                throw new ValidationException(validationResult.Errors);
            }
        }
    }
}
