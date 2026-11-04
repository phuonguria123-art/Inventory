# Inventory Management System

Inventory Management System là ứng dụng quản lý kho được xây dựng bằng ASP.NET Core và Angular. Hệ thống hỗ trợ quản lý sản phẩm, nhà cung cấp, kho hàng, tồn kho theo lô và vòng đời đơn mua hàng.

Mục tiêu của dự án là mô phỏng các nghiệp vụ backend thường gặp trong hệ thống kho: nhập, xuất, chuyển kho, kiểm kê, giữ hàng, theo dõi lịch sử giao dịch và nhận hàng từ Purchase Order. Dự án tập trung nhiều hơn vào nghiệp vụ backend, tính nhất quán dữ liệu và phân quyền; Angular được sử dụng để hoàn thiện luồng sử dụng từ đầu đến cuối.

## Tính năng chính

### Xác thực và phân quyền

- Đăng ký, đăng nhập bằng JWT access token và refresh token.
- Mật khẩu được băm bằng BCrypt.
- Phân quyền theo role và permission bằng policy-based authorization.
- Seed các role `Admin`, `Manager`, `Staff` cùng tập permission tương ứng.
- Angular HTTP interceptor tự gắn access token và xử lý refresh token.
- Dùng chung một refresh request khi nhiều API đồng thời trả về `401`.
- Auth guard và permission guard bảo vệ route phía Angular.

### Quản lý danh mục

- CRUD sản phẩm.
- CRUD kho hàng.
- CRUD nhà cung cấp.
- Quản lý role của người dùng.

### Hoạt động kho

- Theo dõi tồn theo kho và sản phẩm.
- Theo dõi chi tiết tồn theo từng lô hàng.
- Nhập kho theo một hoặc nhiều sản phẩm, mỗi sản phẩm có nhiều lô.
- Xuất kho theo đúng lô và số lượng khả dụng.
- Chuyển hàng theo lô giữa hai kho.
- Kiểm kê và điều chỉnh tồn theo số lượng thực tế.
- Giữ và hủy giữ hàng theo mã tham chiếu.
- Theo dõi lịch sử giao dịch nhập, xuất, chuyển và điều chỉnh.
- Cảnh báo sản phẩm dưới mức tồn tối thiểu hoặc vượt mức tồn tối đa.
- Cấu hình mức tồn tối thiểu/tối đa riêng cho từng sản phẩm tại từng kho.
- Báo cáo tổng nhập/xuất theo sản phẩm và khoảng thời gian, tổng hợp theo tháng.
- Tự động chuyển reservation hết hạn sang `Expired` và giải phóng lượng đang giữ bằng background worker.

### Purchase Order

- Tạo, xem và cập nhật Purchase Order ở trạng thái nháp.
- Lọc danh sách theo trạng thái, nhà cung cấp, kho nhận và ngày tạo.
- Chuyển trạng thái theo vòng đời đơn mua hàng.
- Hủy đơn chưa hoàn tất.
- Nhận toàn bộ đơn theo từng sản phẩm và nhiều lô hàng.
- Tự động nhập kho và ghi lịch sử giao dịch khi nhận hàng thành công.
- Chống nhận lặp bằng `Idempotency-Key`; retry cùng thao tác không làm tăng tồn lần thứ hai.

## Công nghệ sử dụng

### Backend

- .NET 8 và ASP.NET Core Web API
- Entity Framework Core 8
- SQL Server
- Redis (`IDistributedCache`/StackExchange.Redis)
- JWT Bearer Authentication
- BCrypt
- AutoMapper
- Swagger/OpenAPI
- xUnit, ASP.NET Core `WebApplicationFactory` và EF Core InMemory

### Frontend

- Angular 22
- TypeScript và SCSS
- Angular Reactive Forms
- RxJS
- Angular HTTP Interceptor
- Route Guard và Permission Guard
- `@openng/ngx-toastr`

