import { HttpClient } from "@angular/common/http";
import { inject, Injectable } from "@angular/core";
import { Observable } from "rxjs";
import { environment } from "../../../../environments/environment";
import { WarehouseModel } from "../../../core/models/features-models/warehouse/warehouse.model";
import { WarehouseCreateModel } from "../../../core/models/features-models/warehouse/warehouse-create.model";
import { WarehouseUpdateModel } from "../../../core/models/features-models/warehouse/warehouse-update.model";

@Injectable({
    providedIn: "root",
})
export class WarehouseService {
    private readonly http = inject(HttpClient);
    private readonly apiUrl = "/api/warehouses";

    public getList(): Observable<WarehouseModel[]> {
        return this.http.get<WarehouseModel[]>(this.url(this.apiUrl));
    }

    public getWarehouseById(id: string): Observable<WarehouseModel> {
        return this.http.get<WarehouseModel>(this.url(`${this.apiUrl}/${id}`));
    }

    public createWarehouse(warehouse: WarehouseCreateModel): Observable<WarehouseModel> {
        return this.http.post<WarehouseModel>(this.url(this.apiUrl), warehouse);
    }

    public updateWarehouse(id: string, warehouse: WarehouseUpdateModel): Observable<void> {
        return this.http.put<void>(this.url(`${this.apiUrl}/${id}`), warehouse);
    }

    public deleteWarehouse(id: string): Observable<void> {
        return this.http.delete<void>(this.url(`${this.apiUrl}/${id}`));
    }

    private url(s: string): string {
        return environment.apis.default.url + s;
    }
}
