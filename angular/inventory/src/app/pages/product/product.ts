import { Component, inject } from '@angular/core';
import { AuthService } from '../../core/services/auth.service';

@Component({
  imports: [],
  selector: 'app-product',
  styleUrl: './product.scss',
  templateUrl: './product.html',
})
export class Product {
  private readonly authService = inject(AuthService);
  constructor() { }
  logout() {
    this.authService.logout();
  }
}
