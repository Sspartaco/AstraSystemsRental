using AstraSystemsRental.Vehicles.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace AstraSystemsRental.Vehicles.Api.Persistence;

public sealed class AstraVehiclesDbContext(DbContextOptions<AstraVehiclesDbContext> options) : DbContext(options)
{
    public DbSet<VehicleCatalog> VehicleCatalog => Set<VehicleCatalog>();
    public DbSet<ValuationSourceCatalog> ValuationSources => Set<ValuationSourceCatalog>();
    public DbSet<ValuationCacheEntry> ValuationCache => Set<ValuationCacheEntry>();
    public DbSet<QuoteRequest> QuoteRequests => Set<QuoteRequest>();
    public DbSet<FleetVehicle> FleetVehicles => Set<FleetVehicle>();
    public DbSet<FleetVehicleStatusHistory> FleetVehicleStatusHistory => Set<FleetVehicleStatusHistory>();
    public DbSet<FleetVehicleOdometerReading> FleetVehicleOdometerReadings => Set<FleetVehicleOdometerReading>();
    public DbSet<FleetVehicleDocument> FleetVehicleDocuments => Set<FleetVehicleDocument>();
    public DbSet<PendingQuotaCompensation> PendingQuotaCompensations => Set<PendingQuotaCompensation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<VehicleCatalog>(entity =>
        {
            entity.ToTable("VehicleCatalog", "vehicles");
            entity.HasKey(e => e.PlateNumber);
            entity.Property(e => e.PlateNumber).HasMaxLength(10);
            entity.Property(e => e.VehicleClass).HasMaxLength(60);
            entity.Property(e => e.Brand).HasMaxLength(80);
            entity.Property(e => e.Line).HasMaxLength(120);
            entity.Property(e => e.FullLine).HasMaxLength(200);
            entity.Property(e => e.Engine).HasMaxLength(60);
            entity.Property(e => e.ImageUrl).HasMaxLength(500);
            entity.Property(e => e.ImageAttribution).HasMaxLength(300);
            entity.Property(e => e.ImageFetchedAtUtc).HasColumnType("datetime2(3)");
            entity.Property(e => e.CreatedAtUtc).HasColumnType("datetime2(3)");
            entity.Property(e => e.UpdatedAtUtc).HasColumnType("datetime2(3)");
        });

        modelBuilder.Entity<ValuationSourceCatalog>(entity =>
        {
            entity.ToTable("ValuationSources", "vehicles");
            entity.HasKey(e => e.Code);
            entity.Property(e => e.Code).HasMaxLength(30);
            entity.Property(e => e.DisplayName).HasMaxLength(60);
        });

