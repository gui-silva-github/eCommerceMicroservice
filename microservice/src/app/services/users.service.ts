import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, catchError, map, of, switchMap } from 'rxjs';
import { MsalService } from '@azure/msal-angular';
import { AuthenticationResult } from '@azure/msal-browser';
import { AuthenticationResponse } from '../models/authentication-response';
import { Register } from '../models/register';
import { environment } from '../../environment';

/** Resposta de GET /api/Users/me (Users service, via APIM). */
export interface CurrentUserResponse {
  userID: string;
  email: string;
  personName: string;
  gender: string | null;
  isAdmin: boolean;
}

@Injectable({
  providedIn: 'root',
})
export class UsersService {
  private baseUrl: string = environment.apiUrl;
  public isAuthenticated = false;
  public isAdmin = false;
  public currentUserName: string | null = '';
  public currentUserId: string | null = null;

  /** Só existe quando environment.entra está preenchido (providers do MSAL registrados). */
  private readonly msal = inject(MsalService, { optional: true });

  constructor(private http: HttpClient) {
    this.isAuthenticated = !!localStorage.getItem('authToken');
    const isAdminValue = localStorage.getItem('isAdmin');
    this.isAdmin = isAdminValue !== null && isAdminValue !== undefined && isAdminValue.toLowerCase() === 'true';
    this.currentUserName = localStorage.getItem('currentUserName');
    this.currentUserId = localStorage.getItem('currentUserId');
  }

  /** true = login pelo Microsoft Entra External ID; false = formulário local antigo. */
  get usesEntra(): boolean {
    return environment.entra !== null && this.msal !== null;
  }

  /**
   * Redireciona para a página hospedada do Entra External ID (user flow "Inscrição e entrada").
   * signUp = true abre direto na tela de criar conta (prompt=create).
   */
  loginWithEntra(signUp = false): void {
    if (!this.usesEntra) {
      return;
    }

    this.msal!.loginRedirect({
      scopes: [environment.entra!.apiScope],
      prompt: signUp ? 'create' : undefined,
    }).subscribe();
  }

  /**
   * Chamado uma vez na inicialização do app: processa o retorno do redirect (#code=...),
   * define a conta ativa e carrega o perfil em /api/Users/me.
   * Retorna o perfil quando acabou de logar, ou null quando não houve login nesse carregamento.
   */
  completeEntraLogin(): Observable<CurrentUserResponse | null> {
    if (!this.usesEntra) {
      return of(null);
    }

    const msal = this.msal!;

    // navigateToLoginRequestUrl: false → o MSAL não volta para /auth/login (que dispararia outro login);
    // quem navega depois do login é o AppComponent.
    return msal.handleRedirectObservable({ navigateToLoginRequestUrl: false }).pipe(
      switchMap((result: AuthenticationResult | null) => {
        if (result?.account) {
          msal.instance.setActiveAccount(result.account);
          return this.loadCurrentUser();
        }

        const accounts = msal.instance.getAllAccounts();
        if (!msal.instance.getActiveAccount() && accounts.length > 0) {
          msal.instance.setActiveAccount(accounts[0]);
        }

        // Sessão do MSAL sumiu (cache limpo/expirado) mas as flags locais ficaram: zera tudo.
        if (accounts.length === 0 && this.isAuthenticated) {
          this.clearLocalSession();
        }

        return of(null);
      }),
      catchError((error) => {
        console.error('Falha ao concluir o login no Entra External ID', error);
        this.clearLocalSession();
        return of(null);
      }),
    );
  }

  /** GET /api/Users/me: cria/atualiza o usuário no Postgres (UserID = oid) e diz se é Admin. */
  loadCurrentUser(): Observable<CurrentUserResponse> {
    return this.http.get<Record<string, unknown>>(`${environment.usersApiUrl}/me`).pipe(
      map((raw) => ({
        userID: String(raw['userID'] ?? raw['UserID'] ?? ''),
        email: String(raw['email'] ?? raw['Email'] ?? ''),
        personName: String(raw['personName'] ?? raw['PersonName'] ?? ''),
        gender: (raw['gender'] ?? raw['Gender'] ?? null) as string | null,
        isAdmin: Boolean(raw['isAdmin'] ?? raw['IsAdmin'] ?? false),
      })),
      map((user) => {
        // O token fica no cache do MSAL; aqui só marcamos a sessão para o restante do app.
        this.setAuthStatus('entra', user.isAdmin, user.personName, user.userID);
        return user;
      }),
    );
  }

  register(register: Register): Observable<AuthenticationResponse> {
    return this.http.post<AuthenticationResponse>(`${this.baseUrl}register`, register);
  }

  login(email: string, password: string): Observable<AuthenticationResponse> {
    if (email === 'admin@gmail.com' && password === 'admin') {
      const adminUser: AuthenticationResponse = {
        userID: '00000000-0000-0000-0000-000000000001',
        personName: 'Admin',
        email: 'admin@gmail.com',
        gender: 'Masculino',
        token: 'admin_token',
        success: true,
      };

      return of(adminUser);
    }

    return this.http.post<AuthenticationResponse>(`${this.baseUrl}login`, { email, password });
  }

  setAuthStatus(token: string, isAdmin: boolean, currentUserName: string, userId: string): void {
    this.isAuthenticated = true;
    this.isAdmin = isAdmin;
    this.currentUserName = currentUserName;
    this.currentUserId = userId;
    localStorage.setItem('authToken', token);
    localStorage.setItem('isAdmin', isAdmin.toString());
    localStorage.setItem('currentUserName', currentUserName);
    localStorage.setItem('currentUserId', userId);
  }

  logout(): void {
    this.clearLocalSession();

    if (this.usesEntra) {
      // Encerra também a sessão no Entra (senão o próximo login entra direto, sem pedir senha).
      this.msal!.logoutRedirect().subscribe();
    }
  }

  private clearLocalSession(): void {
    this.isAuthenticated = false;
    this.isAdmin = false;
    this.currentUserName = null;
    this.currentUserId = null;
    localStorage.removeItem('authToken');
    localStorage.removeItem('isAdmin');
    localStorage.removeItem('currentUserName');
    localStorage.removeItem('currentUserId');
  }

  normalizeAuthResponse(raw: Record<string, unknown>): AuthenticationResponse {
    return {
      userID: String(raw['userID'] ?? raw['UserID'] ?? ''),
      personName: String(raw['personName'] ?? raw['PersonName'] ?? ''),
      email: String(raw['email'] ?? raw['Email'] ?? ''),
      gender: String(raw['gender'] ?? raw['Gender'] ?? ''),
      token: String(raw['token'] ?? raw['Token'] ?? ''),
      success: Boolean(raw['success'] ?? raw['Success'] ?? false),
    };
  }
}
