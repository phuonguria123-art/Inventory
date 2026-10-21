import { isPlatformBrowser } from "@angular/common";
import { inject, Injectable, PLATFORM_ID } from "@angular/core";

@Injectable({
    providedIn: 'root'
})
export class IdempotencyKeyService {
    private readonly platformId = inject(PLATFORM_ID);

    getOrCreate(
        operation: string,
        resourceId: string
    ): string {
        const storageKey = this.buildStorageKey(
            operation,
            resourceId
        );

        if (!isPlatformBrowser(this.platformId)) {
            return crypto.randomUUID();
        }

        const existingKey = sessionStorage.getItem(storageKey);

        if (existingKey) {
            return existingKey;
        }

        const newKey = crypto.randomUUID();

        sessionStorage.setItem(storageKey, newKey);

        return newKey;
    }

    complete(
        operation: string,
        resourceId: string
    ): void {
        if (!isPlatformBrowser(this.platformId)) {
            return;
        }

        sessionStorage.removeItem(
            this.buildStorageKey(operation, resourceId)
        );
    }

    reset(
        operation: string,
        resourceId: string
    ): string {
        this.complete(operation, resourceId);

        return this.getOrCreate(operation, resourceId);
    }

    private buildStorageKey(
        operation: string,
        resourceId: string
    ): string {
        return `idempotency:${operation}:${resourceId}`;
    }
}