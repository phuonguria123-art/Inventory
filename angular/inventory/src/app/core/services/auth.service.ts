import { HttpClient } from "@angular/common/http";
import { BehaviorSubject, catchError, map, Observable, tap, throwError } from "rxjs";
import { environment } from "../../../environments/environment";
import { LoginModel } from "../models/account/login.model";
import { TokenModel } from "../models/account/token.model";
import { RefreshTokenRequestModel } from "../models/account/refreshTokenRequets.model";
import { inject, Injectable } from "@angular/core";
import { NotificationService } from "./notification.service";
import { BrowserStorageService } from "./browser-storage.service";
import { Router } from "@angular/router";
import { ApplicationConfigService } from "./application-config.service";

@Injectable({ providedIn: "root" })
export class AuthService {

    private readonly applicationConfigService = inject(ApplicationConfigService);
    private readonly notification = inject(NotificationService);
    private readonly storage = inject(BrowserStorageService);

    private currentUserSubject: BehaviorSubject<TokenModel | null> = new BehaviorSubject<TokenModel | null>(this.readCurrentUser());
    readonly currentUser$ = this.currentUserSubject.asObservable();
    constructor(
        private http: HttpClient,
        private router: Router
    ) {
    }
    private readCurrentUser(): TokenModel | null {
        const storedUser = this.storage.getItem('currentUser');

        if (!storedUser) {
            return null;
        }

        try {
            const user = JSON.parse(storedUser) as Partial<TokenModel>;
            if (!user.userId?.trim() || !user.accessToken?.trim() || !user.refreshToken?.trim()) {
                this.storage.removeItem('currentUser');
                return null;
            }
            if (
                typeof user.userId !== 'string' ||
                typeof user.accessToken !== 'string' ||
                typeof user.refreshToken !== 'string'
            ) {
                this.storage.removeItem('currentUser');
                return null;
            }

            return user as TokenModel;
        } catch {
            this.storage.removeItem('currentUser');
            return null;
        }
    }
    public get currentUserValue(): TokenModel | null {
        return this.currentUserSubject.value;
    }

    login(user: LoginModel): Observable<TokenModel> {
        return this.http
            .post<TokenModel>(
                this.url(`/api/Auth/login`),
                user
            )
            .pipe(
                map(response => {
                    this.setCurrentUser(response);
                    return response;
                })
            );
    }

    refreshToken(): Observable<TokenModel> {

        const user = this.currentUserValue;

        if (!user) {
            return throwError(
                () => new Error("User not found")
            );
        }

        const request: RefreshTokenRequestModel = {
            userId: user.userId,
            refreshToken: user.refreshToken
        };

        return this.http
            .post<TokenModel>(
                this.url('/api/Auth/refresh-token'),
                request
            )
            .pipe(
                tap(response => {
                    this.setCurrentUser(response);
                })
            );
    }

    setCurrentUser(user: TokenModel) {
        this.storage.setItem(
            "currentUser",
            JSON.stringify(user)
        );
        this.currentUserSubject.next(user);
    }

    tryRefreshToken() {
        return this.refreshToken().pipe(
            catchError(() => {

                this.logout();

                this.notification.error(
                    "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.",
                    "Lỗi"
                );

                return throwError(
                    () => new Error("Refresh token failed")
                );
            })
        );
    }

    logout() {
        this.storage.removeItem("currentUser");
        this.currentUserSubject.next(null);
        this.applicationConfigService.clear();
        this.router.navigateByUrl('/login');
    }

    url(s: string) {
        return environment.oAuthConfig.issuer + s;
    }
}