## Kiến trúc dự án

Backend sử dụng kiến trúc phân tầng. Mỗi project có trách nhiệm riêng nhằm hạn chế phụ thuộc giữa nghiệp vụ và hạ tầng:

```text
aspnet-core/
├── Inventory.Domain          # Entity, enum, domain exception và abstraction cốt lõi
├── Inventory.Application     # DTO, service nghiệp vụ, interface và mapping
├── Inventory.Infrastructure  # EF Core DbContext, migration và repository
├── Inventory.Api             # Controller, authentication, authorization và middleware
└── Inventory.Api.Tests       # Integration test cho các luồng chính
```

Luồng phụ thuộc tổng quát:

```text
HTTP Request
    ↓
Inventory.Api
    ↓
Inventory.Application
    ↓
Domain abstractions
    ↑
Inventory.Infrastructure ─┬→ SQL Server (nguồn dữ liệu chính)
                          └→ Redis (cache dữ liệu đọc)
```

Frontend được đặt trong `angular/inventory`:

```text
angular/inventory/src/app/
├── auth       # Đăng nhập và đăng ký
├── core       # Model, service, interceptor, guard và helper dùng chung
├── layout     # Navbar, sidebar và vùng hiển thị route
└── pages      # Các màn hình danh mục và nghiệp vụ
```

Đây là kiến trúc phân tầng trong một modular monolith; dự án hiện không tách thành microservices.

## Mô hình tồn kho

Bốn thành phần chính của nghiệp vụ kho:

- `InventoryBalance`: tổng tồn của một sản phẩm tại một kho.
- `InventoryItem`: số lượng chi tiết của từng lô hàng thuộc một tồn kho.
- `InventoryTransaction`: lịch sử biến động số lượng của sản phẩm tại từng kho; mỗi giao dịch có thể tham chiếu tới lô cụ thể và mã chứng từ phát sinh.
- `InventoryReservation`: số lượng đang được giữ cho một yêu cầu hoặc mã tham chiếu.

```text
Warehouse + Product
        │
        ▼
InventoryBalance
        ├── InventoryItem        (tồn theo lô)
        ├── InventoryTransaction (lịch sử biến động)
        └── InventoryReservation (lượng đang giữ)
```

Số lượng khả dụng được xác định theo công thức:

```text
AvailableQuantity = QuantityOnHand - ReservedQuantity
```

SQL Server là nguồn dữ liệu chính khi quyết định có thể xuất hoặc giữ hàng hay không. Các thao tác làm thay đổi tồn được xử lý trong transaction để giữ dữ liệu tổng, dữ liệu lô và lịch sử giao dịch nhất quán.

Mức tồn tối thiểu và tối đa được cấu hình trên `InventoryBalance`, vì cùng một sản phẩm có thể cần ngưỡng cảnh báo khác nhau tại từng kho. Bản ghi chưa cấu hình dùng giá trị `null` và không xuất hiện trong danh sách cảnh báo tương ứng.

## Đảm bảo tính nhất quán và độ tin cậy

Các quyết định backend chính của dự án:

- **Transaction:** nhập, xuất, chuyển, kiểm kê, reservation và nhận Purchase Order cập nhật tồn tổng, tồn theo lô cùng lịch sử giao dịch trong một transaction.
- **Invariant tồn kho:** `InventoryBalance.QuantityOnHand` phải bằng tổng `InventoryItem.Quantity` của các lô thuộc bản ghi tồn đó.
- **Optimistic concurrency:** `InventoryBalance.RowVersion` phát hiện lost update; xung đột đồng thời được ánh xạ thành HTTP `409 Conflict` thay vì ghi đè âm thầm.
- **Idempotency:** thao tác nhận Purchase Order lưu operation record trong cùng transaction; unique constraint trên `(Key, Operation)` là lớp bảo vệ cuối cùng khi client gửi lại request.
- **Global exception handling:** `IExceptionHandler` ánh xạ lỗi validation/not found/conflict/concurrency thành HTTP `400/404/409` và không trả chi tiết exception nội bộ cho lỗi `500`.
- **Background processing:** hosted service xử lý reservation `Active` đã hết hạn theo từng batch, tôn trọng `CancellationToken` và không giải phóng cùng reservation hai lần.

