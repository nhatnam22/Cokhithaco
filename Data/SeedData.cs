using System;
using System.Collections.Generic;
using System.Linq;
using CoKhiThaCo.Helpers;
using CoKhiThaCo.Models;
using CoKhiThaCo.Models.Enums;
using Microsoft.AspNetCore.Identity;

namespace CoKhiThaCo.Data
{
    public static class SeedData
    {
        public static void Initialize(MyDbContext context, UserManager<ApplicationUser> userManager, RoleManager<ApplicationRole> roleManager)
        {
            // 1. Seed Roles
            if (!roleManager.Roles.Any())
            {
                var roles = new[]
                {
                    new ApplicationRole { Name = GlobalRole.SystemAdmin.ToString(), Description = "System Administrator" },
                    new ApplicationRole { Name = GlobalRole.Member.ToString(), Description = "Member" }
                };
                foreach (var role in roles)
                {
                    roleManager.CreateAsync(role).Wait();
                }
            }

            // 2. Seed Position (Administrator)
            Guid adminPositionId;
            if (!context.Positions.Any())
            {
                var adminPosition = new Position
                {
                    PositionId = Guid.NewGuid(),
                    PositionName = "Administrator",
                    Description = "Quan tri vien he thong",
                    CreatedDate = TimeHelper.Now
                };
                context.Positions.Add(adminPosition);
                context.SaveChanges();
                adminPositionId = adminPosition.PositionId;
            }
            else
            {
                adminPositionId = context.Positions.First().PositionId;
            }

            // 3. Seed Admin User
            if (!userManager.Users.Any())
            {
                var adminUser = new ApplicationUser
                {
                    UserName = "admin@cokhithaco.com",
                    Email = "admin@cokhithaco.com",
                    FullName = "Administrator",
                    GlobalRole = GlobalRole.SystemAdmin,
                    PositionId = adminPositionId,
                    IsActive = true,
                    EmailConfirmed = true,
                    PhoneNumberConfirmed = true,
                    CreatedDate = TimeHelper.Now
                };
                userManager.CreateAsync(adminUser, "Admin@123").Wait();
            }

            var admin = userManager.Users.FirstOrDefault(u => u.UserName == "admin@cokhithaco.com");
            Guid? adminId = admin?.Id;

            // 4. Seed Menus (chỉ seed lần đầu, không xóa dữ liệu hiện có)
            if (!context.Menus.Any())
            {
                SeedMenus(context, adminId);
            }

            // 5. Seed Permissions (chỉ seed lần đầu, không xóa dữ liệu hiện có)
            if (!context.Permissions.Any())
            {
                SeedPermissions(context, adminId);
            }

            // 6. Seed CategoryCuttingFiles
            if (!context.CategoryCuttingFiles.Any())
            {
                SeedCategoryCuttingFiles(context, adminId);
            }

            // 7. Gán tất cả quyền cho Position Administrator
            if (adminPositionId != Guid.Empty && !context.PositionsPermissions.Any())
            {
                var permissionIds = context.Permissions.Where(p => !p.IsDeleted).Select(p => p.Id).ToList();
                foreach (var permissionId in permissionIds)
                {
                    context.PositionsPermissions.Add(new PositionsPermission
                    {
                        PositionId = adminPositionId,
                        PermissionId = permissionId
                    });
                }
                context.SaveChanges();
            }
        }

