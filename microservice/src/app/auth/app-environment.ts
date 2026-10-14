/**
 * Formato comum dos arquivos src/environment*.ts.
 * `entra: null` = login local antigo (Compose/ACA/AKS sem Entra).
 * `entra: {...}` = login pelo Microsoft Entra External ID + chamadas via Azure API Management.
 */
export interface EntraExternalIdConfig {
  /** Application (client) ID do app registration "ecommerce-spa". */
  clientId: string;
  /** https://<subdominio>.ciamlogin.com/ (subdomínio = parte antes de .onmicrosoft.com). */
  authority: string;
  /** Host do authority, ex.: <subdominio>.ciamlogin.com. */
  knownAuthority: string;
  /** Precisa estar cadastrada em Authentication > Single-page application. */
  redirectUri: string;
  postLogoutRedirectUri: string;
  /** Scope exposto pelo app registration "ecommerce-api": api://<api-client-id>/Ecommerce.Access */
  apiScope: string;
}

export interface AppEnvironment {
  production: boolean;
  apiUrl: string;
  usersApiUrl: string;
  productsMicroserviceUrl: string;
  ordersMicroserviceUrl: string;
  entra: EntraExternalIdConfig | null;
}
