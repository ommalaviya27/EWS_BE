using Microsoft.EntityFrameworkCore;
using Shared.EWS.Entities;

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
                b.HasOne<Role>().WithMany().HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Cascade);
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
        }
    }
}