## Luồng nghiệp vụ chính

### Nhập kho

1. Người dùng chọn kho nhận và danh sách sản phẩm.
2. Mỗi sản phẩm phải có ít nhất một lô với mã lô, số lượng và giá vốn.
3. Backend kiểm tra dữ liệu trùng, số lượng và thông tin ngày của lô.
4. Hệ thống tạo hoặc cập nhật `InventoryBalance` và `InventoryItem`.
5. Hệ thống ghi `InventoryTransaction` cho nghiệp vụ nhập.
6. Toàn bộ thay đổi được thực hiện trong cùng một transaction.

### Xuất kho

1. Người dùng chọn kho, sản phẩm, lô và số lượng cần xuất.
2. Backend kiểm tra tồn khả dụng hoặc reservation tương ứng.
3. Hệ thống trừ tồn của lô và cập nhật tồn tổng.
4. Reservation được giải phóng khi xuất theo lượt giữ hợp lệ.
5. Hệ thống ghi lịch sử xuất kho.

### Chuyển kho

1. Kiểm tra kho nguồn và kho đích khác nhau.
2. Trừ tồn theo lô tại kho nguồn.
3. Tạo mới hoặc cộng vào lô tương ứng tại kho đích.
4. Cập nhật tồn tổng của cả hai kho.
5. Ghi giao dịch chuyển kho trong cùng một transaction.

### Kiểm kê

1. Người dùng chọn các lô đang tồn tại và nhập số lượng đếm thực tế.
2. Hệ thống tính chênh lệch giữa số lượng hệ thống và số lượng thực tế.
3. Tồn theo lô và tồn tổng được cập nhật theo chênh lệch.
4. Mỗi thay đổi được ghi lại bằng giao dịch điều chỉnh có mã phiếu kiểm kê.

### Giữ hàng

1. Người dùng giữ số lượng sản phẩm theo kho và mã tham chiếu.
2. Hệ thống chỉ cho giữ khi số lượng khả dụng còn đủ.
3. `ReservedQuantity` tăng nhưng `QuantityOnHand` không thay đổi.
4. Khi hủy giữ hoặc xuất hàng theo lượt giữ, số lượng giữ được giải phóng.
5. Background worker định kỳ tìm reservation `Active` quá `ExpiresAt`, chuyển sang `Expired` và giảm `ReservedQuantity` trong transaction.

### Nhận Purchase Order

Vòng đời chính của đơn:

```text
Draft → Sent → Confirmed → Shipping → Received
Draft / Sent / Confirmed / Shipping → Cancelled
```

Khi nhận hàng:

1. Người dùng khai báo một hoặc nhiều lô cho từng dòng sản phẩm.
2. Tổng số lượng các lô phải bằng số lượng đã đặt.
3. Backend thực hiện nghiệp vụ nhập kho tại kho nhận của Purchase Order.
4. Tồn tổng, tồn theo lô và lịch sử giao dịch được cập nhật.
5. Mã Purchase Order được dùng làm tham chiếu cho giao dịch nhập.
6. Đơn chuyển sang trạng thái `Received` sau khi transaction hoàn tất.

Frontend gửi một `Idempotency-Key` cho thao tác nhận hàng. Nếu request bị retry với cùng key và cùng nội dung, backend trả kết quả đã xử lý mà không nhập kho lần thứ hai; nếu cùng key được dùng cho nội dung khác, API trả lỗi xung đột.

Phiên bản hiện tại nhận toàn bộ đơn trong một lần; nhận hàng nhiều đợt được để trong roadmap.

