import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { catchError, throwError } from 'rxjs';

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      // 401 is handled by auth.interceptor (logout + redirect).
      // Here we just normalize a message every feature service can surface.
      const message = resolveErrorMessage(error);
      console.error(`[HTTP ${error.status}] ${req.method} ${req.url}:`, message);
      return throwError(() => ({ ...error, friendlyMessage: message }));
    }),
  );
};

function resolveErrorMessage(error: HttpErrorResponse): string {
  if (error.status === 0) {
    return 'Cannot reach the server. Check your connection or that the API is running.';
  }
  if (error.status === 403) {
    return "You don't have permission to do that.";
  }
  if (error.status === 404) {
    return 'The requested resource was not found.';
  }
  if (error.status >= 500) {
    return 'Something went wrong on the server. Please try again shortly.';
  }
  return error.error?.message ?? 'An unexpected error occurred.';
}
