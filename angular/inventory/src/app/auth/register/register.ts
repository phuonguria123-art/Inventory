import { Component, inject, signal } from '@angular/core';
import { AbstractControl, FormControl, FormGroup, ReactiveFormsModule, ValidationErrors, Validators } from '@angular/forms';
import { RegisterModel } from '../../core/models/account/register.model';
import { NotificationService } from '../../core/services/notification.service';
import { UserService } from '../../core/services/user.service';
import { finalize } from 'rxjs';
import { getApiErrorMessage } from '../../core/helpers/api-error.helper';

function passwordsMatch(control: AbstractControl): ValidationErrors | null {
  return control.get('password')?.value === control.get('confirmPassword')?.value ? null : { passwordsMismatch: true };
}


@Component({
  imports: [
    ReactiveFormsModule,
  ],
  selector: 'app-register',
  styleUrl: './register.scss',
  templateUrl: './register.html',
})
export class Register {
  private readonly notification = inject(NotificationService);
  private readonly userService = inject(UserService);
  registerModel: RegisterModel = new RegisterModel();
  readonly isSubmitting = signal(false);
  readonly registerForm = new FormGroup({
    email: new FormControl<string>('', { nonNullable: true, validators: [Validators.required, Validators.email, Validators.maxLength(254)] }),
    userName: new FormControl<string>('', { nonNullable: true, validators: [Validators.required, Validators.minLength(3), Validators.maxLength(100), Validators.pattern(/.*\S.*/)] }),
    password: new FormControl<string>('', { nonNullable: true, validators: [Validators.required, Validators.minLength(6), Validators.maxLength(100)] }),
    confirmPassword: new FormControl<string>('', { nonNullable: true, validators: Validators.required }),
  }, { validators: passwordsMatch });
  constructor(

  ) { }
  onSubmit() {
    if (this.registerForm.invalid || this.isSubmitting()) { this.registerForm.markAllAsTouched(); return; }
    this.parseFormToModel();
    this.isSubmitting.set(true);
    this.userService.register(this.registerModel).pipe(finalize(() => this.isSubmitting.set(false))).subscribe({
      next: () => this.notification.success('Đăng ký tài khoản thành công'),
      error: err => this.notification.error(getApiErrorMessage(err, 'Không thể đăng ký tài khoản')),
    });
  }
  parseFormToModel() {
    this.registerModel.email = this.registerForm.controls.email.value.trim();
    this.registerModel.userName = this.registerForm.controls.userName.value.trim();
    this.registerModel.password = this.registerForm.controls.password.value;
  }
  closeLoginInfo() { }
}
