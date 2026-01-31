using Microsoft.EntityFrameworkCore;
using UCar.Models;
using UCar.Models.Enums;

namespace UCar.Data;

public class UCarDbContext : DbContext
{
    public UCarDbContext(DbContextOptions<UCarDbContext> options) : base(options)
    {
    }

    // User Management
    public DbSet<Role> Roles { get; set; }
    public DbSet<UserAccount> UserAccounts { get; set; }
    public DbSet<Customer> Customers { get; set; }
    public DbSet<StaffProfile> StaffProfiles { get; set; }
    public DbSet<Branch> Branches { get; set; }

    // Vehicle Management
    public DbSet<VehicleType> VehicleTypes { get; set; }
    public DbSet<VehicleModel> VehicleModels { get; set; }
    public DbSet<Vehicle> Vehicles { get; set; }
    public DbSet<Price> Prices { get; set; }
    public DbSet<VehicleStatusHistory> VehicleStatusHistories { get; set; }
    public DbSet<MaintenanceOrder> MaintenanceOrders { get; set; }

    // Pricing & Policies (Module 3.0)
    public DbSet<DepositPolicy> DepositPolicies { get; set; }
    public DbSet<SurchargePolicy> SurchargePolicies { get; set; }
    public DbSet<HolidayConfig> HolidayConfigs { get; set; }

    // Booking & Contracts
    public DbSet<Booking> Bookings { get; set; }
    public DbSet<RentalContract> RentalContracts { get; set; }
    public DbSet<HandoverRecord> HandoverRecords { get; set; }
    public DbSet<ReturnRecord> ReturnRecords { get; set; }
    public DbSet<ContractViolation> ContractViolations { get; set; }
    public DbSet<ContractCharge> ContractCharges { get; set; }
    public DbSet<CollateralItem> CollateralItems { get; set; }
    public DbSet<HandoverAccessory> HandoverAccessories { get; set; }


    // Payments & Incidents
    public DbSet<PaymentTransaction> PaymentTransactions { get; set; }
    public DbSet<Incident> Incidents { get; set; }
    public DbSet<IncidentFineDetail> IncidentFineDetails { get; set; }
    public DbSet<IncidentImpoundDetail> IncidentImpoundDetails { get; set; }
    public DbSet<IncidentCost> IncidentCosts { get; set; }
    public DbSet<IncidentDocument> IncidentDocuments { get; set; }

    // Supporting
    public DbSet<CustomerDocument> CustomerDocuments { get; set; }

    // Operations & HR (Module 8.0)
    public DbSet<OperationalTask> OperationalTasks { get; set; } = null!;
    public DbSet<Shift> Shifts { get; set; } = null!;
    public DbSet<ShiftAssignment> ShiftAssignments { get; set; } = null!;


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure enum conversions to strings
        ConfigureEnumConversions(modelBuilder);

        // Configure unique constraints
        ConfigureUniqueConstraints(modelBuilder);

        // Configure relationships
        ConfigureUserManagement(modelBuilder);
        ConfigureVehicleManagement(modelBuilder);
        ConfigureBookingAndContracts(modelBuilder);
        ConfigurePaymentsAndIncidents(modelBuilder);

        // Configure default values
        ConfigureDefaults(modelBuilder);

