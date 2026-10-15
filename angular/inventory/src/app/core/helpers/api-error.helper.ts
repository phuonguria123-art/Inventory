
import { HttpErrorResponse } from '@angular/common/http';

interface ApiErrorBody {
    message?: string;
    errors?: string[];
}

export function getApiErrorMessage(
    error: unknown,
    fallback: string
): string {
    if (!(error instanceof HttpErrorResponse)) {
        return fallback;
    }

    const body: unknown = error.error;

    if (typeof body === 'string' && body.trim()) {
        return body;
    }

    if (typeof body === 'object' && body !== null) {
        const apiError = body as ApiErrorBody;

        if (apiError.message?.trim()) {
            return apiError.message;
        }

        if (apiError.errors?.length) {
            return apiError.errors.join(', ');
        }

        // Hỗ trợ lỗi theo cấu trúc ProblemDetails của ASP.NET Core
        if ('detail' in body && typeof body.detail === 'string' && body.detail.trim()) {
            return body.detail;
        }

        if ('title' in body && typeof body.title === 'string' && body.title.trim()) {
            return body.title;
        }
    }

    if (error.status === 0) {
        return 'Không thể kết nối tới máy chủ.';
    }

    if (error.status === 401) {
        return 'Phiên đăng nhập không hợp lệ.';
    }

    if (error.status === 403) {
        return 'Bạn không có quyền thực hiện thao tác này.';
    }

    return fallback;
}