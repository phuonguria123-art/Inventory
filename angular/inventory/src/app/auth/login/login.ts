import { Component, inject } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { NotificationService } from '../../core/services/notification.service';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { LoginModel } from '../../core/models/account/login.model';
import { AuthService } from '../../core/services/auth.service';
import { ApplicationConfigService } from '../../core/services/application-config.service';
import { switchMap } from 'rxjs';

@Component({
  imports: [RouterLink, ReactiveFormsModule],
  selector: 'app-login',
  styleUrl: './login.scss',
  templateUrl: './login.html',
})
export class Login {
  private readonly notification = inject(NotificationService);
  private readonly authService = inject(AuthService);
  private readonly appConfigService = inject(ApplicationConfigService);

  private route = inject(ActivatedRoute);
  loginModel: LoginModel = new LoginModel();
  private returnUrl = '/';
  private loginForm = new FormGroup({
    userName: new FormControl<string>('', { nonNullable: true }),
    password: new FormControl<string>('', { nonNullable: true }),
  });
  constructor(private router: Router) { }
  ngOnInit() {
    this.returnUrl =
      this.route.snapshot.queryParamMap.get('returnUrl') || '/';

    if (this.authService.currentUserValue) {
      this.router.navigateByUrl(this.returnUrl);
    }
  }
  onSubmit() {
    this.parseFormToModel();
    this.authService
      .login(this.loginModel)
      .pipe(switchMap(() => this.appConfigService.getAppConfig()))
      .subscribe({
        next: () => {
          this.notification.success('Đăng nhập thành công', 'Thành công');

          this.router.navigateByUrl(this.returnUrl);
        },
        error: (err) => {
          this.appConfigService.clear();
          this.authService.logout();
          this.notification.error('Đăng nhập thất bại', 'Lỗi');
        },
      });
  }
  parseFormToModel() {
    this.loginModel.userName = this.loginForm.controls.userName.value;
    this.loginModel.password = this.loginForm.controls.password.value;
  }

}
