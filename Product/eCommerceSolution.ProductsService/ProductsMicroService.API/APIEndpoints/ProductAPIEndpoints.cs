using eCommerce.BusinessLogicLayer.DTO;
using eCommerce.BusinessLogicLayer.ServiceContracts;

namespace eCommerce.ProductsMicroService.API.APIEndpoints
{
    public static class ProductAPIEndpoints
    {
        public static IEndpointRouteBuilder MapProductAPIEndpoints(this IEndpointRouteBuilder app)
        {
            app.MapGet("/api/products", async (IProductsService productsService) =>
            {
                List<ProductResponse?> products = await productsService.GetProducts();
                return Results.Ok(products);
            })
            .WithTags("Products");

            app.MapGet("/api/products/search/product-id/{ProductID:guid}", async (IProductsService productsService, Guid ProductID) =>
            {
                ProductResponse? product = await productsService.GetProductByCondition(temp => temp.ProductID == ProductID);

                if (product == null)
                {
                    return Results.NotFound(new ApiErrorResponse("Produto não encontrado.", Type: "NotFound"));
                }

                return Results.Ok(product);
            })
            .WithTags("Products");

            app.MapGet("/api/products/search/{SearchString}", async (IProductsService productsService, string SearchString) =>
            {
                string search = SearchString.ToLower();

                List<ProductResponse?> productsByProductName = await productsService.GetProductsByCondition(
                    temp => temp.ProductName != null && temp.ProductName.ToLower().Contains(search));

                List<ProductResponse?> productsByCategory = await productsService.GetProductsByCondition(
                    temp => temp.Category != null && temp.Category.ToLower().Contains(search));

                var products = productsByProductName.Union(productsByCategory);

                return Results.Ok(products);
            })
            .WithTags("Products");

            app.MapPost("/api/products", async (IProductsService productsService, ProductAddRequest productAddRequest) =>
            {
                var addedProductResponse = await productsService.AddProduct(productAddRequest);

                if (addedProductResponse != null)
                {
                    return Results.Created($"/api/products/search/product-id/{addedProductResponse.ProductID}", addedProductResponse);
                }

                return Results.Problem("Erro ao adicionar produto");
            })
            .WithTags("Products");

            app.MapPut("/api/products", async (IProductsService productsService, ProductUpdateRequest productUpdateRequest) =>
            {
                var updatedProductResponse = await productsService.UpdateProduct(productUpdateRequest);

                if (updatedProductResponse != null)
                {
                    return Results.Ok(updatedProductResponse);
                }

                return Results.Problem("Erro ao atualizar produto");
            })
            .WithTags("Products");

            app.MapDelete("/api/products/{ProductID:guid}", async (IProductsService productsService, Guid ProductID) =>
            {
                bool isDeleted = await productsService.DeleteProduct(ProductID);

                if (isDeleted)
                {
                    return Results.Ok(true);
                }

                return Results.NotFound(new ApiErrorResponse("Produto não encontrado.", Type: "NotFound"));
            })
            .WithTags("Products");

            return app;
        }
    }
}
