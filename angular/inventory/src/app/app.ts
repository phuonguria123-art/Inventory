import { Component, signal } from '@angular/core';
import { Router, RouterOutlet } from '@angular/router';


@Component({
  selector: 'app-root',
  imports: [RouterOutlet],
  templateUrl: './app.html',
  styleUrl: './app.scss'
})
export class App {
  constructor(private router: Router) { }
  protected readonly title = signal('inventory');
  openRegister() {
    this.router.navigateByUrl("register");

  }
}
