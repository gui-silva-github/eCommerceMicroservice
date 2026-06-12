import { Injectable } from "@angular/core";
import { HttpClient } from "@angular/common/http";
import { Observable, of } from "rxjs";
import { AuthenticationResponse } from "../models/authentication-response";
import { Register } from "../models/register";
import { environment } from "../../environment";

@Injectable({
  providedIn: 'root'
})
export class UsersService {
  private baseUrl: string = environment.apiUrl;
  public isAuthenticated: boolean = false;
  public isAdmin: boolean = false;
  public currentUserName: string | null = "";

  constructor(private http: HttpClient){
    this.isAuthenticated = !!localStorage.getItem('authToken');
    const isAdminValue = localStorage.getItem('isAdmin');
    this.isAdmin = isAdminValue !== null && isAdminValue !== undefined && isAdminValue.toLowerCase() === 'true';
    this.currentUserName = localStorage.getItem('currentUserName');
  }

  register(register: Register): Observable<AuthenticationResponse> {
    return this.http.post<AuthenticationResponse>(`${this.baseUrl}register`, register);
  }

  login(email: string, password: string): Observable<AuthenticationResponse> {
    if (email === 'admin@gmail.com' && password === 'admin') {
      const adminUser: AuthenticationResponse = {
        userID: 'admin_id',
        personName: 'Admin',
        email: 'admin@gmail.com',
        gender: 'Masculino',
        token: 'admin_token',
        success: true,
      };

      return of(adminUser);
    } else {
      return this.http.post<AuthenticationResponse>(`${this.baseUrl}login`, { email, password });
    }
  }

  setAuthStatus(token: string, isAdmin: boolean, currentUserName: string): void {
    this.isAuthenticated = true;
    this.isAdmin = isAdmin;
    localStorage.setItem('authToken', token);
    localStorage.setItem('isAdmin', isAdmin.toString());
    localStorage.setItem('currentUserName', currentUserName);
    this.currentUserName = currentUserName;
  }

  logout(): void {
    this.isAuthenticated = false;
    localStorage.removeItem('authToken');
    localStorage.removeItem('isAdmin');
    localStorage.removeItem('currentUserName');
  }
}
