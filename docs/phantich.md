Phân tích ảnh hưởng: Thay đổi CommandCuttingFile từ 1-1 → Many-to-Many với ProductionCommand
Hiện tại vs Mới
HIỆN TẠI (1-1):                          MỚI (Many-to-Many):
CommandCuttingFile                        CommandCuttingFile
├── CuttingFileId                         ├── CuttingFileId
├── ProductionCommandId  ← FK đơn         ├── ICollection<ProductionCommand>
├── ProductionCommand    ← Nav đơn        └── (các thuộc tính còn lại giữ nguyên)
└── (các thuộc tính khác)

→ Mỗi (CuttingFile, ProductionCommand)   → 1 CuttingFile = 1 CommandCuttingFile
  tạo 1 CommandCuttingFile riêng           với nhiều ProductionCommand
NHÓM 1: Bắt buộc sửa (Logic nghiệp vụ)
1.1 Models/CommandCuttingFile.cs
- Xóa: ProductionCommandId (Guid), ProductionCommand (nav đơn)
- Thêm: ICollection<ProductionCommand> ProductionCommands { get; set; }
1.2 Data/MyDbContext.cs (line 130-133)
- Sửa: Bỏ unique index (CuttingFileId, ProductionCommandId) → chuyển thành index trên CuttingFileId (vì 1 CuttingFile = 1 CommandCuttingFile)
- Thêm: Cấu hình many-to-many relationship (EF Core tự tạo bảng join CommandCuttingFileProductionCommand)
1.3 Services/CommandCuttingFileService.cs — Ảnh hưởng nặng nhất
Method	Vấn đề	Sửa
GetPaged (line 109-110)	Query c.ProductionCommand.CommandCode → sai nav	Đổi sang c.ProductionCommands.Any(pc => ...)
GetPaged (line 114)	Filter c.ProductionCommandId == cmdId	Đổi sang c.ProductionCommands.Any(pc => pc.Id == cmdId)
GetPaged (line 125)	items.Select(c => c.ProductionCommandId)	Đổi sang collection
GetPaged (line 133-136)	Logic siblings (groupby CuttingFileId)	Xóa — không cần nữa vì 1 CCF = 1 CuttingFile
GetById (line 169-177)	Logic siblings	Xóa — lấy trực tiếp item.ProductionCommands
Create (line 193-215)	Tạo N CommandCuttingFile (1 per ProductionCommandId)	Sửa: Tạo 1 CommandCuttingFile với N ProductionCommand
ValidateNoDuplicate (line 87-95)	Check trùng theo ProductionCommandId	Sửa: Check trùng theo CuttingFileId (1 CCF = 1 CuttingFile)
MapToDto (line 347-393)	Nhận List<Guid> productionCommandIds	Sửa: Nhận trực tiếp ICollection<ProductionCommand>
1.4 Services/ProductionCommandService.cs
Line	Vấn đề	Sửa
51-52, 72-73	c.ProductionCommandId == p.Id	Đổi sang join table query
125	c.ProductionCommandId == id (check xóa)	Đổi sang c.ProductionCommands.Any(pc => pc.Id == id)
1.5 Services/ExecuteService.cs
Line	Vấn đề	Sửa
54	Include "Planning.CommandCuttingFile.ProductionCommand"	→ "Planning.CommandCuttingFile.ProductionCommands"
667	e.Planning?.CommandCuttingFile?.ProductionCommand?.CommandCode	→ .ProductionCommands.FirstOrDefault()?.CommandCode hoặc join nhiều
1.6 Services/MaterialFeedingPlanService.cs
Line	Vấn đề	Sửa
37	Include "Planning.CommandCuttingFile.ProductionCommand"	→ "Planning.CommandCuttingFile.ProductionCommands"
88	mf.Planning.CommandCuttingFile.ProductionCommand.CommandCode.Contains(...)	→ .ProductionCommands.Any(pc => ...)
322	mf.Planning?.CommandCuttingFile?.ProductionCommand?.CommandCode	→ .ProductionCommands.FirstOrDefault()?.CommandCode
1.7 Services/PlanningService.cs
Line	Vấn đề	Sửa
58	Include "CommandCuttingFile.ProductionCommand"	→ "CommandCuttingFile.ProductionCommands"
896-897	p.CommandCuttingFile?.ProductionCommand?.CommandCode/OrderName	→ .ProductionCommands.FirstOrDefault()?.CommandCode
1.8 Services/SummaryPlanService.cs
Line	Vấn đề	Sửa
33	Include "CommandCuttingFile.ProductionCommand"	→ "CommandCuttingFile.ProductionCommands"
80	p.CommandCuttingFile.ProductionCommand.CommandCode.Contains(...)	→ .ProductionCommands.Any(pc => ...)
120	ccfs.Values.Select(c => c.ProductionCommandId)	→ lấy từ collection
146-153	ccf?.ProductionCommandId → map DTO	→ lấy từ collection
NHÓM 2: DTO — Cần kiểm tra và sửa
2.1 Models/Dtos/CommandCuttingFile/CommandCuttingFileDtos.cs
- CommandCuttingFileDto.ProductionCommands (List<CommandProductionCommandDto>) → Giữ nguyên (đã đúng)
- CreateCommandCuttingFileRequest.ProductionCommandIds (List<Guid>) → Giữ nguyên (vẫn nhận list ID)
- CommandCuttingFileListQuery.ProductionCommandId → Giữ nguyên (filter theo ProductionCommand trong collection)
NHÓM 3: Migration — Bắt buộc
3.1 Migration cần làm:
1. Tạo bảng join: CommandCuttingFileProductionCommand
   ├── CommandCuttingFileId (FK)
   └── ProductionCommandId (FK)

