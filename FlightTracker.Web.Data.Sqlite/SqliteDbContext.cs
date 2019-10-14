using FlightTracker.DTOs;
using FlightTracker.Web.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace FlightTracker.Web.Data
{
    public class SqliteDbContext : DbContext
    {
        public SqliteDbContext(DbContextOptions<SqliteDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<FlightData>().OwnsOne(e => e.Aircraft);

            modelBuilder.Entity<FlightData>().OwnsOne(e => e.FlightPlan, p =>
            {
                p.OwnsOne(o => o.Departure);
                p.OwnsOne(o => o.Destination);
                p.Property(p => p.Waypoints).HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, null),
                    v => System.Text.Json.JsonSerializer.Deserialize<List<Waypoint>>(v, null));
            });
            modelBuilder.Entity<FlightData>().OwnsOne(e => e.StatusTakeOff).WithOwner();
            modelBuilder.Entity<FlightData>().OwnsOne(e => e.StatusLanding).WithOwner();

            modelBuilder.Entity<FlightStatusWrapper>().HasKey(e => new { e.FlightId, e.SimTime });
        }

        public DbSet<FlightData> Flights { get; set; }
        public DbSet<FlightStatusWrapper> FlightStatuses { get; set; }
    }
}
