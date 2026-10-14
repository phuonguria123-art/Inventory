
import { PurchaseOrderStatus } from '../../../enum/purchase-order-status.enum';
import { PurchaseOrderDetailModel } from './purchase-order-detail.model';

export class PurchaseOrderModel {
    id: string = '';
    code: string = '';
    supplierId: string = '';

    // Ngày tạo đơn
    createDate: Date = new Date();

    // Ngày gửi đơn tới nhà cung cấp
    sendDate: Date | null = null;

    expectedReceiveDate: Date = new Date();

    // Ngày nhận thực tế
    receivedDate: Date | null = null;

    // Nhân viên nhận
    receivedTo: string | null = null;

    // Kho nhận hàng
    receivingWarehouseId: string = '';

    freight: number | null = null;
    status: PurchaseOrderStatus = PurchaseOrderStatus.Draft;

    // Tổng tiền sau khi cộng các phí phụ
    totalPrice: number | null = null;

    totalWeight: number | null = null;
    type: string | null = null;

    orderDetailDto: PurchaseOrderDetailModel[] = [];
}

