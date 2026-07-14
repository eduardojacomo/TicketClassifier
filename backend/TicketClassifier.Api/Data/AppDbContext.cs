using Microsoft.EntityFrameworkCore;
using TicketClassifier.Api.Models;

namespace TicketClassifier.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<TicketBatch> Batches => Set<TicketBatch>();
    public DbSet<Ticket> Tickets => Set<Ticket>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<TicketBatch>().HasKey(x => x.Id);
        b.Entity<Ticket>().HasKey(x => x.Id);
        b.Entity<Ticket>().HasIndex(x => x.BatchId);
        b.Entity<Ticket>()
            .HasOne<TicketBatch>()
            .WithMany(x => x.Tickets)
            .HasForeignKey(x => x.BatchId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