## Cache Redis

Dự án áp dụng Cache-Aside Pattern có phạm vi cho hai truy vấn cảnh báo `low-stock` và `excess-stock`:

1. API đọc cache bằng cache key đã quy ước.
2. Khi cache miss, dữ liệu được đọc từ SQL Server và ghi lại vào Redis với TTL 5 phút.
3. Các nghiệp vụ thay đổi tồn hoặc ngưỡng cảnh báo sẽ invalidate hai cache liên quan.
4. Nếu Redis tạm thời không khả dụng, lỗi cache được ghi log và API fallback về SQL Server.

Redis không được dùng để quyết định đủ hàng để xuất hoặc giữ; các quyết định ghi dữ liệu luôn dựa trên SQL Server.

## Báo cáo

Màn hình báo cáo biến động tồn cho phép chọn sản phẩm và khoảng thời gian. Backend lọc các giao dịch `Receipt`/`Issue`, nhóm theo năm–tháng và trả projection gồm tổng nhập, tổng xuất cùng chênh lệch. Chuyển kho và điều chỉnh tồn không được tính vào báo cáo nhập/xuất này.

## Xác thực và phân quyền

Backend dùng policy-based authorization với các permission code, ví dụ:

```text
product.read
product.create
inventory.read
inventory.receive
inventory.issue
inventory.transfer
purchase-order.read
purchase-order.receive
```

Sau khi đăng nhập, Angular tải profile chứa role và permission trước khi điều hướng vào ứng dụng. Permission phía frontend chỉ dùng để điều khiển route/menu; API vẫn là nơi quyết định quyền truy cập cuối cùng.

## Yêu cầu môi trường

- .NET SDK 8
- SQL Server hoặc SQL Server Express
- Redis tương thích Redis protocol (khuyến nghị để kiểm tra cache; API vẫn fallback về SQL Server khi Redis không chạy)
- Node.js tương thích với Angular 22
- npm
- EF Core CLI (`dotnet-ef`) nếu chạy migration bằng command line

## Cài đặt và chạy dự án

### 1. Clone repository

```cmd
git clone <repository-url>
cd InventoryManagement
```

### 2. Cấu hình backend

Dự án yêu cầu connection string và JWT signing key. Không nên commit secret thật vào `appsettings.json`; với môi trường development có thể dùng .NET User Secrets:

```cmd
dotnet user-secrets set "ConnectionStrings:DBConnection" "Server=YOUR_SERVER;Database=Inventory;Trusted_Connection=True;TrustServerCertificate=True" --project aspnet-core\Inventory.Api
dotnet user-secrets set "AppSettings:Token" "YOUR_DEVELOPMENT_SIGNING_KEY_AT_LEAST_32_CHARACTERS" --project aspnet-core\Inventory.Api
```

Cấu hình mặc định đang sử dụng:

```text
Issuer:  MyAwesomeApp
Audience: MyawesomeAudience
Frontend origin: http://localhost:4200
Backend HTTPS: https://localhost:7081
Redis: 127.0.0.1:6379
Reservation expiration interval: 5 phút
```

Có thể thay đổi Redis và chu kỳ xử lý reservation bằng configuration:

```json
{
  "Redis": {
    "Configuration": "127.0.0.1:6379",
    "InstanceName": "InventoryManagement:"
  },
  "ReservationExpiration": {
    "CheckIntervalMinutes": 5,
    "BatchSize": 100
  }
}
```

Nếu chưa cài EF Core CLI:

```cmd
dotnet tool install --global dotnet-ef
```

Áp dụng migration:

```cmd
dotnet ef database update --project aspnet-core\Inventory.Infrastructure --startup-project aspnet-core\Inventory.Api
```

Chạy API:

```cmd
dotnet run --project aspnet-core\Inventory.Api --launch-profile https
```

Swagger có tại:

```text
https://localhost:7081/swagger
```

