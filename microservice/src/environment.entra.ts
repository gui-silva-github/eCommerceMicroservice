import { AppEnvironment } from './app/auth/app-environment';

/**
 * Login pelo Microsoft Entra External ID + todas as chamadas pelo Azure API Management.
 * Preencha com os valores anotados no roteiro docs/roteiro-entra-external-id-apim.md:
 *   <APIM>          → nome do API Management (ex.: apim-ecommerce-lab)
 *   <SUBDOMINIO>    → parte antes de .onmicrosoft.com do tenant externo (ex.: harborclientes)
 *   <SPA_CLIENT_ID> → Application (client) ID do app registration ecommerce-spa
 *   <API_CLIENT_ID> → Application (client) ID do app registration ecommerce-api
 * Serve local: npm run start:entra  | Build do nginx (AKS): --build-arg BUILD_CONFIGURATION=entra-prod
 */
const apimBase = 'https://<APIM>.azure-api.net/ecommerce';
const appOrigin = 'http://localhost:4200';

export const environment: AppEnvironment = {
  production: true,
  apiUrl: `${apimBase}/api/Auth/`,
  usersApiUrl: `${apimBase}/api/Users`,
  productsMicroserviceUrl: `${apimBase}/api/products`,
  ordersMicroserviceUrl: `${apimBase}/api/Orders`,
  entra: {
    clientId: '<SPA_CLIENT_ID>',
    authority: 'https://<SUBDOMINIO>.ciamlogin.com/',
    knownAuthority: '<SUBDOMINIO>.ciamlogin.com',
    redirectUri: `${appOrigin}/`,
    postLogoutRedirectUri: `${appOrigin}/`,
    apiScope: 'api://<API_CLIENT_ID>/Ecommerce.Access',
  },
};
