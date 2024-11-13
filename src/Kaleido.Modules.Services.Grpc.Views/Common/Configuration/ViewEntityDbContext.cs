using Kaleido.Modules.Services.Grpc.Views.Common.Models;
using Microsoft.EntityFrameworkCore;
using Kaleido.Common.Services.Grpc.Configuration.Constants;
using Kaleido.Common.Services.Grpc.Configuration.Interfaces;

namespace Kaleido.Modules.Services.Grpc.Views.Common.Configuration;

public class ViewEntityDbContext : DbContext, IKaleidoDbContext<ViewEntity>
{
    public DbSet<ViewEntity> Items { get; set; }

    public ViewEntityDbContext(DbContextOptions<ViewEntityDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ViewEntity>(entity =>
        {
            entity.Property(c => c.Name).IsRequired().HasColumnType("varchar(100)");
            entity.ToTable("Views");
            DefaultOnModelCreatingMethod.ForBaseEntity(entity);
        });
    }
}
