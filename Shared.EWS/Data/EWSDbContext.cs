using Microsoft.EntityFrameworkCore;
using Shared.EWS.Entities;
using Shared.EWS.Enums;

namespace Shared.EWS.Data
{
    public class EWSDbContext : DbContext
    {
        public EWSDbContext(DbContextOptions<EWSDbContext> options) 
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<UserToken> UserTokens { get; set; }
        public DbSet<Projects> Projects { get; set; }
        public DbSet<Tasks> Tasks { get; set; }
        public DbSet<TaskComment> TaskComments { get; set; }
        public DbSet<TaskAttachment> TaskAttachments { get; set; }
        public DbSet<Attendance> Attendances { get; set; }
        public DbSet<LeaveApplication> LeaveApplications { get; set; }
        public DbSet<PublicHoliday> PublicHolidays { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            foreach (var entityType in modelBuilder.Model.GetEntityTypes()
                .Where(e => typeof(BaseEntity).IsAssignableFrom(e.ClrType)))
            {
                modelBuilder.Entity(entityType.ClrType, b =>
                {
                    b.Property("CreatedAt").HasColumnName("created_at");
                    b.Property("UpdatedAt").HasColumnName("updated_at");
                    b.Property("IsDeleted").HasColumnName("is_deleted");
                });
            }

            modelBuilder.Entity<User>(b =>
            {
                b.ToTable("users");
                b.HasKey(x => x.Id);
                b.Property(x => x.Id).HasColumnName("user_id");
                b.Property(x => x.Name).HasColumnName("user_name").HasMaxLength(100).IsRequired();
                b.Property(x => x.Email).HasColumnName("user_email").IsRequired();
                b.HasIndex(x => x.Email).IsUnique();
                b.Property(x => x.PasswordHash).HasColumnName("password_hash").IsRequired();
                b.Property(x => x.MobileNumber).HasColumnName("mobile_number").IsRequired();
                b.Property(x => x.status).HasColumnName("status");
                b.Property(x => x.PasswordResetToken).HasColumnName("password_reset_token");
                b.Property(x => x.PasswordResetTokenExpiry).HasColumnName("password_reset_token_expiry");
                b.Property(x => x.TeamLeadId).HasColumnName("team_lead_id");
                b.HasOne<Role>().WithMany().HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Cascade);
                b.HasIndex(x => x.RoleId);
            });

            modelBuilder.Entity<Role>(b =>
            {
                b.ToTable("roles");
                b.HasKey(x => x.Id);
                b.Property(x => x.Id).HasColumnName("role_id");
                b.Property(x => x.Name).HasColumnName("role_name").IsRequired();
                b.HasData(
                    new Role { Id = 1, Name = "Admin" },
                    new Role { Id = 2, Name = "Team Lead" },
                    new Role { Id = 3, Name = "Employee" }
                );
            });