2. Migrate dữ liệu cũ:
   - Mỗi dòng CommandCuttingFile hiện tại có 1 ProductionCommandId
   - Insert vào bảng join: (CommandCuttingFileId, ProductionCommandId)
   - Nếu nhiều CommandCuttingFile cùng CuttingFileId → gộp thành 1 dòng mới

3. Xóa cột: ProductionCommandId khỏi CommandCuttingFiles

4. Xóa index cũ: IX_CommandCuttingFiles_CuttingFileId_ProductionCommandId
   IX_CommandCuttingFiles_ProductionCommandId

5. Thêm index mới: IX_CommandCuttingFiles_CuttingFileId (unique)
NHÓM 4: Ảnh hưởng phụ (Cần kiểm tra thêm)
File	Ảnh hưởng
Planning model	CommandCuttingFileId FK → Không đổi (vẫn trỏ đến 1 CommandCuttingFile)
ExecuteService.Complete	_commandCuttingFileService.UpdateCompletedQuantity(ccfId.Value, ...) → Không đổi
PlanningService.UpdateCommandCuttingFilePlannedQuantity	Không đổi
CuttingFileService	Query commandFileRepo → kiểm tra logic có dùng ProductionCommandId không
Tóm tắt mức độ ảnh hưởng
Mức độ	Số file	File
Nặng (viết lại)	1	CommandCuttingFileService.cs
Trung bình (sửa query/include)	4	ExecuteService, MaterialFeedingPlanService, PlanningService, SummaryPlanService
Nhẹ (sửa model/config)	2	CommandCuttingFile.cs, MyDbContext.cs
Migration	1	Migration mới
Không đổi	2	Planning.cs, DTOs 
Tôi đã đọc dữ liệu. Đây là phân tích:
Phân tích dữ liệu hiện tại
Từ file phantichchucnangimport.md:
- ~162 dòng CommandCuttingFile với cấu trúc cũ (1 ProductionCommandId mỗi dòng)
- Nhiều dòng trùng CuttingFileId nhưng khác ProductionCommandId (VD: 47805E0C xuất hiện ở dòng 4 và 25)
- Status = 0 (Created) cho hầu hết, một số = 1 (Planned)
- CompletedQuantity = 0 cho tất cả → chưa có dữ liệu sản xuất
- Một số dòng IsDeleted = 1
Vấn đề khi migrate
Cấu trúc mới yêu cầu: 1 CommandCuttingFile = 1 CuttingFile + Nhiều ProductionCommand
Nhưng hiện tại: 1 CuttingFile có N CommandCuttingFile (mỗi dòng 1 ProductionCommandId)
→ Cần gộp các dòng trùng CuttingFileId thành 1 dòng, chuyển ProductionCommandId vào bảng join.
So sánh 3 phương án
 	Phương án 1: Xóa + Import lại	Phương án 2: SQL consolidate	Phương án 3: Backup Excel + Import
Độ phức tạp	Thấp	Cao (SQL phức tạp)	Trung bình
Giữ dữ liệu Planning	❌ Mất	✅ Giữ	❌ Mất (tạo lại)
Giữ dữ liệu MaterialFeedingPlan/Execute	❌ Mất	✅ Giữ	❌ Mất
Rủi ro lỗi dữ liệu	Thấp	Cao (FK updates)	Trung bình
Thời gian	Nhanh	Chậm	Trung bình
Đánh giá
Dữ liệu hiện tại:
- CompletedQuantity = 0 → chưa có sản xuất
- Planning/MaterialFeedingPlan/Execute → có thể đã tạo nhưng chưa dùng nhiều
- Dữ liệu có thể import lại từ Excel (đã có format)
→ Phương án 1 (Xóa + Import lại) là tối ưu nhất vì:
1. Dữ liệu chưa có giá trị sản xuất
2. Tránh được SQL consolidate phức tạp, dễ sai sót
3. Import Excel đã có sẵn chức năng
4. Schema mới sạch sẽ, không cần fix data
Kế hoạch chi tiết (Phương án 1)
1. Backup dữ liệu CommandCuttingFile ra file Excel (dự phòng)
2. Xóa theo thứ tự FK:
   - Execute → MaterialFeedingPlan → Planning → CommandCuttingFile
3. Sửa migration ChangeCommandCuttingFileToManyToMany:
   - Bỏ SQL migrate dữ liệu cũ (không cần nữa)
   - Chỉ cần: tạo bảng join + drop cột ProductionCommandId
4. Áp dụng migration
5. Import lại dữ liệu từ Excel vào CommandCuttingFile mới
Bạn xác nhận phương án 1, tôi sẽ triển khai?