import { inject, Injectable } from '@angular/core';
import { UserService } from './user.service';
import { AppConfig } from '../models/user/config.model';
import { BrowserStorageService } from './browser-storage.service';
import { BehaviorSubject, map, Observable, tap } from 'rxjs';

@Injectable({
  providedIn: 'root',
})
export class ApplicationConfigService {
  private static readonly storageKey = 'app_config';

  private readonly userService = inject(UserService);
  private readonly storage = inject(BrowserStorageService);
  private readonly configSubject = new BehaviorSubject<AppConfig | null>(this.readFromStorage());

  readonly config$ = this.configSubject.asObservable();

  //lấy permission và gán vào localStorage với khóa config
  getAppConfig(): Observable<AppConfig> {
    return this.userService.getProfile().pipe(
      map((profile): AppConfig => ({
        userId: profile.id,
        userName: profile.userName,
        email: profile.email,

        roles: profile.roles.map((role) => role.name),

        permissions: [
          ...new Set(
            profile.roles.flatMap((role) => role.permissions.map((permission) => permission.code)),
          ),
        ],
      })),
      tap((config) => this.set(config)),
    );
  }
  getAll$(): Observable<AppConfig | null> {
    return this.config$;
  }

  getAll(): AppConfig | null {
    return this.configSubject.value;
  }

  set(config: AppConfig): void {
    this.storage.setItem(ApplicationConfigService.storageKey, JSON.stringify(config));
    this.configSubject.next(config);
  }

  clear(): void {
    this.storage.removeItem(ApplicationConfigService.storageKey);
    this.configSubject.next(null);
  }

  private readFromStorage(): AppConfig | null {
    const storedConfig = this.storage.getItem(ApplicationConfigService.storageKey);

    if (!storedConfig) {
      return null;
    }

    try {
      const config = JSON.parse(storedConfig) as Partial<AppConfig>;

      if (
        typeof config.userId !== 'string' ||
        typeof config.userName !== 'string' ||
        typeof config.email !== 'string' ||
        !Array.isArray(config.roles) ||
        !Array.isArray(config.permissions)
      ) {
        this.storage.removeItem(ApplicationConfigService.storageKey);
        return null;
      }

      return config as AppConfig;
    } catch {
      this.storage.removeItem(ApplicationConfigService.storageKey);
      return null;
    }
  }
}