        /// <summary>
        /// Seed toàn bộ Menu theo cấu trúc phân cấp
        /// [CẬP NHẬT] Bổ sung menu: Ca làm việc, File cắt, Lệnh file cắt, Menu, Tổng quan kế hoạch
        /// </summary>
        private static void SeedMenus(MyDbContext context, Guid? adminId)
        {
            // === Menu cha ===
            var orgManagement = new Menu { Name = "Tổ chức", Path = null, Icon = "Organization", OrderDisplay = 1, CreatedDate = TimeHelper.Now, CreatedBy = adminId };
            var userManagement = new Menu { Name = "Người dùng", Path = null, Icon = "Users", OrderDisplay = 2, CreatedDate = TimeHelper.Now, CreatedBy = adminId };
            var systemManagement = new Menu { Name = "Hệ thống", Path = null, Icon = "Settings", OrderDisplay = 3, CreatedDate = TimeHelper.Now, CreatedBy = adminId };
            var productionManagement = new Menu { Name = "Sản xuất", Path = null, Icon = "Production", OrderDisplay = 4, CreatedDate = TimeHelper.Now, CreatedBy = adminId };

            context.Menus.AddRange(orgManagement, userManagement, systemManagement, productionManagement);
            context.SaveChanges();

            // === Menu con: Tổ chức ===
            var positions = new Menu { Name = "Chức vụ", Path = "/positions", Icon = "Position", OrderDisplay = 1, CreatedDate = TimeHelper.Now, CreatedBy = adminId };

            // === Menu con: Người dùng ===
            var users = new Menu { Name = "Danh sách người dùng", Path = "/users", Icon = "User", OrderDisplay = 1, CreatedDate = TimeHelper.Now, CreatedBy = adminId };

            // === Menu con: Hệ thống ===
            var permissions = new Menu { Name = "Phân quyền", Path = "/permissions", Icon = "Permission", OrderDisplay = 1, CreatedDate = TimeHelper.Now, CreatedBy = adminId };
            var menus = new Menu { Name = "Menu", Path = "/menus", Icon = "Menu", OrderDisplay = 2, CreatedDate = TimeHelper.Now, CreatedBy = adminId };
            var auditLogs = new Menu { Name = "Audit Log", Path = "/audit-logs", Icon = "Audit", OrderDisplay = 3, CreatedDate = TimeHelper.Now, CreatedBy = adminId };

            // === Menu con: Sản xuất ===
            var machineMenu = new Menu { Name = "Máy", Path = "/machines", Icon = "Machine", OrderDisplay = 1, CreatedDate = TimeHelper.Now, CreatedBy = adminId };
            var workShiftMenu = new Menu { Name = "Ca làm việc", Path = "/work-shifts", Icon = "WorkShift", OrderDisplay = 2, CreatedDate = TimeHelper.Now, CreatedBy = adminId };
            var productionCommandMenu = new Menu { Name = "Lệnh sản xuất", Path = "/production-commands", Icon = "Order", OrderDisplay = 3, CreatedDate = TimeHelper.Now, CreatedBy = adminId };
            var cuttingFileMenu = new Menu { Name = "File cắt", Path = "/cutting-files", Icon = "CuttingFile", OrderDisplay = 4, CreatedDate = TimeHelper.Now, CreatedBy = adminId };
            var commandCuttingFileMenu = new Menu { Name = "Lệnh file cắt", Path = "/command-cutting-files", Icon = "CommandCuttingFile", OrderDisplay = 5, CreatedDate = TimeHelper.Now, CreatedBy = adminId };
            var planningMenu = new Menu { Name = "Kế hoạch", Path = "/planning", Icon = "Planning", OrderDisplay = 6, CreatedDate = TimeHelper.Now, CreatedBy = adminId };
            var materialFeedingMenu = new Menu { Name = "Cấp phôi", Path = "/material-feeding-plans", Icon = "Material", OrderDisplay = 7, CreatedDate = TimeHelper.Now, CreatedBy = adminId };
            var executeMenu = new Menu { Name = "Thực thi", Path = "/execute", Icon = "Execute", OrderDisplay = 8, CreatedDate = TimeHelper.Now, CreatedBy = adminId };
            var summaryPlanMenu = new Menu { Name = "Tổng quan kế hoạch", Path = "/summary-plans", Icon = "Summary", OrderDisplay = 9, CreatedDate = TimeHelper.Now, CreatedBy = adminId };

            context.Menus.AddRange(positions, users, permissions, menus, auditLogs,
                machineMenu, workShiftMenu, productionCommandMenu, cuttingFileMenu,
                commandCuttingFileMenu, planningMenu, materialFeedingMenu, executeMenu, summaryPlanMenu);
            context.SaveChanges();

            // Gán ParentId
            positions.ParentId = orgManagement.Id;
            users.ParentId = userManagement.Id;
            permissions.ParentId = systemManagement.Id;
            menus.ParentId = systemManagement.Id;
            auditLogs.ParentId = systemManagement.Id;
            machineMenu.ParentId = productionManagement.Id;
            workShiftMenu.ParentId = productionManagement.Id;
            productionCommandMenu.ParentId = productionManagement.Id;
            cuttingFileMenu.ParentId = productionManagement.Id;
            commandCuttingFileMenu.ParentId = productionManagement.Id;
            planningMenu.ParentId = productionManagement.Id;
            materialFeedingMenu.ParentId = productionManagement.Id;
            executeMenu.ParentId = productionManagement.Id;
            summaryPlanMenu.ParentId = productionManagement.Id;
            context.SaveChanges();
        }

