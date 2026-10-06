import { HttpErrorResponse, HttpEvent, HttpHandler, HttpInterceptor, HttpRequest } from "@angular/common/http";
import { Injectable } from "@angular/core";
import { AuthService } from "../services/auth.service";
import { catchError, finalize, Observable, shareReplay, switchMap, throwError } from "rxjs";
import { environment } from "../../../environments/environment";
import { TokenModel } from "../models/account/token.model";

@Injectable()
export class AuthInterceptor implements HttpInterceptor {
  private refreshRequest$?: Observable<TokenModel>;

  constructor(private authService: AuthService) { }

  intercept(
    request: HttpRequest<unknown>,
    next: HttpHandler
  ): Observable<HttpEvent<unknown>> {
    const requestUrl = request.url.toLowerCase();
    const apiUrl = environment.oAuthConfig.issuer.toLowerCase();

    const isApiRequest =
      requestUrl.startsWith(apiUrl);

    const isLoginRequest =
      requestUrl.includes('/api/auth/login');

    const isRegisterRequest =
      requestUrl.includes('/api/auth/register');

    const isRefreshRequest =
      requestUrl.includes('/api/auth/refresh-token');

    const isPublicAuthRequest =
      isLoginRequest ||
      isRegisterRequest ||
      isRefreshRequest;

    const currentUser =
      this.authService.currentUserValue;

    if (
      isApiRequest &&
      !isPublicAuthRequest &&
      currentUser?.accessToken
    ) {
      request = this.addAccessToken(
        request,
        currentUser.accessToken
      );
    }

    return next.handle(request).pipe(
      catchError((error: HttpErrorResponse) => {
        if (error.status !== 401) {
          return throwError(() => error);
        }

        // Không xử lý 401 của API bên ngoài.
        if (!isApiRequest) {
          return throwError(() => error);
        }

        // Sai tài khoản hoặc mật khẩu thì trả lỗi về form login.
        if (isLoginRequest || isRegisterRequest) {
          return throwError(() => error);
        }

        // Refresh token cũng hết hạn hoặc không hợp lệ.
        if (isRefreshRequest) {
          this.authService.logout();
          return throwError(() => error);
        }

        return this.refreshAndRetry(request, next);
      })
    );
  }

  private refreshAndRetry(
    request: HttpRequest<unknown>,
    next: HttpHandler
  ): Observable<HttpEvent<unknown>> {
    /*
     * Chỉ request 401 đầu tiên tạo refreshRequest$.
     * Những request 401 tiếp theo dùng lại cùng Observable.
     */
    if (!this.refreshRequest$) {
      this.refreshRequest$ = this.authService
        .tryRefreshToken()
        .pipe(
          /*
           * Đặt finalize trước shareReplay để chỉ reset khi
           * request refresh thực sự kết thúc.
           */
          finalize(() => {
            this.refreshRequest$ = undefined;
          }),

          /*
           * Chia sẻ kết quả cho tất cả request đang chờ.
           * API refresh chỉ được gọi một lần.
           */
          shareReplay({
            bufferSize: 1,
            refCount: false,
          })
        );
    }

    return this.refreshRequest$.pipe(
      switchMap((newUser) => {
        if (!newUser.accessToken) {
          return throwError(
            () => new Error('Access token not found')
          );
        }

        const retryRequest = this.addAccessToken(
          request,
          newUser.accessToken
        );

        return next.handle(retryRequest);
      })
    );
  }

  private addAccessToken<T>(
    request: HttpRequest<T>,
    accessToken: string
  ): HttpRequest<T> {
    return request.clone({
      setHeaders: {
        Authorization: `Bearer ${accessToken}`,
      },
    });
  }
}