import { AppEnvironment } from './app/auth/app-environment';

/** Build/serve apontando para o API Gateway no Azure Container Apps. */
export const environment: AppEnvironment = {
  production: true,
  apiUrl:
    'YOUR_AZURE_GATEWAY_URL/api/Auth/',
  usersApiUrl:
    'YOUR_AZURE_GATEWAY_URL/api/Users',
  productsMicroserviceUrl:
    'YOUR_AZURE_PRODUCTS_MICROSERVICE_URL',
  ordersMicroserviceUrl:
    'YOUR_AZURE_ORDERS_MICROSERVICE_URL',
  entra: null,
};
