/** Build/serve apontando para o API Gateway no Azure Container Apps. */
export const environment = {
  production: true,
  apiUrl:
    'YOUR_AZURE_GATEWAY_URL/api/Auth/',
  productsMicroserviceUrl:
    'YOUR_AZURE_PRODUCTS_MICROSERVICE_URL',
  ordersMicroserviceUrl:
    'YOUR_AZURE_ORDERS_MICROSERVICE_URL',
};