        modelBuilder.Entity<ValuationCacheEntry>(entity =>
        {
            entity.ToTable("ValuationCache", "vehicles");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Status).HasConversion<byte>();
            entity.Property(e => e.PlateNumber).HasMaxLength(10);
            entity.Property(e => e.SourceCode).HasMaxLength(30);
            entity.Property(e => e.ValueMin).HasColumnType("decimal(14,2)");
            entity.Property(e => e.ValueMax).HasColumnType("decimal(14,2)");
            entity.Property(e => e.ValueAvg).HasColumnType("decimal(14,2)");
            entity.Property(e => e.Currency).HasMaxLength(3);
            entity.Property(e => e.RawPayload).HasColumnType("nvarchar(max)");
            entity.Property(e => e.ErrorMessage).HasMaxLength(400);
            entity.Property(e => e.FetchedAtUtc).HasColumnType("datetime2(3)");
            entity.Property(e => e.ExpiresAtUtc).HasColumnType("datetime2(3)");
            entity.HasIndex(e => new { e.PlateNumber, e.SourceCode }).IsUnique();
        });

        modelBuilder.Entity<QuoteRequest>(entity =>
        {
            entity.ToTable("QuoteRequests", "vehicles");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Status).HasConversion<byte>();
            entity.Property(e => e.PlateNumber).HasMaxLength(10);
            entity.Property(e => e.CreatedAtUtc).HasColumnType("datetime2(3)");
            entity.Property(e => e.CompletedAtUtc).HasColumnType("datetime2(3)");
            entity.HasIndex(e => e.RequestId).IsUnique();
        });

        modelBuilder.Entity<FleetVehicle>(entity =>
        {
            entity.ToTable("FleetVehicles", "vehicles");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.OwnerType).HasMaxLength(20);
            entity.Property(e => e.PlateNumber).HasMaxLength(10);
            entity.Property(e => e.VehicleClass).HasMaxLength(60);
            entity.Property(e => e.Brand).HasMaxLength(80);
            entity.Property(e => e.Line).HasMaxLength(120);
            entity.Property(e => e.BodyType).HasMaxLength(60);
            entity.Property(e => e.ServiceType).HasMaxLength(40);
            entity.Property(e => e.FuelType).HasMaxLength(40);
            entity.Property(e => e.Transmission).HasMaxLength(40);
            entity.Property(e => e.Color).HasMaxLength(40);
            entity.Property(e => e.Vin).HasMaxLength(32);
            entity.Property(e => e.EngineNumber).HasMaxLength(32);
            entity.Property(e => e.SerialNumber).HasMaxLength(32);
            entity.Property(e => e.ChassisNumber).HasMaxLength(32);
            entity.Property(e => e.TransitLicenseNumber).HasMaxLength(40);
            entity.Property(e => e.PurchaseInvoiceNumber).HasMaxLength(40);
            entity.Property(e => e.PurchaseValue).HasColumnType("decimal(14,2)");
            entity.Property(e => e.Status).HasConversion<byte>();
            entity.Property(e => e.Notes).HasMaxLength(1000);
            entity.Property(e => e.CreatedAtUtc).HasColumnType("datetime2(3)");
            entity.Property(e => e.UpdatedAtUtc).HasColumnType("datetime2(3)");
            entity.Property(e => e.RowVersion).IsRowVersion();
            entity.HasIndex(e => new { e.OwnerType, e.OwnerId, e.PlateNumber }).IsUnique();
        });

        modelBuilder.Entity<FleetVehicleStatusHistory>(entity =>
        {
            entity.ToTable("FleetVehicleStatusHistory", "vehicles");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PreviousStatus).HasConversion<byte?>();
            entity.Property(e => e.NewStatus).HasConversion<byte>();
            entity.Property(e => e.Reason).HasMaxLength(400);
            entity.Property(e => e.ChangedAtUtc).HasColumnType("datetime2(3)");
            entity.HasOne<FleetVehicle>().WithMany().HasForeignKey(e => e.FleetVehicleId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<FleetVehicleOdometerReading>(entity =>
        {
            entity.ToTable("FleetVehicleOdometerReadings", "vehicles");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Source).HasMaxLength(20);
            entity.Property(e => e.Notes).HasMaxLength(400);
            entity.Property(e => e.CreatedAtUtc).HasColumnType("datetime2(3)");
            entity.HasOne<FleetVehicle>().WithMany().HasForeignKey(e => e.FleetVehicleId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<FleetVehicleDocument>(entity =>
        {
            entity.ToTable("FleetVehicleDocuments", "vehicles");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.DocumentType).HasMaxLength(40);
            entity.Property(e => e.DocumentNumber).HasMaxLength(60);
            entity.Property(e => e.Status).HasConversion<byte>();
            entity.Property(e => e.Notes).HasMaxLength(400);
            entity.Property(e => e.CreatedAtUtc).HasColumnType("datetime2(3)");
            entity.Property(e => e.UpdatedAtUtc).HasColumnType("datetime2(3)");
            entity.HasOne<FleetVehicle>().WithMany().HasForeignKey(e => e.FleetVehicleId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PendingQuotaCompensation>(entity =>
        {
            entity.ToTable("PendingQuotaCompensations", "vehicles");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.NodeKey).HasMaxLength(80);
            entity.Property(e => e.OwnerType).HasMaxLength(20);
            entity.Property(e => e.Error).HasMaxLength(400);
            entity.Property(e => e.CreatedAtUtc).HasColumnType("datetime2(3)");
        });
    }
}
