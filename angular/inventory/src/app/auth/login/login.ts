import { Component, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { NotificationService } from '../../core/services/notification.service';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { LoginModel } from '../../core/models/account/login.model';
import { AuthService } from '../../core/services/auth.service';
import { ApplicationConfigService } from '../../core/services/application-config.service';
import { finalize, switchMap } from 'rxjs';
import { getApiErrorMessage } from '../../core/helpers/api-error.helper';

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
  readonly isSubmitting = signal(false);
  private returnUrl = '/';
  readonly loginForm = new FormGroup({
    userName: new FormControl<string>('', { nonNullable: true, validators: [Validators.required, Validators.maxLength(100), Validators.pattern(/.*\S.*/)] }),
    password: new FormControl<string>('', { nonNullable: true, validators: [Validators.required, Validators.minLength(6), Validators.maxLength(100)] }),
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
    if (this.loginForm.invalid || this.isSubmitting()) { this.loginForm.markAllAsTouched(); return; }
    this.parseFormToModel();
    this.isSubmitting.set(true);
    this.authService
      .login(this.loginModel)
      .pipe(switchMap(() => this.appConfigService.getAppConfig()), finalize(() => this.isSubmitting.set(false)))
      .subscribe({
        next: () => {
          this.notification.success('Đăng nhập thành công', 'Thành công');

          this.router.navigateByUrl(this.returnUrl);
        },
        error: (err) => {
          this.appConfigService.clear();
          this.authService.logout();
          this.notification.error(getApiErrorMessage(err, 'Đăng nhập thất bại'));
        },
      });
  }
  parseFormToModel() {
    this.loginModel.userName = this.loginForm.controls.userName.value.trim();
    this.loginModel.password = this.loginForm.controls.password.value;
  }

}
