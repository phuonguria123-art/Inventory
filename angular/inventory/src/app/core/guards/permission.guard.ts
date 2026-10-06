import { inject, Injectable } from "@angular/core";
import { ActivatedRouteSnapshot, Router, RouterStateSnapshot, UrlTree } from "@angular/router";
import { map, Observable, of, tap } from "rxjs";
import { PermissionService } from "../services/permission.service";

@Injectable({
    providedIn: "root",
})
export class PermissionGuard {
    protected readonly router = inject(Router);
    protected readonly permissionService = inject(PermissionService);
    canActivate(
        route: ActivatedRouteSnapshot,
        state: RouterStateSnapshot
    ): Observable<boolean | UrlTree> {
        let { requiredPolicy } = route.data || {};

        if (!requiredPolicy) return of(true);

        return this.permissionService.getGrantedPolicy$(requiredPolicy).pipe(
            map((hasPermission) => hasPermission ? true : this.router.createUrlTree(['/403'])
            ),
        )

    }
}