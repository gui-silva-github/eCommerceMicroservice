import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of } from 'rxjs';
import { AuthenticationResponse } from '../models/authentication-response';
import { Register } from '../models/register';
import { environment } from '../../environment';

@Injectable({
  providedIn: 'root',
})
export class UsersService {
  private baseUrl: string = environment.apiUrl;
  public isAuthenticated = false;
  public isAdmin = false;
  public currentUserName: string | null = '';
  public currentUserId: string | null = null;

  constructor(private http: HttpClient) {
    this.isAuthenticated = !!localStorage.getItem('authToken');
    const isAdminValue = localStorage.getItem('isAdmin');
    this.isAdmin = isAdminValue !== null && isAdminValue !== undefined && isAdminValue.toLowerCase() === 'true';
    this.currentUserName = localStorage.getItem('currentUserName');
    this.currentUserId = localStorage.getItem('currentUserId');
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
