import { ApplicationConfig } from '@angular/core';
import { provideRouter, withComponentInputBinding } from '@angular/router';
import { provideHttpClient, withInterceptorsFromDi } from '@angular/common/http';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';

import { routes } from './app.routes';
import { environment } from '../environment';
import { provideEntraExternalId } from './auth/msal.config';

export const appConfig: ApplicationConfig = {
  providers: [
    provideRouter(routes, withComponentInputBinding()),
    // withInterceptorsFromDi: o MsalInterceptor é um HTTP_INTERCEPTORS baseado em classe.
    provideHttpClient(withInterceptorsFromDi()),
    provideAnimationsAsync(),
    ...provideEntraExternalId(environment),
  ],
};