        /// <summary>
        /// Seed toàn bộ Permission theo RequirePermission trong Controllers
        /// [CẬP NHẬT] Bổ sung đầy đủ các permission codes từ controllers
        /// </summary>
        private static void SeedPermissions(MyDbContext context, Guid? adminId)
        {
            var menuLookup = context.Menus.Where(m => !m.IsDeleted).ToDictionary(m => m.Name);

            var permissions = new List<Permission>();

            // ====== Chức vụ (PositionController) ======
            permissions.AddRange(new[]
            {
                new Permission { Code = "Position.View", Name = "Xem chức vụ", Description = "Xem danh sách chức vụ", MenuId = menuLookup["Chức vụ"].Id, OrderDisplay = 1, CreatedDate = TimeHelper.Now, CreatedBy = adminId },
                new Permission { Code = "Position.Create", Name = "Tạo chức vụ", Description = "Tạo chức vụ", MenuId = menuLookup["Chức vụ"].Id, OrderDisplay = 2, CreatedDate = TimeHelper.Now, CreatedBy = adminId },
                new Permission { Code = "Position.Update", Name = "Sửa chức vụ", Description = "Sửa chức vụ", MenuId = menuLookup["Chức vụ"].Id, OrderDisplay = 3, CreatedDate = TimeHelper.Now, CreatedBy = adminId },
                new Permission { Code = "Position.Delete", Name = "Xóa chức vụ", Description = "Xóa chức vụ", MenuId = menuLookup["Chức vụ"].Id, OrderDisplay = 4, CreatedDate = TimeHelper.Now, CreatedBy = adminId },
                new Permission { Code = "Position.AssignPermission", Name = "Gán quyền", Description = "Gán quyền cho chức vụ", MenuId = menuLookup["Chức vụ"].Id, OrderDisplay = 5, CreatedDate = TimeHelper.Now, CreatedBy = adminId }
            });

            // ====== Người dùng (UserController) ======
            permissions.AddRange(new[]
            {
                new Permission { Code = "User.View", Name = "Xem người dùng", Description = "Xem danh sách người dùng", MenuId = menuLookup["Danh sách người dùng"].Id, OrderDisplay = 1, CreatedDate = TimeHelper.Now, CreatedBy = adminId },
                new Permission { Code = "User.Create", Name = "Tạo người dùng", Description = "Tạo người dùng", MenuId = menuLookup["Danh sách người dùng"].Id, OrderDisplay = 2, CreatedDate = TimeHelper.Now, CreatedBy = adminId },
                new Permission { Code = "User.Update", Name = "Sửa người dùng", Description = "Sửa người dùng", MenuId = menuLookup["Danh sách người dùng"].Id, OrderDisplay = 3, CreatedDate = TimeHelper.Now, CreatedBy = adminId },
                new Permission { Code = "User.Delete", Name = "Xóa người dùng", Description = "Xóa người dùng", MenuId = menuLookup["Danh sách người dùng"].Id, OrderDisplay = 4, CreatedDate = TimeHelper.Now, CreatedBy = adminId },
                new Permission { Code = "User.ToggleStatus", Name = "Bật/tắt trạng thái", Description = "Bật/tắt trạng thái người dùng", MenuId = menuLookup["Danh sách người dùng"].Id, OrderDisplay = 5, CreatedDate = TimeHelper.Now, CreatedBy = adminId }
            });

            // ====== Phân quyền (PermissionController) ======
            permissions.AddRange(new[]
            {
                new Permission { Code = "Permission.View", Name = "Xem quyền", Description = "Xem danh sách quyền", MenuId = menuLookup["Phân quyền"].Id, OrderDisplay = 1, CreatedDate = TimeHelper.Now, CreatedBy = adminId },
                new Permission { Code = "Permission.Create", Name = "Tạo quyền", Description = "Tạo quyền", MenuId = menuLookup["Phân quyền"].Id, OrderDisplay = 2, CreatedDate = TimeHelper.Now, CreatedBy = adminId },
                new Permission { Code = "Permission.Update", Name = "Sửa quyền", Description = "Sửa quyền", MenuId = menuLookup["Phân quyền"].Id, OrderDisplay = 3, CreatedDate = TimeHelper.Now, CreatedBy = adminId },
                new Permission { Code = "Permission.Delete", Name = "Xóa quyền", Description = "Xóa quyền", MenuId = menuLookup["Phân quyền"].Id, OrderDisplay = 4, CreatedDate = TimeHelper.Now, CreatedBy = adminId }
            });

            // ====== Menu (MenuController trong MenuAuditSeedController) ======
            permissions.AddRange(new[]
            {
                new Permission { Code = "Menu.View", Name = "Xem menu", Description = "Xem danh sách menu", MenuId = menuLookup["Menu"].Id, OrderDisplay = 1, CreatedDate = TimeHelper.Now, CreatedBy = adminId },
                new Permission { Code = "Menu.Create", Name = "Tạo menu", Description = "Tạo menu", MenuId = menuLookup["Menu"].Id, OrderDisplay = 2, CreatedDate = TimeHelper.Now, CreatedBy = adminId },
                new Permission { Code = "Menu.Update", Name = "Sửa menu", Description = "Sửa menu", MenuId = menuLookup["Menu"].Id, OrderDisplay = 3, CreatedDate = TimeHelper.Now, CreatedBy = adminId },
                new Permission { Code = "Menu.Delete", Name = "Xóa menu", Description = "Xóa menu", MenuId = menuLookup["Menu"].Id, OrderDisplay = 4, CreatedDate = TimeHelper.Now, CreatedBy = adminId }
            });

            // ====== Audit Log (AuditLogController) ======
            permissions.AddRange(new[]
            {
                new Permission { Code = "AuditLog.View", Name = "Xem audit log", Description = "Xem audit log", MenuId = menuLookup["Audit Log"].Id, OrderDisplay = 1, CreatedDate = TimeHelper.Now, CreatedBy = adminId }
            });

            // ====== Máy (MachineController) ======
            permissions.AddRange(new[]
            {
                new Permission { Code = "Machine.View", Name = "Xem máy", Description = "Xem danh sách máy", MenuId = menuLookup["Máy"].Id, OrderDisplay = 1, CreatedDate = TimeHelper.Now, CreatedBy = adminId },
                new Permission { Code = "Machine.Create", Name = "Tạo máy", Description = "Tạo máy mới", MenuId = menuLookup["Máy"].Id, OrderDisplay = 2, CreatedDate = TimeHelper.Now, CreatedBy = adminId },
                new Permission { Code = "Machine.Update", Name = "Sửa máy", Description = "Sửa thông tin máy", MenuId = menuLookup["Máy"].Id, OrderDisplay = 3, CreatedDate = TimeHelper.Now, CreatedBy = adminId },
                new Permission { Code = "Machine.Delete", Name = "Xóa máy", Description = "Xóa máy", MenuId = menuLookup["Máy"].Id, OrderDisplay = 4, CreatedDate = TimeHelper.Now, CreatedBy = adminId }
            });

            // ====== Phân công máy (MachineOperatorController) ======
            permissions.AddRange(new[]
            {
                new Permission { Code = "MachineOperator.View", Name = "Xem phân công máy", Description = "Xem phân công máy cho người dùng", MenuId = menuLookup["Máy"].Id, OrderDisplay = 5, CreatedDate = TimeHelper.Now, CreatedBy = adminId },
                new Permission { Code = "MachineOperator.Assign", Name = "Phân công máy", Description = "Phân công người dùng cho máy", MenuId = menuLookup["Máy"].Id, OrderDisplay = 6, CreatedDate = TimeHelper.Now, CreatedBy = adminId }
            });

            // ====== Ca làm việc (WorkShiftController) ======
            permissions.AddRange(new[]
            {
                new Permission { Code = "WorkShift.View", Name = "Xem ca làm việc", Description = "Xem danh sách ca làm việc", MenuId = menuLookup["Ca làm việc"].Id, OrderDisplay = 1, CreatedDate = TimeHelper.Now, CreatedBy = adminId },
                new Permission { Code = "WorkShift.Create", Name = "Tạo ca làm việc", Description = "Tạo ca làm việc", MenuId = menuLookup["Ca làm việc"].Id, OrderDisplay = 2, CreatedDate = TimeHelper.Now, CreatedBy = adminId },
                new Permission { Code = "WorkShift.Update", Name = "Sửa ca làm việc", Description = "Sửa ca làm việc", MenuId = menuLookup["Ca làm việc"].Id, OrderDisplay = 3, CreatedDate = TimeHelper.Now, CreatedBy = adminId },
                new Permission { Code = "WorkShift.Delete", Name = "Xóa ca làm việc", Description = "Xóa ca làm việc", MenuId = menuLookup["Ca làm việc"].Id, OrderDisplay = 4, CreatedDate = TimeHelper.Now, CreatedBy = adminId }
            });

            // ====== Nghỉ giải lao (ShiftBreak trong WorkShiftController) ======
            permissions.AddRange(new[]
            {
                new Permission { Code = "ShiftBreak.View", Name = "Xem nghỉ giải lao", Description = "Xem danh sách nghỉ giải lao", MenuId = menuLookup["Ca làm việc"].Id, OrderDisplay = 5, CreatedDate = TimeHelper.Now, CreatedBy = adminId },
                new Permission { Code = "ShiftBreak.Create", Name = "Tạo nghỉ giải lao", Description = "Tạo nghỉ giải lao", MenuId = menuLookup["Ca làm việc"].Id, OrderDisplay = 6, CreatedDate = TimeHelper.Now, CreatedBy = adminId },
                new Permission { Code = "ShiftBreak.Update", Name = "Sửa nghỉ giải lao", Description = "Sửa nghỉ giải lao", MenuId = menuLookup["Ca làm việc"].Id, OrderDisplay = 7, CreatedDate = TimeHelper.Now, CreatedBy = adminId },
                new Permission { Code = "ShiftBreak.Delete", Name = "Xóa nghỉ giải lao", Description = "Xóa nghỉ giải lao", MenuId = menuLookup["Ca làm việc"].Id, OrderDisplay = 8, CreatedDate = TimeHelper.Now, CreatedBy = adminId }
            });

            // ====== Lệnh sản xuất (ProductionCommandController) ======
            permissions.AddRange(new[]
            {
                new Permission { Code = "ProductionCommand.View", Name = "Xem lệnh sản xuất", Description = "Xem danh sách lệnh sản xuất", MenuId = menuLookup["Lệnh sản xuất"].Id, OrderDisplay = 1, CreatedDate = TimeHelper.Now, CreatedBy = adminId },
                new Permission { Code = "ProductionCommand.Create", Name = "Tạo lệnh sản xuất", Description = "Tạo lệnh sản xuất", MenuId = menuLookup["Lệnh sản xuất"].Id, OrderDisplay = 2, CreatedDate = TimeHelper.Now, CreatedBy = adminId },
                new Permission { Code = "ProductionCommand.Update", Name = "Sửa lệnh sản xuất", Description = "Sửa lệnh sản xuất", MenuId = menuLookup["Lệnh sản xuất"].Id, OrderDisplay = 3, CreatedDate = TimeHelper.Now, CreatedBy = adminId },
                new Permission { Code = "ProductionCommand.Delete", Name = "Xóa lệnh sản xuất", Description = "Xóa lệnh sản xuất", MenuId = menuLookup["Lệnh sản xuất"].Id, OrderDisplay = 4, CreatedDate = TimeHelper.Now, CreatedBy = adminId },
                new Permission { Code = "ProductionCommand.Import", Name = "Import lệnh sản xuất", Description = "Import lệnh sản xuất từ Excel", MenuId = menuLookup["Lệnh sản xuất"].Id, OrderDisplay = 5, CreatedDate = TimeHelper.Now, CreatedBy = adminId }
            });

            // ====== File cắt (CuttingFileController) ======
            permissions.AddRange(new[]
            {
                new Permission { Code = "CuttingFile.View", Name = "Xem file cắt", Description = "Xem danh sách file cắt", MenuId = menuLookup["File cắt"].Id, OrderDisplay = 1, CreatedDate = TimeHelper.Now, CreatedBy = adminId },
                new Permission { Code = "CuttingFile.Create", Name = "Tạo file cắt", Description = "Tạo file cắt", MenuId = menuLookup["File cắt"].Id, OrderDisplay = 2, CreatedDate = TimeHelper.Now, CreatedBy = adminId },
                new Permission { Code = "CuttingFile.Update", Name = "Sửa file cắt", Description = "Sửa file cắt", MenuId = menuLookup["File cắt"].Id, OrderDisplay = 3, CreatedDate = TimeHelper.Now, CreatedBy = adminId },
                new Permission { Code = "CuttingFile.Delete", Name = "Xóa file cắt", Description = "Xóa file cắt", MenuId = menuLookup["File cắt"].Id, OrderDisplay = 4, CreatedDate = TimeHelper.Now, CreatedBy = adminId }
            });

            // ====== Loại file cắt (CategoryCuttingFileController) ======
            permissions.AddRange(new[]
            {
                new Permission { Code = "CategoryCuttingFile.View", Name = "Xem loại file cắt", Description = "Xem danh sách loại file cắt", MenuId = menuLookup["File cắt"].Id, OrderDisplay = 5, CreatedDate = TimeHelper.Now, CreatedBy = adminId },
                new Permission { Code = "CategoryCuttingFile.Create", Name = "Tạo loại file cắt", Description = "Tạo loại file cắt", MenuId = menuLookup["File cắt"].Id, OrderDisplay = 6, CreatedDate = TimeHelper.Now, CreatedBy = adminId },
                new Permission { Code = "CategoryCuttingFile.Update", Name = "Sửa loại file cắt", Description = "Sửa loại file cắt", MenuId = menuLookup["File cắt"].Id, OrderDisplay = 7, CreatedDate = TimeHelper.Now, CreatedBy = adminId },
                new Permission { Code = "CategoryCuttingFile.Delete", Name = "Xóa loại file cắt", Description = "Xóa loại file cắt", MenuId = menuLookup["File cắt"].Id, OrderDisplay = 8, CreatedDate = TimeHelper.Now, CreatedBy = adminId }
            });

            // ====== Lệnh file cắt (CommandCuttingFileController) ======
            permissions.AddRange(new[]
            {
                new Permission { Code = "CommandCuttingFile.View", Name = "Xem lệnh file cắt", Description = "Xem danh sách lệnh file cắt", MenuId = menuLookup["Lệnh file cắt"].Id, OrderDisplay = 1, CreatedDate = TimeHelper.Now, CreatedBy = adminId },
                new Permission { Code = "CommandCuttingFile.Create", Name = "Tạo lệnh file cắt", Description = "Tạo lệnh file cắt", MenuId = menuLookup["Lệnh file cắt"].Id, OrderDisplay = 2, CreatedDate = TimeHelper.Now, CreatedBy = adminId },
                new Permission { Code = "CommandCuttingFile.Update", Name = "Sửa lệnh file cắt", Description = "Sửa lệnh file cắt", MenuId = menuLookup["Lệnh file cắt"].Id, OrderDisplay = 3, CreatedDate = TimeHelper.Now, CreatedBy = adminId },
                new Permission { Code = "CommandCuttingFile.Delete", Name = "Xóa lệnh file cắt", Description = "Xóa lệnh file cắt", MenuId = menuLookup["Lệnh file cắt"].Id, OrderDisplay = 4, CreatedDate = TimeHelper.Now, CreatedBy = adminId }
            });

            // ====== Kế hoạch (PlanningController) ======
            permissions.AddRange(new[]
            {
                new Permission { Code = "Planning.View", Name = "Xem kế hoạch", Description = "Xem kế hoạch", MenuId = menuLookup["Kế hoạch"].Id, OrderDisplay = 1, CreatedDate = TimeHelper.Now, CreatedBy = adminId },
                new Permission { Code = "Planning.Create", Name = "Tạo kế hoạch", Description = "Tạo kế hoạch", MenuId = menuLookup["Kế hoạch"].Id, OrderDisplay = 2, CreatedDate = TimeHelper.Now, CreatedBy = adminId },
                new Permission { Code = "Planning.Schedule", Name = "Lên kế hoạch", Description = "Lên lịch kế hoạch", MenuId = menuLookup["Kế hoạch"].Id, OrderDisplay = 3, CreatedDate = TimeHelper.Now, CreatedBy = adminId },
                new Permission { Code = "Planning.Update", Name = "Sửa kế hoạch", Description = "Sửa kế hoạch", MenuId = menuLookup["Kế hoạch"].Id, OrderDisplay = 4, CreatedDate = TimeHelper.Now, CreatedBy = adminId },
                new Permission { Code = "Planning.Delete", Name = "Xóa kế hoạch", Description = "Xóa kế hoạch", MenuId = menuLookup["Kế hoạch"].Id, OrderDisplay = 5, CreatedDate = TimeHelper.Now, CreatedBy = adminId }
            });

            // ====== Cấp phôi (MaterialFeedingPlanController) ======
            permissions.AddRange(new[]
            {
                new Permission { Code = "MaterialFeedingPlan.View", Name = "Xem cấp phôi", Description = "Xem kế hoạch cấp phôi", MenuId = menuLookup["Cấp phôi"].Id, OrderDisplay = 1, CreatedDate = TimeHelper.Now, CreatedBy = adminId },
                new Permission { Code = "MaterialFeedingPlan.Create", Name = "Tạo cấp phôi", Description = "Tạo kế hoạch cấp phôi", MenuId = menuLookup["Cấp phôi"].Id, OrderDisplay = 2, CreatedDate = TimeHelper.Now, CreatedBy = adminId },
                new Permission { Code = "MaterialFeedingPlan.Update", Name = "Sửa cấp phôi", Description = "Sửa kế hoạch cấp phôi", MenuId = menuLookup["Cấp phôi"].Id, OrderDisplay = 3, CreatedDate = TimeHelper.Now, CreatedBy = adminId },
                new Permission { Code = "MaterialFeedingPlan.Complete", Name = "Xác nhận cấp phôi", Description = "Xác nhận đã cấp phôi", MenuId = menuLookup["Cấp phôi"].Id, OrderDisplay = 4, CreatedDate = TimeHelper.Now, CreatedBy = adminId },
                new Permission { Code = "MaterialFeedingPlan.Delete", Name = "Xóa cấp phôi", Description = "Xóa kế hoạch cấp phôi", MenuId = menuLookup["Cấp phôi"].Id, OrderDisplay = 5, CreatedDate = TimeHelper.Now, CreatedBy = adminId }
            });

            // ====== Thực thi (ExecuteController) ======
            permissions.AddRange(new[]
            {
                new Permission { Code = "Execute.View", Name = "Xem thực thi", Description = "Xem thực thi", MenuId = menuLookup["Thực thi"].Id, OrderDisplay = 1, CreatedDate = TimeHelper.Now, CreatedBy = adminId },
                new Permission { Code = "Execute.Receive", Name = "Nhận phôi", Description = "Xác nhận nhận phôi", MenuId = menuLookup["Thực thi"].Id, OrderDisplay = 2, CreatedDate = TimeHelper.Now, CreatedBy = adminId },
                new Permission { Code = "Execute.Start", Name = "Bắt đầu thực thi", Description = "Bắt đầu thực thi lệnh", MenuId = menuLookup["Thực thi"].Id, OrderDisplay = 3, CreatedDate = TimeHelper.Now, CreatedBy = adminId },
                new Permission { Code = "Execute.End", Name = "Kết thúc thực thi", Description = "Kết thúc lệnh thực thi", MenuId = menuLookup["Thực thi"].Id, OrderDisplay = 4, CreatedDate = TimeHelper.Now, CreatedBy = adminId },
                new Permission { Code = "Execute.Continute", Name = "Tiếp tục thực thi", Description = "Tiếp tục thực thi sau tạm dừng", MenuId = menuLookup["Thực thi"].Id, OrderDisplay = 5, CreatedDate = TimeHelper.Now, CreatedBy = adminId },
                new Permission { Code = "Execute.Complete", Name = "Hoàn thành thực thi", Description = "Hoàn thành hoặc hủy thực thi", MenuId = menuLookup["Thực thi"].Id, OrderDisplay = 6, CreatedDate = TimeHelper.Now, CreatedBy = adminId }
            });

            // ====== Tổng quan kế hoạch (SummaryPlanController) ======
            permissions.AddRange(new[]
            {
                new Permission { Code = "Summary.View", Name = "Xem tổng quan", Description = "Xem tổng quan kế hoạch", MenuId = menuLookup["Tổng quan kế hoạch"].Id, OrderDisplay = 1, CreatedDate = TimeHelper.Now, CreatedBy = adminId }
            });

            context.Permissions.AddRange(permissions);
            context.SaveChanges();
        }

        private static void SeedCategoryCuttingFiles(MyDbContext context, Guid? adminId)
        {
            var categories = new List<CategoryCuttingFile>
            {
                new CategoryCuttingFile { Id = Guid.NewGuid(), Code = "CCF-001", Name = "Cắt công nghiệp", Description = "Loại file cắt công nghiệp", CreatedDate = TimeHelper.Now, CreatedBy = adminId },
                new CategoryCuttingFile { Id = Guid.NewGuid(), Code = "CCF-002", Name = "Cắt CNC", Description = "Loại file cắt CNC", CreatedDate = TimeHelper.Now, CreatedBy = adminId },
                new CategoryCuttingFile { Id = Guid.NewGuid(), Code = "CCF-003", Name = "Cắt Laser", Description = "Loại file cắt Laser", CreatedDate = TimeHelper.Now, CreatedBy = adminId }
            };
            context.CategoryCuttingFiles.AddRange(categories);
            context.SaveChanges();
        }
    }
}