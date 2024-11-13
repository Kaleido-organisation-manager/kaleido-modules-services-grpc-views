using Kaleido.Common.Services.Grpc.Configuration.Constants;
using Kaleido.Common.Services.Grpc.Configuration.Interfaces;
using Kaleido.Modules.Services.Grpc.Views.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace Kaleido.Modules.Services.Grpc.Views.Common.Configuration;

public class ViewRevisionDbContext : DbContext, IKaleidoDbContext<ViewRevisionEntity>
{
    public DbSet<ViewRevisionEntity> Items { get; set; }

    public ViewRevisionDbContext(DbContextOptions<ViewRevisionDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ViewRevisionEntity>(entity =>
        {
            entity.ToTable("ViewRevisions");
            DefaultOnModelCreatingMethod.ForBaseEntity(entity);
            DefaultOnModelCreatingMethod.ForBaseRevisionEntity(entity);
        });
    }
}
