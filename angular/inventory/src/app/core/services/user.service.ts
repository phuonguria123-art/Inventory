import { HttpClient } from "@angular/common/http";
import { inject, Injectable } from "@angular/core";
import { environment } from "../../../environments/environment";
import { RegisterModel } from "../models/account/register.model";
import { Observable } from "rxjs";
import { TokenModel } from "../models/account/token.model";
import { LoginModel } from "../models/account/login.model";
import { BrowserStorageService } from "./browser-storage.service";
import { UserProfile } from "../models/user/userProfile.model";

@Injectable({ providedIn: "root" })
export class UserService {
    private baseInternalUrl = "/api/Auth";
    private readonly storage = inject(BrowserStorageService);
    constructor(private http: HttpClient) { }

    getProfile(): Observable<UserProfile> {
        return this.http.get<UserProfile>(this.url(`${this.baseInternalUrl}/profile`));
    }

    register(user: RegisterModel): Observable<UserProfile> {
        console.log(user);
        return this.http.post<UserProfile>(this.url(`${this.baseInternalUrl}/register`), user);
    }
    remove = () => {
        this.storage.removeItem("user_info");
        this.storage.removeItem('currentUser');
        this.storage.removeItem('app_config');
    };

    url(s: string) {
        return environment.apis.default.url + s;
    }
}
