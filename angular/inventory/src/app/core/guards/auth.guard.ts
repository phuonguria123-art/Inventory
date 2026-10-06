import { Injectable } from "@angular/core";
import { ActivatedRouteSnapshot, CanActivate, Router, RouterStateSnapshot, UrlTree } from "@angular/router";
import { AuthService } from "../services/auth.service";

@Injectable({ providedIn: "root" })
export class AuthGuard implements CanActivate {
    constructor(
        private router: Router,
        private authservice: AuthService
    ) { }

    canActivate(route: ActivatedRouteSnapshot, state: RouterStateSnapshot): boolean | UrlTree {
        const currentUser = this.authservice.currentUserValue;
        if (currentUser && currentUser.accessToken) {
            return true;
        }
        // not logged in so redirect to login page with the return url
        return this.router.createUrlTree(['/login'], {
            queryParams: { returnUrl: state.url },
        });
    }
}
