using AstraSystemsRental.Maintenance.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace AstraSystemsRental.Maintenance.Api.Persistence;

public sealed class AstraMaintenanceDbContext(DbContextOptions<AstraMaintenanceDbContext> options) : DbContext(options)
{
    public DbSet<MaintenanceRoutine> MaintenanceRoutines => Set<MaintenanceRoutine>();
    public DbSet<MaintenanceRoutinePeriodicity> MaintenanceRoutinePeriodicities => Set<MaintenanceRoutinePeriodicity>();
    public DbSet<MaintenanceRoutineConcept> MaintenanceRoutineConcepts => Set<MaintenanceRoutineConcept>();
    public DbSet<RoutineAssignment> RoutineAssignments => Set<RoutineAssignment>();
    public DbSet<RoutineAssignmentHistory> RoutineAssignmentHistory => Set<RoutineAssignmentHistory>();
    public DbSet<MileageReading> MileageReadings => Set<MileageReading>();
    public DbSet<WorkshopReservation> WorkshopReservations => Set<WorkshopReservation>();
    public DbSet<WorkshopProvider> WorkshopProviders => Set<WorkshopProvider>();
    public DbSet<WorkshopReservationPhoto> WorkshopReservationPhotos => Set<WorkshopReservationPhoto>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MaintenanceRoutine>(entity =>
        {
            entity.ToTable("MaintenanceRoutines", "maintenance");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.OwnerType).HasMaxLength(20);
            entity.Property(e => e.Name).HasMaxLength(150);
            entity.Property(e => e.Description).HasMaxLength(400);
            entity.Property(e => e.CreatedAtUtc).HasColumnType("datetime2(3)");
            entity.Property(e => e.UpdatedAtUtc).HasColumnType("datetime2(3)");
            entity.Property(e => e.RowVersion).IsRowVersion();
            entity.HasIndex(e => new { e.OwnerType, e.OwnerId, e.Name }).IsUnique();
        });

        modelBuilder.Entity<MaintenanceRoutinePeriodicity>(entity =>
        {
            entity.ToTable("MaintenanceRoutinePeriodicities", "maintenance");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Unit).HasConversion<byte>();
            entity.HasOne<MaintenanceRoutine>().WithMany().HasForeignKey(e => e.RoutineId).OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => e.RoutineId);
        });

        modelBuilder.Entity<MaintenanceRoutineConcept>(entity =>
        {
            entity.ToTable("MaintenanceRoutineConcepts", "maintenance");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(150);
            entity.Property(e => e.Quantity).HasColumnType("decimal(14,2)");
            entity.Property(e => e.QuantityUnit).HasConversion<byte?>();
            entity.Property(e => e.Notes).HasMaxLength(400);
            entity.HasOne<MaintenanceRoutinePeriodicity>().WithMany().HasForeignKey(e => e.PeriodicityId).OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => e.PeriodicityId);
        });

        modelBuilder.Entity<RoutineAssignment>(entity =>
        {
            entity.ToTable("RoutineAssignments", "maintenance");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.OwnerType).HasMaxLength(20);
            entity.Property(e => e.AssignedAtUtc).HasColumnType("datetime2(3)");
            entity.Property(e => e.RowVersion).IsRowVersion();
            entity.HasIndex(e => new { e.OwnerType, e.OwnerId, e.FleetVehicleId }).IsUnique();
            entity.HasOne<MaintenanceRoutine>().WithMany().HasForeignKey(e => e.RoutineId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<RoutineAssignmentHistory>(entity =>
        {
            entity.ToTable("RoutineAssignmentHistory", "maintenance");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ChangedAtUtc).HasColumnType("datetime2(3)");
            entity.HasIndex(e => new { e.FleetVehicleId, e.ChangedAtUtc });
        });

        modelBuilder.Entity<MileageReading>(entity =>
        {
            entity.ToTable("MileageReadings", "maintenance");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.OwnerType).HasMaxLength(20);
            entity.Property(e => e.ReadingType).HasConversion<byte>();
            entity.Property(e => e.Source).HasMaxLength(20);
            entity.Property(e => e.Notes).HasMaxLength(400);
            entity.Property(e => e.CreatedAtUtc).HasColumnType("datetime2(3)");
            entity.HasIndex(e => new { e.OwnerType, e.OwnerId, e.FleetVehicleId, e.ReadingDate });
        });

        modelBuilder.Entity<WorkshopProvider>(entity =>
        {
            entity.ToTable("WorkshopProviders", "maintenance");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.OwnerType).HasMaxLength(20);
            entity.Property(e => e.ProviderType).HasConversion<byte>();
            entity.Property(e => e.Name).HasMaxLength(160);
            entity.Property(e => e.DocumentNumber).HasMaxLength(40);
            entity.Property(e => e.ContactPhone).HasMaxLength(40);
            entity.Property(e => e.ContactEmail).HasMaxLength(256);
            entity.Property(e => e.Address).HasMaxLength(250);
            entity.Property(e => e.CreatedAtUtc).HasColumnType("datetime2(3)");
            entity.Property(e => e.UpdatedAtUtc).HasColumnType("datetime2(3)");
            entity.HasIndex(e => new { e.OwnerType, e.OwnerId, e.Name });
        });

        modelBuilder.Entity<WorkshopReservation>(entity =>
        {
            entity.ToTable("WorkshopReservations", "maintenance");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.OwnerType).HasMaxLength(20);
            entity.Property(e => e.Status).HasConversion<byte>();
            entity.Property(e => e.Notes).HasMaxLength(1000);
            entity.Property(e => e.ScheduledAtUtc).HasColumnType("datetime2(3)");
            entity.Property(e => e.ExpectedEndAtUtc).HasColumnType("datetime2(3)");
            entity.Property(e => e.PickedUpAtUtc).HasColumnType("datetime2(3)");
            entity.Property(e => e.ReadyAtUtc).HasColumnType("datetime2(3)");
            entity.Property(e => e.CollectedAtUtc).HasColumnType("datetime2(3)");
            entity.Property(e => e.CreatedAtUtc).HasColumnType("datetime2(3)");
            entity.Property(e => e.UpdatedAtUtc).HasColumnType("datetime2(3)");
            entity.Property(e => e.RowVersion).IsRowVersion();
            entity.HasIndex(e => new { e.OwnerType, e.OwnerId, e.FleetVehicleId, e.Status });
            entity.HasOne<WorkshopProvider>().WithMany().HasForeignKey(e => e.ProviderId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<WorkshopReservationPhoto>(entity =>
        {
            entity.ToTable("WorkshopReservationPhotos", "maintenance");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Status).HasConversion<byte>();
            entity.Property(e => e.FileName).HasMaxLength(260);
            entity.Property(e => e.StoragePath).HasMaxLength(400);
            entity.Property(e => e.ContentType).HasMaxLength(100);
            entity.Property(e => e.Notes).HasMaxLength(400);
            entity.Property(e => e.CreatedAtUtc).HasColumnType("datetime2(3)");
            entity.HasOne<WorkshopReservation>().WithMany().HasForeignKey(e => e.WorkshopReservationId).OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => e.WorkshopReservationId);
        });
    }
}
