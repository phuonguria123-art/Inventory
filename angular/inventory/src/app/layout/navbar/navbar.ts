import { Component, computed, inject, signal } from '@angular/core';
import { AuthService } from '../../core/services/auth.service';
import { ApplicationConfigService } from '../../core/services/application-config.service';
import { toSignal } from '@angular/core/rxjs-interop';
@Component({
  imports: [],
  selector: 'app-navbar',
  styleUrl: './navbar.scss',
  templateUrl: './navbar.html',
})
export class Navbar {
  private readonly applicationConfigService =
    inject(ApplicationConfigService);

  private readonly authService = inject(AuthService);
  readonly isProfileMenuOpen = signal(false);
  readonly config = toSignal(
    this.applicationConfigService.config$,
    {
      initialValue: this.applicationConfigService.getAll(),
    }
  );
  constructor() { }

  readonly initials = computed(() => {
    const userName = this.config()?.userName?.trim();

    if (!userName) {
      return 'U';
    }

    return userName
      .split(/\s+/)
      .slice(0, 2)
      .map(part => part.charAt(0).toUpperCase())
      .join('');
  });

  readonly roleNames = computed(() => {
    const roles = this.config()?.roles ?? [];
    return roles.length > 0 ? roles.join(', ') : 'Người dùng';
  });
  toggleProfileMenu(): void {
    this.isProfileMenuOpen.update(value => !value);
  }
  logout() {
    this.authService.logout();
  }
}
