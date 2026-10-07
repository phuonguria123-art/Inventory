import { HttpClient, HttpParams } from "@angular/common/http";
import { inject, Injectable } from "@angular/core";
import { Observable } from "rxjs";
import { ProductModel } from "../../../core/models/features-models/product/product.model";
import { environment } from "../../../../environments/environment";
import { ProductCreateModel } from "../../../core/models/features-models/product/product-create.model";
import { ProductUpdateModel } from "../../../core/models/features-models/product/product-update.model";
import { PagedResult } from "../../../core/models/common/paged-result.model";

@Injectable({
    providedIn: "root",
})
export class ProductService {
    private readonly http = inject(HttpClient);
    private readonly apiUrl = "/api/products";

    public getProducts(
        pageNumber: number,
        pageSize: number,
    ): Observable<PagedResult<ProductModel>> {
        const params = new HttpParams()
            .set('pageNumber', pageNumber.toString())
            .set('pageSize', pageSize.toString());
        return this.http.get<PagedResult<ProductModel>>(this.url(this.apiUrl), { params });
    }
    public getProductById(id: string): Observable<ProductModel> {
        return this.http.get<ProductModel>(this.url(`${this.apiUrl}/${id}`));
    }
    public createProduct(product: ProductCreateModel): Observable<ProductModel> {
        return this.http.post<ProductModel>(this.url(this.apiUrl), product);
    }
    public updateProduct(id: string, product: ProductUpdateModel): Observable<void> {
        return this.http.put<void>(this.url(`${this.apiUrl}/${id}`), product);
    }
    public deleteProduct(id: string): Observable<void> {
        return this.http.delete<void>(this.url(`${this.apiUrl}/${id}`));
    }
    private url(s: string): string {
        return environment.apis.default.url + s;
    }
}
