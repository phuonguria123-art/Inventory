import { inject, Injectable, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';

@Injectable({ providedIn: 'root' })
export class BrowserStorageService {
  private readonly platformId = inject(PLATFORM_ID);
  private readonly isBrowser = isPlatformBrowser(this.platformId);

  getItem(key: string): string | null {
    if (!this.isBrowser) {
      return null;
    }

    try {
      return window.localStorage.getItem(key);
    } catch {
      return null;
    }
  }

  setItem(key: string, value: string): void {
    if (!this.isBrowser) {
      return;
    }

    try {
      window.localStorage.setItem(key, value);
    } catch {
      // Storage may be unavailable in private mode or restricted browsers.
    }
  }

  removeItem(key: string): void {
    if (!this.isBrowser) {
      return;
    }

    try {
      window.localStorage.removeItem(key);
    } catch {
      // Storage may be unavailable in private mode or restricted browsers.
    }
  }
}
