import { Injectable, signal } from '@angular/core';
import { Router } from '@angular/router';
import { Observable, tap } from 'rxjs';
import { AuthResponse } from '../models/collection.models';
import { AuthApiService } from './auth-api.service';

const TOKEN_KEY = 'pokedex_token';
const USERNAME_KEY = 'pokedex_username';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly _isAuthenticated = signal(this.hasToken());

  readonly isAuthenticated = this._isAuthenticated.asReadonly();

  constructor(
    private readonly authApi: AuthApiService,
    private readonly router: Router
  ) {}

  get token(): string | null {
    return sessionStorage.getItem(TOKEN_KEY);
  }

  get username(): string | null {
    return sessionStorage.getItem(USERNAME_KEY);
  }

  login(username: string, password: string): Observable<AuthResponse> {
    return this.authApi.login({ username, password }).pipe(
      tap(response => this.persistSession(response))
    );
  }

  register(username: string, password: string): Observable<AuthResponse> {
    return this.authApi.register({ username, password }).pipe(
      tap(response => this.persistSession(response))
    );
  }

  logout(): void {
    sessionStorage.removeItem(TOKEN_KEY);
    sessionStorage.removeItem(USERNAME_KEY);
    this._isAuthenticated.set(false);
    void this.router.navigate(['/']);
  }

  private persistSession(response: AuthResponse): void {
    sessionStorage.setItem(TOKEN_KEY, response.token);
    sessionStorage.setItem(USERNAME_KEY, response.username);
    this._isAuthenticated.set(true);
  }

  private hasToken(): boolean {
    return !!sessionStorage.getItem(TOKEN_KEY);
  }
}
