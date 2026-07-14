using System.Reflection;
using Microsoft.EntityFrameworkCore;
using TicketClassifier.Api.Models;

namespace TicketClassifier.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<TicketBatch> Batches => Set<TicketBatch>();
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<TicketSimilaridade> Similaridades => Set<TicketSimilaridade>();
    public DbSet<ParametroClassificacao> ParametrosClassificacao => Set<ParametroClassificacao>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}
