using Microsoft.EntityFrameworkCore;
using RailwayTickets.Models;
using Route = RailwayTickets.Models.Route;

namespace RailwayTickets.Data
{
    public class RailwayDbContext : DbContext
    {
        public RailwayDbContext(DbContextOptions<RailwayDbContext> options)
            : base(options)
        {
        }

        public DbSet<Employee> Employees { get; set; }
        public DbSet<Administrator> Administrators { get; set; }
        public DbSet<Cashier> Cashiers { get; set; }
        public DbSet<Passenger> Passengers { get; set; }
        public DbSet<Ticket> Tickets { get; set; }
        public DbSet<Trip> Trips { get; set; }
        public DbSet<Route> Routes { get; set; }
        public DbSet<Train> Trains { get; set; }
        public DbSet<Carriage> Carriages { get; set; }
        public DbSet<ReturnRequest> ReturnRequests { get; set; }

        public DbSet<CashShift> CashShifts => Set<CashShift>();
        public DbSet<CashEntry> CashEntries => Set<CashEntry>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Employee>().ToTable("Employees", table => table.UseSqlOutputClause(false));
            modelBuilder.Entity<Passenger>().ToTable("Passengers", table => table.UseSqlOutputClause(false));
            modelBuilder.Entity<Route>().ToTable("Routes", table => table.UseSqlOutputClause(false));
            modelBuilder.Entity<Train>().ToTable("Trains", table => table.UseSqlOutputClause(false));
            modelBuilder.Entity<Carriage>().ToTable("Carriages", table => table.UseSqlOutputClause(false));
            modelBuilder.Entity<Trip>().ToTable("Trips", table => table.UseSqlOutputClause(false));
            modelBuilder.Entity<Ticket>().ToTable("Tickets", table => table.UseSqlOutputClause(false));
            modelBuilder.Entity<ReturnRequest>().ToTable("ReturnRequests", table => table.UseSqlOutputClause(false));
            modelBuilder.Entity<CashShift>().ToTable("CashShifts", table => table.UseSqlOutputClause(false));
            modelBuilder.Entity<CashEntry>().ToTable("CashEntries", table => table.UseSqlOutputClause(false));

            modelBuilder.Entity<Passenger>().Property(p => p.AccountLogin).HasMaxLength(100);
            modelBuilder.Entity<Passenger>().HasIndex(p => p.AccountLogin).IsUnique().HasFilter("[AccountLogin] IS NOT NULL");
            modelBuilder.Entity<Employee>().Property(e => e.Login).HasMaxLength(100);
            modelBuilder.Entity<Employee>().HasIndex(e => e.Login).IsUnique();
            modelBuilder.Entity<Ticket>().Property(t => t.TicketNumber).HasMaxLength(100);
            modelBuilder.Entity<Ticket>().Property(t => t.Status).HasMaxLength(24);
            modelBuilder.Entity<Ticket>().HasIndex(t => t.TicketNumber).IsUnique();
            modelBuilder.Entity<Ticket>().HasIndex(t => new { t.TripId, t.CarriageNumber, t.SeatNumber }).IsUnique()
                .HasFilter(Database.IsSqlite() ? "[Status] IN ('Заброньовано', 'Оформлено', 'Оплачено')" : "[Status] IN (N'Заброньовано', N'Оформлено', N'Оплачено')");
            modelBuilder.Entity<ReturnRequest>().Property(r => r.Status).HasMaxLength(24);
            modelBuilder.Entity<ReturnRequest>().HasIndex(r => r.TicketId).IsUnique()
                .HasFilter(Database.IsSqlite() ? "[Status] IN ('Очікує', 'Схвалено')" : "[Status] IN (N'Очікує', N'Схвалено')");
            modelBuilder.Entity<Carriage>().HasIndex(c => new { c.TrainId, c.CarriageNumber }).IsUnique();
            modelBuilder.Entity<CashShift>().HasIndex(s => s.CashierId).IsUnique().HasFilter("[Closed] IS NULL");
            modelBuilder.Entity<CashEntry>().HasIndex(e => e.ReversesId).IsUnique().HasFilter("[ReversesId] IS NOT NULL");
            modelBuilder.Entity<CashEntry>().HasOne<CashShift>().WithMany(s => s.Entries).HasForeignKey(e => e.CashShiftId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<CashShift>().HasOne<Employee>().WithMany().HasForeignKey(e => e.CashierId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<CashShift>().HasOne<Employee>().WithMany().HasForeignKey(e => e.ApprovedBy).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<CashEntry>().HasOne<Ticket>().WithMany().HasForeignKey(e => e.TicketId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<CashEntry>().HasOne<Employee>().WithMany().HasForeignKey(e => e.ActorId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<CashEntry>().HasOne<CashEntry>().WithMany().HasForeignKey(e => e.ReversesId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<CashEntry>().HasOne<ReturnRequest>().WithMany().HasForeignKey(e => e.ReturnRequestId).OnDelete(DeleteBehavior.Restrict);


            // Train → Carriage
            modelBuilder.Entity<Train>()
                .HasMany(t => t.Carriages)
                .WithOne(c => c.Train)
                .HasForeignKey(c => c.TrainId)
                .OnDelete(DeleteBehavior.Cascade);

            // Route → Trip
            modelBuilder.Entity<Route>()
                .HasMany(r => r.Trips)
                .WithOne(t => t.Route)
                .HasForeignKey(t => t.RouteId)
                .OnDelete(DeleteBehavior.Restrict);

            // Train → Trip
            modelBuilder.Entity<Train>()
                .HasMany(t => t.Trips)
                .WithOne(t => t.Train)
                .HasForeignKey(t => t.TrainId)
                .OnDelete(DeleteBehavior.Restrict);

            // Passenger → Ticket
            modelBuilder.Entity<Passenger>()
                .HasMany(p => p.Tickets)
                .WithOne(t => t.Passenger)
                .HasForeignKey(t => t.PassengerId)
                .OnDelete(DeleteBehavior.Restrict);

            // Trip → Ticket
            modelBuilder.Entity<Trip>()
                .HasMany(t => t.Tickets)
                .WithOne(t => t.Trip)
                .HasForeignKey(t => t.TripId)
                .OnDelete(DeleteBehavior.Restrict);

            // Cashier → Ticket
            modelBuilder.Entity<Cashier>()
                .HasMany(c => c.Tickets)
                .WithOne(t => t.Cashier)
                .HasForeignKey(t => t.CashierId)
                .OnDelete(DeleteBehavior.SetNull);

            // Ticket → ReturnRequest
            modelBuilder.Entity<ReturnRequest>()
                .HasOne(r => r.Ticket)
                .WithMany()
                .HasForeignKey(r => r.TicketId)
                .OnDelete(DeleteBehavior.Restrict);

            // Passenger → ReturnRequest
            modelBuilder.Entity<ReturnRequest>()
                .HasOne(r => r.Passenger)
                .WithMany(p => p.ReturnRequests)
                .HasForeignKey(r => r.PassengerId)
                .OnDelete(DeleteBehavior.Restrict);

            // Cashier → ReturnRequest
            modelBuilder.Entity<ReturnRequest>()
                .HasOne(r => r.Cashier)
                .WithMany()
                .HasForeignKey(r => r.CashierId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}