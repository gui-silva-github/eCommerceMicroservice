import { AppEnvironment } from './app/auth/app-environment';

/** Build apontando para o Ingress do AKS. */
export const environment: AppEnvironment = {
  production: true,
  apiUrl: 'YOUR_API_URL',
  usersApiUrl: 'YOUR_USERS_API_URL',
  productsMicroserviceUrl: 'YOUR_PRODUCTS_MICROSERVICE_URL',
  ordersMicroserviceUrl: 'YOUR_ORDERS_MICROSERVICE_URL',
  entra: null,
};
