import { Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ProductModel } from '../../core/models/features-models/product/product.model';
import { NotificationService } from '../../core/services/notification.service';
import { ProductService } from './service/product.service';

@Component({
  imports: [RouterLink],
  selector: 'app-product',
  styleUrl: './product.scss',
  templateUrl: './product.html',
})
export class Product {
  private readonly productService = inject(ProductService);
  private readonly notification = inject(NotificationService);

  readonly products = signal<ProductModel[]>([]);
  readonly pageNumber = signal(1);
  readonly pageSize = signal(10);
  readonly totalCount = signal(0);
  readonly totalPages = signal(0);
  readonly isLoading = signal(false);

  ngOnInit(): void {
    this.getProducts();
  }

  getProducts(): void {
    this.isLoading.set(true);
    this.productService.getProducts(this.pageNumber(), this.pageSize()).subscribe({
      next: (result) => {
        this.products.set(result.items);
        this.totalCount.set(result.totalCount);
        this.totalPages.set(result.totalPages);
        this.isLoading.set(false);
      },
      error: () => {
        this.isLoading.set(false);
        this.notification.error('Không thể tải danh sách sản phẩm');
      },
    });
  }

  changePage(page: number): void {
    if (page < 1 || page > this.totalPages() || page === this.pageNumber()) return;
    this.pageNumber.set(page);
    this.getProducts();
  }

  onDelete(product: ProductModel): void {
    if (!window.confirm(`Bạn có chắc muốn xóa sản phẩm "${product.name}"?`)) return;

    this.productService.deleteProduct(product.id).subscribe({
      next: () => {
        this.notification.success('Xóa sản phẩm thành công');
        this.getProducts();
      },
      error: () => this.notification.error('Không thể xóa sản phẩm'),
    });
  }
}
