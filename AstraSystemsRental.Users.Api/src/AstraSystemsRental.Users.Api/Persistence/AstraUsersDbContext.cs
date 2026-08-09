using AstraSystemsRental.Users.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace AstraSystemsRental.Users.Api.Persistence;

public sealed class AstraUsersDbContext(DbContextOptions<AstraUsersDbContext> options) : DbContext(options)
{
    public DbSet<Person> Persons => Set<Person>();
    public DbSet<User> Users => Set<User>();
    public DbSet<EmailConfirmation> EmailConfirmations => Set<EmailConfirmation>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<RoleNode> RoleNodes => Set<RoleNode>();
    public DbSet<Node> Nodes => Set<Node>();
    public DbSet<Plan> Plans => Set<Plan>();
    public DbSet<PlanNode> PlanNodes => Set<PlanNode>();
    public DbSet<PlanNodeQuota> PlanNodeQuotas => Set<PlanNodeQuota>();
    public DbSet<PlanNodeUsageCounter> PlanNodeUsageCounters => Set<PlanNodeUsageCounter>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<CompanyMember> CompanyMembers => Set<CompanyMember>();
    public DbSet<CompanyInvitation> CompanyInvitations => Set<CompanyInvitation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Person>(entity =>
        {
            entity.ToTable("Persons", "users");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CreatedAtUtc).HasColumnType("datetime2(3)");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users", "users");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CreatedAtUtc).HasColumnType("datetime2(3)");
            entity.HasOne(e => e.Person).WithOne(p => p.User).HasForeignKey<User>(e => e.PersonId);
            entity.HasOne(e => e.Role).WithMany().HasForeignKey(e => e.RoleId);
        });

        modelBuilder.Entity<EmailConfirmation>(entity =>
        {
            entity.ToTable("EmailConfirmations", "users");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ExpiresAtUtc).HasColumnType("datetime2(3)");
            entity.Property(e => e.ConsumedAtUtc).HasColumnType("datetime2(3)");
            entity.Property(e => e.CreatedAtUtc).HasColumnType("datetime2(3)");
            entity.HasOne(e => e.User).WithMany(u => u.EmailConfirmations).HasForeignKey(e => e.UserId);
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.ToTable("RefreshTokens", "users");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TokenHash).HasMaxLength(32).IsRequired();
            entity.Property(e => e.ReplacedByTokenHash).HasMaxLength(32);
            entity.Property(e => e.DeviceInfo).HasMaxLength(200);
            entity.Property(e => e.ExpiresAtUtc).HasColumnType("datetime2(3)");
            entity.Property(e => e.RevokedAtUtc).HasColumnType("datetime2(3)");
            entity.Property(e => e.CreatedAtUtc).HasColumnType("datetime2(3)");
            entity.HasIndex(e => e.TokenHash).IsUnique();
            entity.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId);
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.ToTable("Roles", "access");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CreatedAtUtc).HasColumnType("datetime2(3)");
        });

        modelBuilder.Entity<RoleNode>(entity =>
        {
            entity.ToTable("RoleNodes", "access");
            entity.HasKey(e => new { e.RoleId, e.NodeKey });
            entity.HasOne(e => e.Role).WithMany(r => r.RoleNodes).HasForeignKey(e => e.RoleId);
        });

        modelBuilder.Entity<Node>(entity =>
        {
            entity.ToTable("Nodes", "access");
            entity.HasKey(e => e.Key);
            entity.Property(e => e.Key).HasMaxLength(80);
            entity.Property(e => e.CreatedAtUtc).HasColumnType("datetime2(3)");
        });

        modelBuilder.Entity<Plan>(entity =>
        {
            entity.ToTable("Plans", "subscriptions");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CreatedAtUtc).HasColumnType("datetime2(3)");
        });

        modelBuilder.Entity<PlanNode>(entity =>
        {
            entity.ToTable("PlanNodes", "subscriptions");
            entity.HasKey(e => new { e.PlanId, e.NodeKey });
            entity.HasOne(e => e.Plan).WithMany(p => p.PlanNodes).HasForeignKey(e => e.PlanId);
        });

        modelBuilder.Entity<PlanNodeQuota>(entity =>
        {
            entity.ToTable("PlanNodeQuotas", "subscriptions");
            entity.HasKey(e => new { e.PlanId, e.NodeKey });
        });

        modelBuilder.Entity<PlanNodeUsageCounter>(entity =>
        {
            entity.ToTable("PlanNodeUsageCounters", "subscriptions");
            entity.HasKey(e => new { e.OwnerType, e.OwnerId, e.NodeKey });
            entity.Property(e => e.OwnerType).HasMaxLength(20);
        });

        modelBuilder.Entity<Subscription>(entity =>
        {
            entity.ToTable("Subscriptions", "subscriptions");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.OwnerType).HasMaxLength(20);
            entity.Property(e => e.StartsAtUtc).HasColumnType("datetime2(3)");
            entity.Property(e => e.EndsAtUtc).HasColumnType("datetime2(3)");
            entity.Property(e => e.CreatedAtUtc).HasColumnType("datetime2(3)");
            entity.HasIndex(e => new { e.OwnerType, e.OwnerId });
            entity.HasOne(e => e.Plan).WithMany().HasForeignKey(e => e.PlanId);
        });

        modelBuilder.Entity<Company>(entity =>
        {
            entity.ToTable("Companies", "companies");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CreatedAtUtc).HasColumnType("datetime2(3)");
        });

        modelBuilder.Entity<CompanyMember>(entity =>
        {
            entity.ToTable("CompanyMembers", "companies");
            entity.HasKey(e => new { e.CompanyId, e.UserId });
            entity.Property(e => e.JoinedAtUtc).HasColumnType("datetime2(3)");
            entity.HasOne(e => e.Company).WithMany(c => c.Members).HasForeignKey(e => e.CompanyId);
            entity.HasOne(e => e.User).WithMany(u => u.CompanyMemberships).HasForeignKey(e => e.UserId);
        });

        modelBuilder.Entity<CompanyInvitation>(entity =>
        {
            entity.ToTable("CompanyInvitations", "companies");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Email).HasMaxLength(256);
            entity.Property(e => e.TokenHash).HasMaxLength(128);
            entity.Property(e => e.Status).HasMaxLength(20);
            entity.Property(e => e.ExpiresAtUtc).HasColumnType("datetime2(3)");
            entity.Property(e => e.LastSentAtUtc).HasColumnType("datetime2(3)");
            entity.Property(e => e.CreatedAtUtc).HasColumnType("datetime2(3)");
            entity.Property(e => e.RespondedAtUtc).HasColumnType("datetime2(3)");
            entity.HasOne(e => e.Company).WithMany().HasForeignKey(e => e.CompanyId);
            entity.HasIndex(e => e.TokenHash).IsUnique();
        });
    }
}
