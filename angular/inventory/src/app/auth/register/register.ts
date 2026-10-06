import { Component, inject } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { RegisterModel } from '../../core/models/account/register.model';
import { NotificationService } from '../../core/services/notification.service';
import { UserService } from '../../core/services/user.service';


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
  private registerForm = new FormGroup({
    email: new FormControl<string>('', { nonNullable: true }),
    userName: new FormControl<string>('', { nonNullable: true }),
    password: new FormControl<string>('', { nonNullable: true }),
    confirmPassword: new FormControl<string>('', { nonNullable: true }),
  });
  constructor(

  ) { }
  onSubmit() {
    console.log(this.registerForm.value);
    this.parseFormToModel();
    this.userService.register(this.registerModel).subscribe({
      next: (res) => {
        this.notification.success(
          "Thêm mới lịch sử công tác thành công",
          "Thành công",
        );
        console.log(res);
      },
      error: (err) => {
        console.error(err);
      },
    });
  }
  parseFormToModel() {
    this.registerModel.email = this.registerForm.controls.email.value;
    this.registerModel.userName = this.registerForm.controls.userName.value;
    this.registerModel.password = this.registerForm.controls.password.value;
  }
  closeLoginInfo() { }
}
