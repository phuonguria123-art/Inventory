# Các loại chứng từ và luồng nghiệp vụ kho

## 1. Purchase Order

`PurchaseOrder` chỉ đại diện cho đơn mua hàng doanh nghiệp gửi tới nhà cung cấp, thường viết tắt là PO.

Luồng cơ bản:

```text
Doanh nghiệp tạo Purchase Order
→ Gửi cho nhà cung cấp
→ Nhà cung cấp xác nhận và giao hàng
→ Kho nhận hàng
→ QuantityOnHand tăng
→ Tạo InventoryTransaction loại Receipt
→ Purchase Order chuyển sang Received
```

Ví dụ:

```text
PurchaseOrder.Code = PO-2026-001
OrderedQuantity = 100
ActualReceivedQuantity = 90
```

Khi nhận hàng, các `InventoryTransaction` được tạo nên dùng:

```text
TransactionType = Receipt
Reference = PO-2026-001
```

Nhờ đó có thể tra cứu toàn bộ biến động kho phát sinh từ một Purchase Order.

## 2. Purchase Order không đại diện cho các chứng từ khác

| Nghiệp vụ | Entity phù hợp | Mục đích |
|---|---|---|
| Đơn mua hàng | `PurchaseOrder` | Đặt hàng từ nhà cung cấp |
| Phiếu nhận hàng | `GoodsReceipt` | Xác nhận hàng thực tế đã được nhận |
| Đơn bán hàng | `SalesOrder` | Ghi nhận yêu cầu mua hàng từ khách |
| Phiếu chuyển kho | `StockTransfer` hoặc `TransferOrder` | Chuyển hàng giữa hai kho |
| Phiếu xuất kho | `GoodsIssue` | Xác nhận hàng đã rời kho |
| Phiếu kiểm kê | `InventoryCount` | So sánh tồn hệ thống với tồn thực tế |
| Phiếu điều chỉnh | `InventoryAdjustment` | Ghi nhận nguyên nhân tăng hoặc giảm tồn |
| Phiếu trả nhà cung cấp | `PurchaseReturn` | Trả hàng đã mua cho nhà cung cấp |

Các entity trên là những chứng từ độc lập. Không nên thêm trường loại chứng từ vào `PurchaseOrder` để sử dụng nó cho mọi nghiệp vụ.

## 3. Purchase Order và Goods Receipt

Purchase Order thể hiện số lượng doanh nghiệp muốn mua:

```text
PO-2026-001 đặt 100 sản phẩm
```

Goods Receipt thể hiện số lượng kho thực tế nhận được:

```text
Lần nhận 1: 40
Lần nhận 2: 30
Lần nhận 3: 20
Tổng thực nhận: 90
```

Trong phiên bản đơn giản của dự án, chưa cần entity `GoodsReceipt`. Endpoint nhận PO có thể trực tiếp:

1. Kiểm tra PO đang ở trạng thái cho phép nhận.
2. Cập nhật số lượng thực nhận trên từng dòng PO.
3. Gọi nghiệp vụ nhập kho cho từng sản phẩm.
4. Tạo `InventoryTransaction` loại `Receipt` với `Reference = PurchaseOrder.Code`.
5. Chuyển PO sang `Received`.
6. Lưu toàn bộ trong cùng database transaction.

Chỉ nên tạo `GoodsReceipt` và `GoodsReceiptDetail` khi cần nhận một PO qua nhiều đợt hoặc cần lưu riêng từng lần giao hàng.

## 4. Chuyển kho

Chuyển kho không thuộc Purchase Order vì không liên quan đến nhà cung cấp:

```text
Kho A → Kho B
```

Luồng hiện tại có thể thực hiện trực tiếp bằng `TransferAsync`:

```text
Kho A: tạo TransferOut
Kho B: tạo TransferIn
Hai giao dịch dùng chung Reference
```

Ví dụ:

```text
Reference = TRF-2026-001
```

Chỉ cần entity `StockTransfer` khi muốn quản lý phiếu chuyển kho qua nhiều trạng thái như `Draft`, `InTransit`, `Received` hoặc `Cancelled`.

## 5. Yêu cầu mua hàng từ khách

Yêu cầu mua hàng từ khách không phải Purchase Order. Entity phù hợp là `SalesOrder`:

```text
Khách tạo Sales Order
→ Hệ thống giữ hàng
→ Xác nhận đơn
→ Xuất hàng
→ InventoryReservation chuyển sang Fulfilled
```

Dự án hiện chưa có `SalesOrder`, vì vậy `InventoryReservation` tạm liên kết với yêu cầu nguồn bằng:

```text
Reference = mã đơn hoặc mã yêu cầu bên ngoài
```

Khi có entity `SalesOrder`, có thể bổ sung `SalesOrderId` làm foreign key. `Reference` vẫn có thể được dùng làm mã dễ đọc trong lịch sử giao dịch.

## 6. Phiếu kiểm kê và điều chỉnh tồn

Kiểm kê so sánh số lượng hệ thống với số lượng thực tế:

```text
SystemQuantity = 100
ActualQuantity = 97
Difference = -3
```

Giao dịch phát sinh:

```text
TransactionType = AdjustmentDecrease
Quantity = 3
Reference = COUNT-2026-001
```

Hàm điều chỉnh hiện tại có thể dùng `Reference` làm mã phiếu kiểm kê mà chưa cần bảng riêng.

Khi cần kiểm kê nhiều sản phẩm, trạng thái phiếu hoặc phê duyệt, nên tạo:

```text
InventoryCount
└── InventoryCountDetail
```

Tất cả giao dịch điều chỉnh của cùng một phiếu sử dụng chung `InventoryCount.Code` làm `Reference`.

## 7. Ý nghĩa của Reference

`Reference` là mã chứng từ nghiệp vụ dễ đọc và dùng để nhóm các giao dịch liên quan.

Ví dụ:

| TransactionType | Reference | Nguồn nghiệp vụ |
|---|---|---|
| `Receipt` | `PO-2026-001` | Nhận đơn mua hàng |
| `Issue` | `SO-2026-015` | Xuất hàng cho khách |
| `TransferOut` | `TRF-2026-004` | Chuyển khỏi kho nguồn |
| `TransferIn` | `TRF-2026-004` | Nhận tại kho đích |
| `AdjustmentDecrease` | `COUNT-2026-003` | Chênh lệch kiểm kê |

`Reference` không phải foreign key và không tự bảo đảm chứng từ nguồn tồn tại. Khi entity nguồn đã có trong hệ thống, nên dùng foreign key để liên kết chính xác; `Reference` tiếp tục phục vụ hiển thị, tìm kiếm và đối soát.

## 8. Phạm vi của phiên bản CV

Luồng cần hoàn thiện:

```text
Supplier
→ PurchaseOrder
→ Submitted
→ Received
→ Inventory tăng
→ InventoryTransaction Receipt
```

Các chứng từ chưa cần tạo entity riêng trong phiên bản CV:

- `GoodsReceipt`: dùng trực tiếp thao tác nhận PO trước.
- `SalesOrder`: reservation tạm dùng `Reference`.
- `StockTransfer`: chuyển kho trực tiếp bằng cặp transaction.
- `InventoryCount`: điều chỉnh tồn trực tiếp bằng `Reference`.
- `PurchaseReturn`: giữ trong roadmap.

Những entity này chỉ được bổ sung khi nghiệp vụ cần vòng đời, nhiều dòng, trạng thái hoặc khả năng truy vết riêng mà một chuỗi `Reference` không còn đáp ứng được.
