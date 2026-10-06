import { inject, Injectable } from '@angular/core';
import { ToastrService } from '@openng/ngx-toastr';

@Injectable({ providedIn: 'root' })
export class NotificationService {
  private readonly toastr = inject(ToastrService);

  success(message: string, title = 'Thành công'): void {
    this.toastr.success(message, title);
  }

  error(message: string, title = 'Có lỗi xảy ra'): void {
    this.toastr.error(message, title);
  }

  warning(message: string, title = 'Cảnh báo'): void {
    this.toastr.warning(message, title);
  }

  info(message: string, title = 'Thông báo'): void {
    this.toastr.info(message, title);
  }

  clear(): void {
    this.toastr.clear();
  }
}
