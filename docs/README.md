# CoKhiThaCo - Template API

Template ASP.NET Core Web API (**.NET 9**) tái sử dụng kiến trúc của `ThacoManagementProject`.

## Kiến trúc

```
Controller  →  Service (interface + impl cùng file)  →  IUnitOfWork  →  Repository  →  DbContext
                 ↑
          [Authorize] + [RequirePermission("Module.Action")]
```

| Thành phần | Vị trí |
|---|---|
| Attributes phân quyền | `Attributes/` |
| JWT + AuthService | `Services/AuthService.cs`, `Startup.cs` |
| Identity types | `Data/ApplicationUser.cs` |
| DbContext + Audit | `Data/MyDbContext.cs` |
| Seed | `Data/SeedData.cs` |
| Repository / UoW | `Repositories/`, `UOW/` |
| Filter/Sort dynamic | `Helpers/FilterHelper.cs`, `SortHelper.cs` |
| Export base | `Services/Export/` (EPPlus.Core + PdfSharp) |

## Chạy local

```bash
# 1. SQL Server bằng Docker
docker compose up -d mssql

# 2. Build & run API
dotnet restore
dotnet run
# Swagger: http://localhost:5002
```

Hoặc chạy cả stack:

```bash
docker compose up --build
# API: http://localhost:5002
```

## Đăng nhập mặc định

```
POST /api/auth/login
{
  "userName": "admin@cokhithaco.com",
  "password": "Admin@123"
}
```

Seed tự chạy khi app khởi động (roles, menus, permissions, admin user).

## Phân quyền

- **GlobalRole claim**: `SystemAdmin`, `Member`
- **Permission claim**: mỗi code `{Entity}.{Action}` (`Department.View`, `User.Create`, `Report.Export`...)
- Quyền gắn theo **Position** → khi login nạp vào JWT
- Admin bypass mọi permission module (không hardcode user id)

### Attributes

| Attribute | Mô tả |
|---|---|
| `[Authorize]` | JWT bắt buộc |
| `[RequirePermission("X.Y")]` | Check permission claim (OR nếu nhiều) |
| `[RequireGlobalRole(GlobalRole.X)]` | Check role claim |
| `[RequireGlobalRolePermission(role, "X.Y")]` | Role **hoặc** permission |
| `[ApiKey]` | Header `ApiKey` (M2M) |

## Export

Pattern: Template Method + Builder.

```
BaseExportService<TRequest,TData>
  ├─ FetchData
  ├─ GetExcelBuilder  → BaseExcelBuilder (EPPlus.Core)
  └─ GetPdfBuilder    → BasePdfBuilder   (PdfSharp)
```

Demo:

```
GET /api/report-export/departments/excel?filters=...
GET /api/report-export/departments/pdf
```

### Thêm report mới

1. DTO trong `Models/Dtos/Export/ExportDtos.cs`
2. Builder Excel/PDF trong `Services/Export/Core/`
3. Service subclass `BaseExportService`
4. Đăng ký DI trong `Startup.cs`
5. Action trong `ReportExportController` + `[RequirePermission("Report.Export")]`

## Thêm module mới

1. Entity kế thừa `Auditable` → `Models/{Entity}.cs`
2. DbSet + config trong `Data/MyDbContext.cs`
3. DTO trong `Models/Dtos/{Module}/`
4. Service trong `Services/{Module}Service.cs`
5. Controller + `[RequirePermission]`
6. Seed permission codes trong `Data/SeedData.cs`
7. Đăng ký DI trong `Startup.cs`
8. `dotnet ef migrations add {Name}`

## API chính

| Method | Route | Permission |
|---|---|---|
| POST | `/api/auth/login` | - |
| POST | `/api/auth/logout` | JWT |
| POST | `/api/auth/change-password` | JWT |
| GET/POST/PUT/DELETE | `/api/department` | `Department.*` |
| GET/POST/PUT/DELETE | `/api/position` | `Position.*` |
| GET/POST/PUT/DELETE | `/api/user` | `User.*` |
| GET/POST/PUT/DELETE | `/api/permission` | `Permission.*` |
| POST | `/api/permission/assign-position` | `Position.AssignPermission` |
| GET | `/api/audit-logs` | `AuditLog.View` |
| GET | `/api/report-export/departments/excel` | `Report.Export` |

## Quy ước

- Response thống nhất `ApiResponse<T>` / `PagedResult<T>`
- Soft delete: `IsDeleted = true`
- Timezone: luôn `TimeHelper.Now` (Asia/Ho_Chi_Minh)
- Filter query: `filters=DeptName:contains:Ke toan`
- Sort query: `sorts=DeptName:asc,Level:desc`
- Paging: `?page=1&pageSize=20`

## Packages

- .NET 9, EF Core 8, ASP.NET Core Identity
- JWT Bearer, Swashbuckle
- **EPPlus.Core** (Excel)
- **PDFsharp** (PDF, thay QuestPDF)
- Newtonsoft.Json

> Target `net9.0` do máy dev chưa cài runtime .NET 8. Docker image cũng dùng aspnet:9.0.
