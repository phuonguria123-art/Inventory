import { Injectable } from "@angular/core";
import { ApplicationConfigService } from "./application-config.service";
import { map } from "rxjs";

@Injectable({
    providedIn: "root",
})
export class PermissionService {
    constructor(private config: ApplicationConfigService) { }

    getGrantedPolicy$(key: string) {
        return this.getStream().pipe(
            map((grantedPolicies: any) => this.isPolicyGranted(key, grantedPolicies))
        );
    }

    getGrantedPolicy(key: string | undefined) {
        const policies = this.getSnapshot();
        return this.isPolicyGranted(key, policies);
    }
    protected isPolicyGranted(
        key: string | undefined,
        grantedPolicies: string[]
    ) {
        if (!key) return true;

        const orRegexp = /\|\|/g;
        const andRegexp = /&&/g;

        // TODO: Allow combination of ANDs & ORs
        if (orRegexp.test(key)) {
            const keys = key.split("||").filter(Boolean);

            if (keys.length < 2) return false;

            return keys.some((k) => this.getPolicy(k.trim(), grantedPolicies));
        } else if (andRegexp.test(key)) {
            const keys = key.split("&&").filter(Boolean);

            if (keys.length < 2) return false;

            return keys.every((k) => this.getPolicy(k.trim(), grantedPolicies));
        }

        return this.getPolicy(key, grantedPolicies);
    }

    protected getStream() {
        return this.config.getAll$().pipe(map(this.mapToPolicies));
    }

    protected getSnapshot() {
        return this.mapToPolicies(this.config.getAll());
    }

    protected mapToPolicies(applicationConfiguration: any) {
        return applicationConfiguration?.permissions || [];
    }

    protected getPolicy(key: string, grantedPolicies: string[]) {
        return grantedPolicies.includes(key) || false;
    }
}