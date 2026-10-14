import { EnvironmentProviders, Provider } from '@angular/core';
import { HTTP_INTERCEPTORS } from '@angular/common/http';
import {
  MSAL_INSTANCE,
  MSAL_INTERCEPTOR_CONFIG,
  MsalBroadcastService,
  MsalInterceptor,
  MsalInterceptorConfiguration,
  MsalService,
} from '@azure/msal-angular';
import {
  BrowserCacheLocation,
  InteractionType,
  IPublicClientApplication,
  LogLevel,
  PublicClientApplication,
} from '@azure/msal-browser';
import { AppEnvironment, EntraExternalIdConfig } from './app-environment';

function createMsalInstance(entra: EntraExternalIdConfig): IPublicClientApplication {
  return new PublicClientApplication({
    auth: {
      clientId: entra.clientId,
      authority: entra.authority,
      knownAuthorities: [entra.knownAuthority],
      redirectUri: entra.redirectUri,
      postLogoutRedirectUri: entra.postLogoutRedirectUri,
    },
    cache: {
      // localStorage: a sessão sobrevive a F5 e a novas abas (mesmo comportamento do login antigo).
      cacheLocation: BrowserCacheLocation.LocalStorage,
    },
    system: {
      loggerOptions: {
        logLevel: LogLevel.Warning,
        piiLoggingEnabled: false,
        loggerCallback: (level, message) => {
          if (level === LogLevel.Error) {
            console.error(message);
          } else if (level === LogLevel.Warning) {
            console.warn(message);
          }
        },
      },
    },
  });
}

/**
 * Diz ao MsalInterceptor em quais URLs do APIM ele anexa "Authorization: Bearer <access token>".
 * O matching do MSAL Angular v5 é estrito e ancorado: use * no fim do caminho.
 * GET de produtos fica sem token (catálogo é público no APIM).
 */
function createInterceptorConfig(env: AppEnvironment, entra: EntraExternalIdConfig): MsalInterceptorConfiguration {
  const scopes = [entra.apiScope];
  const protectedResourceMap = new Map<string, Array<string | { httpMethod: string; scopes: string[] | null }> | null>([
    [env.usersApiUrl + '/me', scopes],
    [env.ordersMicroserviceUrl + '*', scopes],
    [
      env.productsMicroserviceUrl + '*',
      [
        { httpMethod: 'POST', scopes },
        { httpMethod: 'PUT', scopes },
        { httpMethod: 'DELETE', scopes },
      ],
    ],
  ]);

  return {
    interactionType: InteractionType.Redirect,
    protectedResourceMap,
  };
}

/** Providers do MSAL. Só são registrados quando environment.entra != null. */
export function provideEntraExternalId(env: AppEnvironment): Array<Provider | EnvironmentProviders> {
  const entra = env.entra;

  if (!entra) {
    return [];
  }

  return [
    { provide: MSAL_INSTANCE, useFactory: () => createMsalInstance(entra) },
    { provide: MSAL_INTERCEPTOR_CONFIG, useFactory: () => createInterceptorConfig(env, entra) },
    { provide: HTTP_INTERCEPTORS, useClass: MsalInterceptor, multi: true },
    MsalService,
    MsalBroadcastService,
  ];
}
