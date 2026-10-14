import { HttpClient, HttpParams } from "@angular/common/http";
import { inject, Injectable } from "@angular/core";
import { Observable } from "rxjs";
import { PagedResult } from "../../../core/models/common/paged-result.model";
import { environment } from "../../../../environments/environment";
import { PurchaseOrderModel } from "../../../core/models/features-models/purchase-order/purchase-order.model";
import { PurchaseOrderStatus } from "../../../core/enum/purchase-order-status.enum";
import { PurchaseOrderCreateModel } from "../../../core/models/features-models/purchase-order/purchase-order-create.model";
import { PurchaseOrderUpdateModel } from "../../../core/models/features-models/purchase-order/purchase-order-update.model";
import { ReceivePurchaseOrderModel } from "../../../core/models/features-models/purchase-order/receive/receive-purchase-order.model";

@Injectable({
    providedIn: "root",
})
export class PurchaseOrderService {
    private readonly http = inject(HttpClient);
    private readonly apiUrl = "/api/PurchaseOrders";

    public getList(
        pageNumber: number,
        pageSize: number,
        status?: PurchaseOrderStatus,
        supplierId?: string,
        warehouseId?: string,
        createDate?: Date
    ): Observable<PagedResult<PurchaseOrderModel>> {
        let params = new HttpParams()
            .set('pageIndex', pageNumber.toString())
            .set('pageSize', pageSize.toString());

        if (status !== undefined && status !== null) {
            params = params.set('status', status.toString());
        }

        if (supplierId) {
            params = params.set('supplierId', supplierId);
        }

        if (warehouseId) {
            params = params.set('warehouseId', warehouseId);
        }

        if (createDate) {
            params = params.set('createDate', createDate.toISOString());
        }

        return this.http.get<PagedResult<PurchaseOrderModel>>(
            this.url(this.apiUrl),
            { params }
        );
    }
    public create(createModel: PurchaseOrderCreateModel): Observable<void> {
        return this.http.post<void>(this.url(this.apiUrl), createModel);
    }

    public update(id: string, updateModel: PurchaseOrderUpdateModel): Observable<void> {
        return this.http.put<void>(this.url(`${this.apiUrl}/${id}`), updateModel);
    }
    public updateStatus(
        id: string,
        status: PurchaseOrderStatus
    ): Observable<void> {
        const params = new HttpParams()
            .set('status', status.toString());
        return this.http.put<void>(
            this.url(`${this.apiUrl}/${id}/status`), null,
            { params })
    }
    public getById(purchaseOrderId: string): Observable<PurchaseOrderModel> {
        return this.http.get<PurchaseOrderModel>(this.url(`${this.apiUrl}/${purchaseOrderId}`))
    }
    public received(receiveModel: ReceivePurchaseOrderModel): Observable<void> {
        return this.http.put<void>(this.url(`${this.apiUrl}/receive`), receiveModel)
    }
    public cancel(id: string): Observable<void> {
        const params = new HttpParams().set('id', id);
        return this.http.put<void>(this.url(`${this.apiUrl}/cancel`), null, { params });
    }

    private url(s: string): string {
        return environment.apis.default.url + s;
    }
}
