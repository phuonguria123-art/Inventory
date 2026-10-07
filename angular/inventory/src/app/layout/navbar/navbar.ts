import { Component, inject } from '@angular/core';
import { AuthService } from '../../core/services/auth.service';

@Component({
  imports: [],
  selector: 'app-navbar',
  styleUrl: './navbar.scss',
  templateUrl: './navbar.html',
})
export class Navbar {

  private readonly authService = inject(AuthService);
  constructor() { }
  logout() {
    this.authService.logout();
  }
}