            modelBuilder.Entity<UserToken>(b =>
            {
                b.ToTable("user_tokens");
                b.HasKey(x => x.Id);
                b.Property(x => x.Id).HasColumnName("id");
                b.Property(x => x.UserId).HasColumnName("user_id");
                b.Property(x => x.AccessToken).HasColumnName("access_token").IsRequired();
                b.Property(x => x.RefreshToken).HasColumnName("refresh_token").IsRequired();
                b.Property(x => x.AccessTokenExpiresAt).HasColumnName("access_token_expires_at");
                b.Property(x => x.RefreshTokenExpiresAt).HasColumnName("refresh_token_expires_at");
                b.Property(x => x.IsRevoked).HasColumnName("is_revoked");
                b.HasIndex(x => x.AccessToken);
                b.HasIndex(x => x.RefreshToken);
                b.HasOne(x => x.User)
                 .WithMany(u => u.Tokens)
                 .HasForeignKey(x => x.UserId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Projects>(b =>
            {
                b.ToTable("projects");
                b.HasKey(x => x.Id);
                b.Property(x => x.Id).HasColumnName("project_id");
                b.Property(x => x.Name).HasColumnName("project_name").IsRequired();
                b.Property(x => x.Description).HasColumnName("project_description").IsRequired().HasMaxLength(500);
                b.Property(x => x.UserId).HasColumnName("team_leader_id").IsRequired();
                b.Property(x => x.ProjectStatus).HasColumnName("project_status").HasConversion<string>().HasDefaultValue(ProjectStatus.Active);
                b.Property(x => x.StartDate).HasColumnName("start_date").IsRequired();
                b.Property(x => x.EndDate).HasColumnName("end_date").IsRequired();
                b.Property(x => x.CreatedBy).HasColumnName("created_by");
                b.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
                b.HasIndex(x => x.UserId);
                b.HasIndex(x => x.Name);
            });

            modelBuilder.Entity<Tasks>(b =>
            {
                b.ToTable("tasks");
                b.HasKey(x => x.Id);
                b.Property(x => x.Id).HasColumnName("task_id").UseIdentityColumn();
                b.Property(x => x.Title).HasColumnName("task_title").HasMaxLength(200).IsRequired();
                b.Property(x => x.Description).HasColumnName("task_description").HasMaxLength(1000).IsRequired();
                b.Property(x => x.ProjectId).HasColumnName("project_id").IsRequired();
                b.Property(x => x.AssignedToUserId).HasColumnName("assigned_to_user_id").IsRequired();
                b.Property(x => x.AssignedByUserId).HasColumnName("assigned_by_user_id").IsRequired();
                b.Property(x => x.TaskStatus).HasColumnName("task_status").HasConversion<string>().HasDefaultValue(TaskStatuses.Pending);
                b.Property(x => x.Priority).HasColumnName("priority").HasConversion<string>().HasDefaultValue(TaskPriority.Medium);
                b.Property(x => x.DueDate).HasColumnName("due_date").IsRequired();
                b.HasOne(x => x.Project)
                 .WithMany()
                 .HasForeignKey(x => x.ProjectId)
                 .OnDelete(DeleteBehavior.Cascade);
                b.HasOne(x => x.AssignedTo)
                 .WithMany()
                 .HasForeignKey(x => x.AssignedToUserId)
                 .OnDelete(DeleteBehavior.Restrict);
                b.HasOne(x => x.AssignedBy)
                 .WithMany()
                 .HasForeignKey(x => x.AssignedByUserId)
                 .OnDelete(DeleteBehavior.Restrict);
                b.HasIndex(x => x.ProjectId);
                b.HasIndex(x => x.AssignedToUserId);
            });

            modelBuilder.Entity<TaskComment>(b =>
            {
                b.ToTable("task_comments");
                b.HasKey(x => x.Id);
                b.Property(x => x.Id).HasColumnName("task_comment_id").UseIdentityColumn();
                b.Property(x => x.TaskId).HasColumnName("task_id").IsRequired();
                b.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
                b.Property(x => x.Comment).HasColumnName("comment").HasMaxLength(2000).IsRequired();
                b.HasOne(x => x.Task)
                 .WithMany(t => t.Comments)
                 .HasForeignKey(x => x.TaskId)
                 .OnDelete(DeleteBehavior.Cascade);
                b.HasOne(x => x.User)
                 .WithMany()
                 .HasForeignKey(x => x.UserId)
                 .OnDelete(DeleteBehavior.Restrict);
                b.HasIndex(x => x.TaskId);
            });

            modelBuilder.Entity<TaskAttachment>(b =>
            {
                b.ToTable("task_attachments");
                b.HasKey(x => x.Id);
                b.Property(x => x.Id).HasColumnName("task_attachment_id").UseIdentityColumn();
                b.Property(x => x.TaskId).HasColumnName("task_id").IsRequired();
                b.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
                b.Property(x => x.FileName).HasColumnName("file_name").HasMaxLength(255).IsRequired();
                b.Property(x => x.FileUrl).HasColumnName("file_url").HasMaxLength(500).IsRequired();
                b.Property(x => x.FileSize).HasColumnName("file_size").IsRequired();
                b.HasOne(x => x.Task)
                 .WithMany(t => t.Attachments)
                 .HasForeignKey(x => x.TaskId)
                 .OnDelete(DeleteBehavior.Cascade);
                b.HasOne(x => x.User)
                 .WithMany()
                 .HasForeignKey(x => x.UserId)
                 .OnDelete(DeleteBehavior.Restrict);
                b.HasIndex(x => x.TaskId);
            });

            modelBuilder.Entity<Attendance>(b =>
            {
                b.ToTable("attendances");
                b.HasKey(x => x.Id);
                b.Property(x => x.Id).HasColumnName("attendance_id").UseIdentityColumn();
                b.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
                b.Property(x => x.AttendanceDate).HasColumnName("attendance_date").IsRequired();
                b.Property(x => x.Status).HasColumnName("status").HasConversion<string>().IsRequired();
                b.Property(x => x.ApprovalStatus).HasColumnName("approval_status").HasConversion<string>().HasDefaultValue(ApprovalStatus.Pending);
                b.Property(x => x.ReviewerId).HasColumnName("reviewer_id");
                b.Property(x => x.ReviewerRemark).HasColumnName("reviewer_remark").HasMaxLength(500);
                b.Property(x => x.ReviewedAt).HasColumnName("reviewed_at");
                b.HasOne(x => x.User)
                 .WithMany()
                 .HasForeignKey(x => x.UserId)
                 .OnDelete(DeleteBehavior.Restrict);
                b.HasOne(x => x.Reviewer)
                 .WithMany()
                 .HasForeignKey(x => x.ReviewerId)
                 .OnDelete(DeleteBehavior.Restrict);
                b.HasIndex(x => new { x.UserId, x.AttendanceDate })
                 .IsUnique()
                 .HasFilter("is_deleted = false");
                b.HasIndex(x => x.UserId);
                b.HasIndex(x => x.AttendanceDate);
            });

            modelBuilder.Entity<LeaveApplication>(b =>
            {
                b.ToTable("leave_applications");
                b.HasKey(x => x.Id);
                b.Property(x => x.Id).HasColumnName("leave_application_id").UseIdentityColumn();
                b.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
                b.Property(x => x.ReviewerId).HasColumnName("reviewer_id");
                b.Property(x => x.LeaveType).HasColumnName("leave_type").HasConversion<string>().IsRequired();
                b.Property(x => x.StartDate).HasColumnName("start_date").IsRequired();
                b.Property(x => x.EndDate).HasColumnName("end_date").IsRequired();
                b.Property(x => x.Reason).HasColumnName("reason").HasMaxLength(1000).IsRequired();
                b.Property(x => x.LeaveStatus).HasColumnName("leave_status").HasConversion<string>().HasDefaultValue(ApprovalStatus.Pending);
                b.Property(x => x.ReviewerRemark).HasColumnName("reviewer_remark").HasMaxLength(500);
                b.Property(x => x.ReviewedAt).HasColumnName("reviewed_at");
                b.HasOne(x => x.User)
                 .WithMany()
                 .HasForeignKey(x => x.UserId)
                 .OnDelete(DeleteBehavior.Restrict);
                b.HasOne(x => x.Reviewer)
                 .WithMany()
                 .HasForeignKey(x => x.ReviewerId)
                 .OnDelete(DeleteBehavior.Restrict);
                b.HasIndex(x => x.UserId);
                b.HasIndex(x => x.StartDate);
            });

            modelBuilder.Entity<PublicHoliday>(b =>
            {
                b.ToTable("public_holidays");
                b.HasKey(x => x.Id);
                b.Property(x => x.Id).HasColumnName("holiday_id").UseIdentityColumn();
                b.Property(x => x.HolidayDate).HasColumnName("holiday_date").IsRequired();
                b.Property(x => x.Name).HasColumnName("holiday_name").HasMaxLength(200).IsRequired();
                b.HasIndex(x => x.HolidayDate).IsUnique().HasFilter("is_deleted = false");
            });
        }
    }
}