        // Seed data
        HolidayDataSeeder.SeedHolidays(modelBuilder);
    }

    private void ConfigureEnumConversions(ModelBuilder modelBuilder)
    {
        // Role
        modelBuilder.Entity<Role>()
            .Property(r => r.Code)
            .HasConversion<string>();

        // Vehicle
        modelBuilder.Entity<Vehicle>()
            .Property(v => v.CurrentStatus)
            .HasConversion<string>();

        // VehicleModel
        modelBuilder.Entity<VehicleModel>()
            .Property(vm => vm.Transmission)
            .HasConversion<string>();

        // Booking
        modelBuilder.Entity<Booking>()
            .Property(b => b.Status)
            .HasConversion<string>();

        // RentalContract
        modelBuilder.Entity<RentalContract>()
            .Property(rc => rc.Status)
            .HasConversion<string>();

        // ContractViolation
        modelBuilder.Entity<ContractViolation>()
            .Property(cv => cv.ViolationType)
            .HasConversion<string>();
        modelBuilder.Entity<ContractViolation>()
            .Property(cv => cv.Status)
            .HasConversion<string>();

        // CollateralItem
        modelBuilder.Entity<CollateralItem>()
            .Property(ci => ci.ItemType)
            .HasConversion<string>();
        modelBuilder.Entity<CollateralItem>()
            .Property(ci => ci.Status)
            .HasConversion<string>();

        // ContractCharge
        modelBuilder.Entity<ContractCharge>()
            .Property(cc => cc.ChargeType)
            .HasConversion<string>();

        // PaymentTransaction
        modelBuilder.Entity<PaymentTransaction>()
            .Property(pt => pt.TxnType)
            .HasConversion<string>();
        modelBuilder.Entity<PaymentTransaction>()
            .Property(pt => pt.PaymentMethod)
            .HasConversion<string>();
        modelBuilder.Entity<PaymentTransaction>()
            .Property(pt => pt.Status)
            .HasConversion<string>();
        modelBuilder.Entity<PaymentTransaction>()
            .Property(pt => pt.RefType)
            .HasConversion<string>();

        // Incident
        modelBuilder.Entity<Incident>()
            .Property(i => i.IncidentType)
            .HasConversion<string>();
        modelBuilder.Entity<Incident>()
            .Property(i => i.Status)
            .HasConversion<string>();

        // IncidentCost
        modelBuilder.Entity<IncidentCost>()
            .Property(ic => ic.CostType)
            .HasConversion<string>();

        // IncidentDocument
        modelBuilder.Entity<IncidentDocument>()
            .Property(id => id.DocType)
            .HasConversion<string>();

        // CustomerDocument
        modelBuilder.Entity<CustomerDocument>()
            .Property(cd => cd.DocType)
            .HasConversion<string>();

        // MaintenanceOrder
        modelBuilder.Entity<MaintenanceOrder>()
            .Property(mo => mo.Status)
            .HasConversion<string>();

        // OperationalTask (Module 8.0)
        modelBuilder.Entity<OperationalTask>()
            .Property(ot => ot.TaskType)
            .HasConversion<string>();
        modelBuilder.Entity<OperationalTask>()
            .Property(ot => ot.Status)
            .HasConversion<string>();

        // DepositPolicy (Module 3.0)
        modelBuilder.Entity<DepositPolicy>()
            .Property(dp => dp.CalculationType)
            .HasConversion<string>();

        // SurchargePolicy (Module 3.0)
        modelBuilder.Entity<SurchargePolicy>()
            .Property(sp => sp.Type)
            .HasConversion<string>();
        modelBuilder.Entity<SurchargePolicy>()
            .Property(sp => sp.CalculationType)
            .HasConversion<string>();
    }

    private void ConfigureUniqueConstraints(ModelBuilder modelBuilder)
    {
        // Role - Code is unique
        modelBuilder.Entity<Role>()
            .HasIndex(r => r.Code)
            .IsUnique();

        // UserAccount - Username is unique
        modelBuilder.Entity<UserAccount>()
            .HasIndex(u => u.Username)
            .IsUnique();

        // Customer - UserId is unique (one-to-one)
        modelBuilder.Entity<Customer>()
            .HasIndex(c => c.UserId)
            .IsUnique();

        // StaffProfile - UserId is unique (one-to-one)
        modelBuilder.Entity<StaffProfile>()
            .HasIndex(s => s.UserId)
            .IsUnique();

        // StaffProfile - StaffCode is unique
        modelBuilder.Entity<StaffProfile>()
            .HasIndex(s => s.StaffCode)
            .IsUnique();

        // Vehicle - PlateNo is unique
        modelBuilder.Entity<Vehicle>()
            .HasIndex(v => v.PlateNo)
            .IsUnique();

        // RentalContract - BookingId is unique (one-to-one)
        modelBuilder.Entity<RentalContract>()
            .HasIndex(rc => rc.BookingId)
            .IsUnique();

        // HandoverRecord - ContractId is unique (one-to-one)
        modelBuilder.Entity<HandoverRecord>()
            .HasIndex(hr => hr.ContractId)
            .IsUnique();

        // ReturnRecord - ContractId is unique (one-to-one)
        modelBuilder.Entity<ReturnRecord>()
            .HasIndex(rr => rr.ContractId)
            .IsUnique();
    }

    private void ConfigureUserManagement(ModelBuilder modelBuilder)
    {
        // UserAccount -> Role
        modelBuilder.Entity<UserAccount>()
            .HasOne(u => u.Role)
            .WithMany(r => r.UserAccounts)
            .HasForeignKey(u => u.RoleId)
            .OnDelete(DeleteBehavior.Restrict);

        // Customer -> UserAccount (one-to-one)
        modelBuilder.Entity<Customer>()
            .HasOne(c => c.UserAccount)
            .WithOne(u => u.Customer)
            .HasForeignKey<Customer>(c => c.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // StaffProfile -> UserAccount (one-to-one)
        modelBuilder.Entity<StaffProfile>()
            .HasOne(s => s.UserAccount)
            .WithOne(u => u.StaffProfile)
            .HasForeignKey<StaffProfile>(s => s.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // StaffProfile -> Branch
        modelBuilder.Entity<StaffProfile>()
            .HasOne(s => s.Branch)
            .WithMany(b => b.StaffProfiles)
            .HasForeignKey(s => s.BranchId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    private void ConfigureVehicleManagement(ModelBuilder modelBuilder)
    {
        // Vehicle -> Branch
        modelBuilder.Entity<Vehicle>()
            .HasOne(v => v.Branch)
            .WithMany(b => b.Vehicles)
            .HasForeignKey(v => v.BranchId)
            .OnDelete(DeleteBehavior.Restrict);

        // Vehicle -> VehicleModel
        modelBuilder.Entity<Vehicle>()
            .HasOne(v => v.Model)
            .WithMany(vm => vm.Vehicles)
            .HasForeignKey(v => v.ModelId)
            .OnDelete(DeleteBehavior.Restrict);

        // VehicleModel -> VehicleType
        modelBuilder.Entity<VehicleModel>()
            .HasOne(vm => vm.VehicleType)
            .WithMany(vt => vt.VehicleModels)
            .HasForeignKey(vm => vm.VehicleTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        // Price -> VehicleType
        modelBuilder.Entity<Price>()
            .HasOne(p => p.VehicleType)
            .WithMany(vt => vt.Prices)
            .HasForeignKey(p => p.VehicleTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        // VehicleStatusHistory -> Vehicle
        modelBuilder.Entity<VehicleStatusHistory>()
            .HasOne(vsh => vsh.Vehicle)
            .WithMany(v => v.StatusHistories)
            .HasForeignKey(vsh => vsh.VehicleId)
            .OnDelete(DeleteBehavior.Cascade);

        // MaintenanceOrder -> Vehicle
        modelBuilder.Entity<MaintenanceOrder>()
            .HasOne(mo => mo.Vehicle)
            .WithMany(v => v.MaintenanceOrders)
            .HasForeignKey(mo => mo.VehicleId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    private void ConfigureBookingAndContracts(ModelBuilder modelBuilder)
    {
        // Booking -> Customer
        modelBuilder.Entity<Booking>()
            .HasOne(b => b.Customer)
            .WithMany(c => c.Bookings)
            .HasForeignKey(b => b.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        // Booking -> VehicleType
        modelBuilder.Entity<Booking>()
            .HasOne(b => b.VehicleType)
            .WithMany(vt => vt.Bookings)
            .HasForeignKey(b => b.VehicleTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        // Booking -> Vehicle (assigned)
        modelBuilder.Entity<Booking>()
            .HasOne(b => b.AssignedVehicle)
            .WithMany(v => v.Bookings)
            .HasForeignKey(b => b.AssignedVehicleId)
            .OnDelete(DeleteBehavior.Restrict);

        // Booking -> UserAccount (created by)
        modelBuilder.Entity<Booking>()
            .HasOne(b => b.CreatedByUser)
            .WithMany(u => u.CreatedBookings)
            .HasForeignKey(b => b.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict);

        // RentalContract -> Booking (one-to-one)
        modelBuilder.Entity<RentalContract>()
            .HasOne(rc => rc.Booking)
            .WithOne(b => b.RentalContract)
            .HasForeignKey<RentalContract>(rc => rc.BookingId)
            .OnDelete(DeleteBehavior.Restrict);

        // RentalContract -> Customer
        modelBuilder.Entity<RentalContract>()
            .HasOne(rc => rc.Customer)
            .WithMany(c => c.RentalContracts)
            .HasForeignKey(rc => rc.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        // RentalContract -> Vehicle
        modelBuilder.Entity<RentalContract>()
            .HasOne(rc => rc.Vehicle)
            .WithMany(v => v.RentalContracts)
            .HasForeignKey(rc => rc.VehicleId)
            .OnDelete(DeleteBehavior.Restrict);

        // RentalContract -> Price
        modelBuilder.Entity<RentalContract>()
            .HasOne(rc => rc.Price)
            .WithMany(p => p.RentalContracts)
            .HasForeignKey(rc => rc.PriceId)
            .OnDelete(DeleteBehavior.Restrict);

        // RentalContract -> UserAccount (handled by)
        modelBuilder.Entity<RentalContract>()
            .HasOne(rc => rc.Handler)
            .WithMany(u => u.HandledContracts)
            .HasForeignKey(rc => rc.HandledBy)
            .OnDelete(DeleteBehavior.Restrict);

        // RentalContract -> UserAccount (confirmed by)
        modelBuilder.Entity<RentalContract>()
            .HasOne(rc => rc.Confirmer)
            .WithMany()
            .HasForeignKey(rc => rc.ConfirmedBy)
            .OnDelete(DeleteBehavior.Restrict);

        // RentalContract -> UserAccount (cancelled by)
        modelBuilder.Entity<RentalContract>()
            .HasOne(rc => rc.Canceller)
            .WithMany()
            .HasForeignKey(rc => rc.CancelledBy)
            .OnDelete(DeleteBehavior.Restrict);

        // HandoverRecord -> RentalContract (one-to-one)
        modelBuilder.Entity<HandoverRecord>()
            .HasOne(hr => hr.RentalContract)
            .WithOne(rc => rc.HandoverRecord)
            .HasForeignKey<HandoverRecord>(hr => hr.ContractId)
            .OnDelete(DeleteBehavior.Restrict);

        // ReturnRecord -> RentalContract (one-to-one)
        modelBuilder.Entity<ReturnRecord>()
            .HasOne(rr => rr.RentalContract)
            .WithOne(rc => rc.ReturnRecord)
            .HasForeignKey<ReturnRecord>(rr => rr.ContractId)
            .OnDelete(DeleteBehavior.Restrict);

        // ContractViolation -> RentalContract
        modelBuilder.Entity<ContractViolation>()
            .HasOne(cv => cv.RentalContract)
            .WithMany(rc => rc.Violations)
            .HasForeignKey(cv => cv.ContractId)
            .OnDelete(DeleteBehavior.Restrict);

        // ContractCharge -> RentalContract
        modelBuilder.Entity<ContractCharge>()
            .HasOne(cc => cc.RentalContract)
            .WithMany(rc => rc.Charges)
            .HasForeignKey(cc => cc.ContractId)
            .OnDelete(DeleteBehavior.Restrict);

        // ContractCharge -> ContractViolation
        modelBuilder.Entity<ContractCharge>()
            .HasOne(cc => cc.Violation)
            .WithMany(cv => cv.ContractCharges)
            .HasForeignKey(cc => cc.ViolationId)
            .OnDelete(DeleteBehavior.Restrict);

        // CollateralItem -> RentalContract
        modelBuilder.Entity<CollateralItem>()
            .HasOne(ci => ci.RentalContract)
            .WithMany(rc => rc.CollateralItems)
            .HasForeignKey(ci => ci.ContractId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    private void ConfigurePaymentsAndIncidents(ModelBuilder modelBuilder)
    {
        // PaymentTransaction -> RentalContract
        modelBuilder.Entity<PaymentTransaction>()
            .HasOne(pt => pt.RentalContract)
            .WithMany(rc => rc.PaymentTransactions)
            .HasForeignKey(pt => pt.ContractId)
            .OnDelete(DeleteBehavior.Restrict);

        // PaymentTransaction -> Customer
        modelBuilder.Entity<PaymentTransaction>()
            .HasOne(pt => pt.Customer)
            .WithMany(c => c.PaymentTransactions)
            .HasForeignKey(pt => pt.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        // Incident -> Vehicle
        modelBuilder.Entity<Incident>()
            .HasOne(i => i.Vehicle)
            .WithMany(v => v.Incidents)
            .HasForeignKey(i => i.VehicleId)
            .OnDelete(DeleteBehavior.Restrict);

        // Incident -> RentalContract
        modelBuilder.Entity<Incident>()
            .HasOne(i => i.RentalContract)
            .WithMany(rc => rc.Incidents)
            .HasForeignKey(i => i.ContractId)
            .OnDelete(DeleteBehavior.Restrict);

        // Incident -> Customer
        modelBuilder.Entity<Incident>()
            .HasOne(i => i.Customer)
            .WithMany(c => c.Incidents)
            .HasForeignKey(i => i.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        // IncidentFineDetail -> Incident (one-to-one)
        modelBuilder.Entity<IncidentFineDetail>()
            .HasOne(ifd => ifd.Incident)
            .WithOne(i => i.FineDetail)
            .HasForeignKey<IncidentFineDetail>(ifd => ifd.IncidentId)
            .OnDelete(DeleteBehavior.Cascade);

        // IncidentImpoundDetail -> Incident (one-to-one)
        modelBuilder.Entity<IncidentImpoundDetail>()
            .HasOne(iid => iid.Incident)
            .WithOne(i => i.ImpoundDetail)
            .HasForeignKey<IncidentImpoundDetail>(iid => iid.IncidentId)
            .OnDelete(DeleteBehavior.Cascade);

        // IncidentCost -> Incident
        modelBuilder.Entity<IncidentCost>()
            .HasOne(ic => ic.Incident)
            .WithMany(i => i.Costs)
            .HasForeignKey(ic => ic.IncidentId)
            .OnDelete(DeleteBehavior.Cascade);

        // IncidentDocument -> Incident
        modelBuilder.Entity<IncidentDocument>()
            .HasOne(id => id.Incident)
            .WithMany(i => i.Documents)
            .HasForeignKey(id => id.IncidentId)
            .OnDelete(DeleteBehavior.Cascade);

        // CustomerDocument -> Customer
        modelBuilder.Entity<CustomerDocument>()
            .HasOne(cd => cd.Customer)
            .WithMany(c => c.Documents)
            .HasForeignKey(cd => cd.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    private void ConfigureDefaults(ModelBuilder modelBuilder)
    {
        // Set default values for CreatedAt fields
        modelBuilder.Entity<UserAccount>()
            .Property(u => u.CreatedAt)
            .HasDefaultValueSql("GETUTCDATE()");

        modelBuilder.Entity<Customer>()
            .Property(c => c.CreatedAt)
            .HasDefaultValueSql("GETUTCDATE()");

        modelBuilder.Entity<Booking>()
            .Property(b => b.CreatedAt)
            .HasDefaultValueSql("GETUTCDATE()");

        modelBuilder.Entity<RentalContract>()
            .Property(rc => rc.CreatedAt)
            .HasDefaultValueSql("GETUTCDATE()");

        modelBuilder.Entity<ContractCharge>()
            .Property(cc => cc.CreatedAt)
            .HasDefaultValueSql("GETUTCDATE()");

        modelBuilder.Entity<VehicleStatusHistory>()
            .Property(vsh => vsh.ChangedAt)
            .HasDefaultValueSql("GETUTCDATE()");

        // Set default values for boolean fields
        modelBuilder.Entity<Role>()
            .Property(r => r.IsActive)
            .HasDefaultValue(true);

        modelBuilder.Entity<UserAccount>()
            .Property(u => u.IsActive)
            .HasDefaultValue(true);

        modelBuilder.Entity<Customer>()
            .Property(c => c.IsBlacklisted)
            .HasDefaultValue(false);

        modelBuilder.Entity<StaffProfile>()
            .Property(s => s.IsActive)
            .HasDefaultValue(true);

        modelBuilder.Entity<Price>()
            .Property(p => p.IsActive)
            .HasDefaultValue(true);

        modelBuilder.Entity<ContractCharge>()
            .Property(cc => cc.IsPaid)
            .HasDefaultValue(false);

        modelBuilder.Entity<CustomerDocument>()
            .Property(cd => cd.IsVerified)
            .HasDefaultValue(false);
    }
}
