import { HttpClient } from "@angular/common/http";
import { inject, Injectable } from "@angular/core";
import { Observable } from "rxjs";
import { environment } from "../../../../environments/environment";
import { SupplierUpdateModel } from "../../../core/models/features-models/supplier/supplier-update.model";
import { SupplierModel } from "../../../core/models/features-models/supplier/supplier.model";
import { SupplierCreateModel } from "../../../core/models/features-models/supplier/supplier-create.model";

@Injectable({
    providedIn: "root",
})
export class SupplierService {
    private readonly http = inject(HttpClient);
    private readonly apiUrl = "/api/suppliers";
    public getList(): Observable<SupplierModel[]> {
        return this.http.get<SupplierModel[]>(this.url(`${this.apiUrl}`));
    }
    public getSupplierById(id: string): Observable<SupplierModel> {
        return this.http.get<SupplierModel>(this.url(`${this.apiUrl}/${id}`));
    }
    public createSupplier(supplier: SupplierCreateModel): Observable<SupplierModel> {
        return this.http.post<SupplierModel>(this.url(this.apiUrl), supplier);
    }
    public updateSupplier(id: string, supplier: SupplierUpdateModel): Observable<void> {
        return this.http.put<void>(this.url(`${this.apiUrl}/${id}`), supplier);
    }
    public deleteSupplier(id: string): Observable<void> {
        return this.http.delete<void>(this.url(`${this.apiUrl}/${id}`));
    }
    private url(s: string): string {
        return environment.apis.default.url + s;
    }
}
