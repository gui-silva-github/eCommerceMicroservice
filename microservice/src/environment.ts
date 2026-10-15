import { AppEnvironment } from './app/auth/app-environment';

/** Ambiente padrão (Compose / build local). */
export const environment: AppEnvironment = {
  production: false,
  apiUrl: 'http://localhost:7010/api/Auth/',
  usersApiUrl: 'http://localhost:7010/api/Users',
  productsMicroserviceUrl: 'http://localhost:7010/api/products',
  ordersMicroserviceUrl: 'http://localhost:7010/api/Orders',
  entra: null,
};
