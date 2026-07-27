import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { AuthService } from '../services/auth.service';

/** P1.4: attach Bearer and clear session on 401. */
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const token = auth.token;

  if (!token || !req.url.includes('/api/')) {
    return next(req);
  }

  // Do not logout on failed login/register — those endpoints return 401 for bad credentials.
  const isAuthCredentialRequest =
    req.url.includes('/api/auth/login') || req.url.includes('/api/auth/register');

  return next(
    req.clone({
      setHeaders: { Authorization: `Bearer ${token}` }
    })
  ).pipe(
    catchError((err: unknown) => {
      if (
        !isAuthCredentialRequest &&
        err instanceof HttpErrorResponse &&
        err.status === 401 &&
        auth.token
      ) {
        auth.logout();
      }
      return throwError(() => err);
    })
  );
};
