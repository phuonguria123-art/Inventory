import { HttpClient, HttpParams } from "@angular/common/http";
import { inject, Injectable } from "@angular/core";
import { Observable } from "rxjs";
import { PagedResult } from "../../../core/models/common/paged-result.model";
import { InventoryModel } from "../../../core/models/features-models/inventory/inventory.model";
import { environment } from "../../../../environments/environment";
import { InventoryTransactionType } from "../../../core/enum/inventory-transaction-type.enum";
import { InventoryTransactionModel } from "../../../core/models/features-models/inventory-transaction/inventory-transaction.model";
import { ReceiveInventoryModel } from "../../../core/models/features-models/inventory/receive/receive-inventoty.model";
import { IssueInventoryModel } from "../../../core/models/features-models/inventory/issue/issue-inventory.model";
import { TransferInventoryModel } from "../../../core/models/features-models/inventory/transfer/transfer-inventory.model";
import { InventoryItemModel } from "../../../core/models/features-models/inventory-item/inventory-item.model";
import { AdjustInventoryModel } from "../../../core/models/features-models/inventory/adjust/adjust-inventory.model";
import { ReservationInventoryModel } from "../../../core/models/features-models/inventory/reservation/reservation.model";
import { InventoryMovementSummaryModel } from "../../../core/models/features-models/inventory/inventory-movement-summary.model";

@Injectable({
    providedIn: "root",
})
export class InventoryService {
    private readonly http = inject(HttpClient);
    private readonly apiUrl = "/api/inventory";

    public getList(
        pageNumber: number,
        pageSize: number,
        warehouseId?: string,
        productId?: string,
        warehouseSearch?: string,
        productSearch?: string,
        sortBy?: string,
        sortDescending: boolean = true
    ): Observable<PagedResult<InventoryModel>> {

        let params = new HttpParams()
            .set('pageNumber', pageNumber.toString())
            .set('pageSize', pageSize.toString());

        if (warehouseId != null) {
            params = params.set('warehouseId', warehouseId);
        }

        if (productId != null) {
            params = params.set('productId', productId);
        }

        if (warehouseSearch != null && warehouseSearch.trim() !== '') {
            params = params.set('warehouseSearch', warehouseSearch);
        }

        if (productSearch != null && productSearch.trim() !== '') {
            params = params.set('productSearch', productSearch);
        }

        if (sortBy != null && sortBy.trim() !== '') {
            params = params.set('sortBy', sortBy);
        }

        params = params.set(
            'sortDescending',
            sortDescending.toString()
        );

        return this.http.get<PagedResult<InventoryModel>>(
            this.url(this.apiUrl),
            { params }
        );
    }
    //lịch sử thay đổi tồn kho
    public getTransactions(
        pageNumber: number,
        pageSize: number,
        warehouseId?: string,
        productId?: string,
        transactionType?: InventoryTransactionType,
        reference?: string
    ) {
        let params = new HttpParams()
            .set('pageNumber', pageNumber)
            .set('pageSize', pageSize);

        if (warehouseId) {
            params = params.set('warehouseId', warehouseId);
        }

        if (productId) {
            params = params.set('productId', productId);
        }

        if (transactionType !== undefined && transactionType !== null) {
            params = params.set('transactionType', transactionType);
        }

        if (reference?.trim()) {
            params = params.set('reference', reference.trim());
        }

        return this.http.get<PagedResult<InventoryTransactionModel>>(
            this.url(`${this.apiUrl}/transactions`),
            { params }
        );
    }

    public getListLots(
        warehouseId: string,
        productId: string
    ): Observable<InventoryItemModel[]> {
        return this.http.get<InventoryItemModel[]>(
            this.url(`${this.apiUrl}/warehouses/${warehouseId}/products/${productId}/lots`)
        );
    }

    //nhập hàng
    public receive(receiveModel: ReceiveInventoryModel): Observable<InventoryModel[]> {
        return this.http.post<InventoryModel[]>(this.url(`${this.apiUrl}/receive`), receiveModel);
    }
    //xuất kho 
    public issue(issueModel: IssueInventoryModel): Observable<InventoryModel[]> {
        return this.http.post<InventoryModel[]>(this.url(`${this.apiUrl}/issue`), issueModel)
    }
    //chuyển kho
    public transfer(transferModel: TransferInventoryModel): Observable<void> {
        return this.http.post<void>(this.url(`${this.apiUrl}/transfer`), transferModel);
    }
    //kiểm kê
    public adjust(adjustModel: AdjustInventoryModel): Observable<InventoryModel[]> {
        return this.http.post<InventoryModel[]>(this.url(`${this.apiUrl}/adjust`), adjustModel);

    }
    //giữ hàng
    public reservations(reservationModel: ReservationInventoryModel): Observable<InventoryModel[]> {
        return this.http.post<InventoryModel[]>(this.url(`${this.apiUrl}/reservations`), reservationModel)
    }

    //cảnh báo sp sắp hết
    public getLowStock(): Observable<InventoryModel[]> {
        return this.http.get<InventoryModel[]>(this.url(`${this.apiUrl}/low-stock`))
    }
    //cảnh báo vượt tồn
    getExcessStock(): Observable<InventoryModel[]> {
        return this.http.get<InventoryModel[]>(
            this.url(`${this.apiUrl}/excess-stock`)
        );
    }
    getMovementByMonthReport(
        productId: string,
        startDate: string,
        endDate: string
    ): Observable<InventoryMovementSummaryModel> {
        const params = new HttpParams()
            .set('productId', productId)
            .set('startDate', startDate)
            .set('endDate', endDate);

        return this.http.get<InventoryMovementSummaryModel>(
            this.url(`${this.apiUrl}/movement-by-month-report`),
            { params }
        );
    }
    private url(s: string): string {
        return environment.apis.default.url + s;
    }
}
