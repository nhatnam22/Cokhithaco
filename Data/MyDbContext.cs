using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using CoKhiThaCo.Helpers;
using CoKhiThaCo.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CoKhiThaCo.Data
{
    public class MyDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, Guid, IdentityUserClaim<Guid>,
        ApplicationUserRole, IdentityUserLogin<Guid>,
        IdentityRoleClaim<Guid>, IdentityUserToken<Guid>>
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public MyDbContext(DbContextOptions<MyDbContext> options, IHttpContextAccessor httpContextAccessor = null)
            : base(options)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public DbSet<Position> Positions { get; set; }
        public DbSet<Menu> Menus { get; set; }
        public DbSet<Permission> Permissions { get; set; }
        public DbSet<PositionsPermission> PositionsPermissions { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<Machine> Machines { get; set; }
        public DbSet<MachineOperator> MachineOperators { get; set; }
        public DbSet<WorkShift> WorkShifts { get; set; }
        public DbSet<WorkShiftMachine> WorkShiftMachines { get; set; }
        public DbSet<Planning> Plannings { get; set; }
        public DbSet<Execute> Executes { get; set; }
        public DbSet<SubExecute> SubExecutes { get; set; }
        public DbSet<CategoryCuttingFile> CategoryCuttingFiles { get; set; }
        public DbSet<CuttingFile> CuttingFiles { get; set; }
        public DbSet<MachineCategoryCuttingFile> MachineCategoryCuttingFiles { get; set; }
        public DbSet<ProductionCommand> ProductionCommands { get; set; }
        public DbSet<CommandCuttingFile> CommandCuttingFiles { get; set; }
        public DbSet<ShiftBreak> ShiftBreaks { get; set; }
        public DbSet<MaterialFeedingPlan> MaterialFeedingPlans { get; set; }
        public DbSet<PlanningSegment> PlanningSegments { get; set; }
        public DbSet<ReceiveMaterialFeed> ReceiveMaterialFeeds { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            foreach (var relationship in builder.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
            {
                relationship.DeleteBehavior = DeleteBehavior.Restrict;
            }

            builder.Entity<ApplicationUserRole>(pq =>
            {
                pq.HasKey(ur => new { ur.UserId, ur.RoleId });
                pq.HasOne(ur => ur.Role)
                    .WithMany(r => r.UserRoles)
                    .HasForeignKey(ur => ur.RoleId)
                    .IsRequired();
                pq.HasOne(ur => ur.User)
                    .WithMany(r => r.UserRoles)
                    .HasForeignKey(ur => ur.UserId)
                    .IsRequired();
            });

            builder.Entity<ApplicationUser>(entity =>
            {
                entity.ToTable("Users");
                entity.HasIndex(e => e.UserName).IsUnique();
                entity.HasIndex(e => e.Email).IsUnique();
            });

            builder.Entity<ApplicationRole>(entity =>
            {
                entity.ToTable("Roles");
                entity.HasIndex(e => e.Name).IsUnique();
            });

            builder.Entity<Permission>(entity =>
            {
                entity.HasIndex(e => e.Code).IsUnique();
            });

            builder.Entity<Position>(entity =>
            {
                entity.HasIndex(e => e.PositionName).IsUnique();
            });

            builder.Entity<PositionsPermission>(entity =>
            {
                entity.HasIndex(e => new { e.PositionId, e.PermissionId }).IsUnique();
            });

            builder.Entity<Machine>(entity =>
            {
                entity.HasIndex(e => e.MachineCode).IsUnique();
            });

            builder.Entity<MachineOperator>(entity =>
            {
                entity.HasIndex(e => new { e.UserId, e.MachineId }).IsUnique();
            });

            builder.Entity<CategoryCuttingFile>(entity =>
            {
                entity.HasIndex(e => e.Code).IsUnique();
            });

            builder.Entity<CuttingFile>(entity =>
            {
                entity.HasIndex(e => e.Code).IsUnique();
            });

            builder.Entity<ProductionCommand>(entity =>
            {
                entity.HasIndex(e => e.CommandCode).IsUnique();
            });

            builder.Entity<MachineCategoryCuttingFile>(entity =>
            {
                entity.HasIndex(e => new { e.MachineId, e.CategoryCuttingFileId }).IsUnique();
            });

            // [THAY ĐỔI NHIỆP VỤ] 1 CommandCuttingFile = 1 CuttingFile + Nhiều ProductionCommand
            // Bỏ unique index cũ (CuttingFileId + ProductionCommandId) vì không còn FK đơn ProductionCommandId
            // Thay bằng index trên CuttingFileId (1 CCF = 1 CuttingFile)
            builder.Entity<CommandCuttingFile>(entity =>
            {
                entity.HasIndex(e => e.CuttingFileId);
                // Cấu hình many-to-many giữa CommandCuttingFile và ProductionCommand
                // EF Core tự tạo bảng join CommandCuttingFileProductionCommand
                entity.HasMany(c => c.ProductionCommands)
                    .WithMany()
                    .UsingEntity(j => j.ToTable("CommandCuttingFileProductionCommand"));
            });

            builder.Entity<Machine>(entity =>
            {
                entity.Property(e => e.PowerKw).HasColumnType("decimal(18,2)");
                entity.Property(e => e.MinCutLengthMm).HasColumnType("decimal(18,2)");
                entity.Property(e => e.MaxCutLengthMm).HasColumnType("decimal(18,2)");
                entity.Property(e => e.MinCutThicknessMm).HasColumnType("decimal(18,2)");
                entity.Property(e => e.MaxCutThicknessMm).HasColumnType("decimal(18,2)");
            });
            builder.Entity<WorkShift>(entity =>
            {
                entity.Property(e => e.StandardTimeMinutes).HasColumnType("decimal(18,2)");
            });
            builder.Entity<ShiftBreak>(entity =>
            {
                entity.Property(e => e.DurationMinutes).HasColumnType("decimal(18,2)");
            });

            builder.Entity<CommandCuttingFile>(entity =>
            {
                entity.Property(e => e.CuttingTime).HasColumnType("decimal(18,2)");
                entity.Property(e => e.CuttingLength).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Length).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Width).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Thickness).HasColumnType("decimal(18,2)");
                entity.Property(e => e.RemainLength).HasColumnType("decimal(18,2)");
                entity.Property(e => e.RemainWidth).HasColumnType("decimal(18,2)");
                entity.Property(e => e.RemainThickness).HasColumnType("decimal(18,2)");
            });

            builder.Entity<Execute>(entity =>
            {
                entity.Property(e => e.ActualTime).HasColumnType("decimal(18,2)");
            });

            builder.Entity<MaterialFeedingPlan>(entity =>
            {
                entity.HasIndex(e => e.PlanningId);
                entity.Property(e => e.Length).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Width).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Thickness).HasColumnType("decimal(18,2)");
            });

            builder.Entity<PlanningSegment>(entity =>
            {
                entity.HasIndex(e => e.PlanningId);
            });

            builder.Entity<SubExecute>(entity =>
            {
                entity.HasIndex(e => e.ExecuteId);
            });
        }

        public override int SaveChanges(bool acceptAllChangesOnSuccess = true)
        {
            OnBeforeSaving();
            return base.SaveChanges(acceptAllChangesOnSuccess);
        }

        public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess = true, CancellationToken cancellationToken = default)
        {
            OnBeforeSaving();
            return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }

        private Guid? GetCurrentUserId()
        {
            var httpContext = _httpContextAccessor?.HttpContext;
            if (httpContext?.User?.Identity?.IsAuthenticated == true)
            {
                var claim = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
                if (claim != null && Guid.TryParse(claim.Value, out var parsedGuid))
                {
                    return parsedGuid;
                }
            }
            return null;
        }

        private void OnBeforeSaving()
        {
            var entries = ChangeTracker.Entries();
            var currentTime = TimeHelper.Now;
            var currentUserId = GetCurrentUserId();

            foreach (var entry in entries)
            {
                if (entry.Entity is Auditable auditable)
                {
                    switch (entry.State)
                    {
                        case EntityState.Added:
                            auditable.CreatedDate = auditable.CreatedDate == default ? currentTime : auditable.CreatedDate;
                            if (!auditable.CreatedBy.HasValue || auditable.CreatedBy == Guid.Empty)
                            {
                                auditable.CreatedBy = currentUserId;
                            }
                            break;

                        case EntityState.Modified:
                            auditable.UpdatedDate = currentTime;
                            auditable.UpdatedBy = currentUserId;

                            if (entry.Property("IsDeleted").IsModified && auditable.IsDeleted)
                            {
                                auditable.DeletedDate = currentTime;
                                auditable.DeletedBy = currentUserId;
                            }
                            break;
                    }
                }
            }

            CaptureAuditLogs(currentUserId, currentTime);
        }

        private void CaptureAuditLogs(Guid? currentUserId, DateTime currentTime)
        {
            var auditLogs = new List<AuditLog>();
            var skipProperties = new HashSet<string>
            {
                "CreatedDate", "CreatedBy", "UpdatedDate", "UpdatedBy",
                "DeletedDate", "DeletedBy", "IsDeleted"
            };

            foreach (var entry in ChangeTracker.Entries())
            {
                if (entry.Entity is AuditLog) continue;
                if (!(entry.Entity is Auditable)) continue;

                var entityName = entry.Entity.GetType().Name;
                var keyPropName = entry.Metadata.FindPrimaryKey()?.Properties.Select(p => p.Name).FirstOrDefault() ?? "Id";
                var entityId = entry.Property(keyPropName).CurrentValue?.ToString() ?? "unknown";

                if (entry.State == EntityState.Added)
                {
                    auditLogs.Add(new AuditLog
                    {
                        Id = Guid.NewGuid(),
                        EntityName = entityName,
                        EntityId = entityId,
                        Action = "Created",
                        ChangedBy = currentUserId,
                        ChangedAt = currentTime
                    });
                }
                else if (entry.State == EntityState.Modified)
                {
                    var isDeletedProp = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "IsDeleted");
                    var isDeletedChanged = isDeletedProp != null && isDeletedProp.IsModified && (bool)isDeletedProp.CurrentValue;

                    if (isDeletedChanged)
                    {
                        auditLogs.Add(new AuditLog
                        {
                            Id = Guid.NewGuid(),
                            EntityName = entityName,
                            EntityId = entityId,
                            Action = "Deleted",
                            ChangedBy = currentUserId,
                            ChangedAt = currentTime
                        });
                    }
                    else
                    {
                        foreach (var property in entry.Properties)
                        {
                            if (property.IsModified && !skipProperties.Contains(property.Metadata.Name))
                            {
                                var rawOldValue = property.OriginalValue;
                                var rawNewValue = property.CurrentValue;
                                if (!object.Equals(rawOldValue, rawNewValue))
                                {
                                    auditLogs.Add(new AuditLog
                                    {
                                        Id = Guid.NewGuid(),
                                        EntityName = entityName,
                                        EntityId = entityId,
                                        Action = "Updated",
                                        PropertyName = property.Metadata.Name,
                                        OldValue = rawOldValue?.ToString(),
                                        NewValue = rawNewValue?.ToString(),
                                        ChangedBy = currentUserId,
                                        ChangedAt = currentTime
                                    });
                                }
                            }
                        }
                    }
                }
                else if (entry.State == EntityState.Deleted)
                {
                    auditLogs.Add(new AuditLog
                    {
                        Id = Guid.NewGuid(),
                        EntityName = entityName,
                        EntityId = entityId,
                        Action = "Deleted",
                        ChangedBy = currentUserId,
                        ChangedAt = currentTime
                    });
                }
            }

            if (auditLogs.Any())
            {
                this.Set<AuditLog>().AddRange(auditLogs);
            }
        }
    }
}