### 3. Chạy Angular

Mở terminal khác:

```cmd
cd angular\inventory
npm install
npm start
```

Truy cập ứng dụng tại:

```text
http://localhost:4200
```

Nếu thay đổi port backend hoặc frontend, cần cập nhật Angular environment và `Cors:AllowedOrigins` tương ứng.

## Dữ liệu khởi tạo

Migration seed sẵn các role và permission:

- `Admin`: toàn bộ permission.
- `Manager`: quản lý danh mục, hoạt động kho, audit và Purchase Order.
- `Staff`: các quyền đọc và nghiệp vụ kho cơ bản.

Dự án không công khai mật khẩu quản trị mặc định trong repository. Có thể đăng ký tài khoản qua giao diện/API, sau đó gán role bằng luồng quản lý người dùng hoặc dữ liệu development phù hợp.

## API

Swagger mô tả chi tiết request và response. Các nhóm controller chính:

- `Auth`
- `Products`
- `Warehouses`
- `Suppliers`
- `Users`
- `Permissions`
- `Inventory`
- `PurchaseOrders`

Đối với API được bảo vệ, đăng nhập để lấy access token rồi sử dụng nút **Authorize** trong Swagger với JWT bearer token.

## Kiểm thử

Backend có integration test cho authentication, nghiệp vụ kho và vòng đời Purchase Order:

```cmd
dotnet test aspnet-core\Inventory.Api.Tests\Inventory.Api.Tests.csproj
```

Hiện có 13 integration test chạy bằng xUnit, `WebApplicationFactory` và EF Core InMemory. Các test này kiểm tra luồng ứng dụng; dự án không dùng InMemory để khẳng định hành vi transaction, foreign key, check/unique constraint hoặc cách LINQ được dịch trên SQL Server thật.

Kiểm tra build Angular:

```cmd
cd angular\inventory
npm run build
```

Frontend unit test đang được hoàn thiện và chưa được coi là tiêu chí chặn phiên bản portfolio hiện tại.

## Trạng thái dự án

Luồng chính từ quản lý danh mục, xác thực/phân quyền, hoạt động kho đến tạo và nhận Purchase Order đã được triển khai ở cả backend và Angular. Báo cáo biến động tồn, cấu hình ngưỡng cảnh báo, optimistic concurrency, idempotency, background worker và Redis cache-aside cũng đã có luồng sử dụng cơ bản.

Các phần đang tiếp tục hoàn thiện:

- Smoke test toàn bộ luồng trên SQL Server từ database sạch.
- Bổ sung frontend unit test cho auth, guard và interceptor.
- Bổ sung ảnh minh họa, chuẩn hóa structured logging và CI.
- Bổ sung một nhóm integration test trên relational database cho rollback, concurrency và database constraint.

Tiến độ chi tiết được theo dõi tại [`PROJECT_TASKS.md`](PROJECT_TASKS.md).

## Roadmap

- Hoàn thiện Sales Order để tạo luồng outbound `xác nhận → reservation → xuất theo lô → hoàn tất/hủy`.
- Nhận Purchase Order nhiều đợt.
- Integration test trên relational database cho rollback, concurrency, idempotency và constraint.
- Báo cáo giá trị tồn theo kho, hàng chậm luân chuyển và cảnh báo hạn sử dụng nâng cao.
- GitHub Actions cho build và test.
- Deployment public khi có nhu cầu demo; Docker, RabbitMQ và SignalR không thuộc phạm vi phiên bản CV hiện tại.

## Phạm vi

Đây là dự án portfolio đang phát triển, không phải sản phẩm production hoàn chỉnh. Mục tiêu hiện tại là thể hiện khả năng xây dựng API .NET, mô hình hóa nghiệp vụ kho, đảm bảo tính nhất quán dữ liệu, triển khai authentication/authorization và tích hợp Angular thành một luồng sử dụng hoàn chỉnh.


