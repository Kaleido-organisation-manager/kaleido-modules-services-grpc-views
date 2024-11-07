using Kaleido.Common.Services.Grpc.Configuration.Constants;
using Kaleido.Common.Services.Grpc.Configuration.Interfaces;
using Kaleido.Modules.Services.Grpc.Views.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace Kaleido.Modules.Services.Grpc.Views.Common.Configuration;

public class CategoryViewLinkRevisionEntityDbContext : DbContext, IKaleidoDbContext<CategoryViewLinkRevisionEntity>
{
    public DbSet<CategoryViewLinkRevisionEntity> Items { get; set; }

    public CategoryViewLinkRevisionEntityDbContext(DbContextOptions<CategoryViewLinkRevisionEntityDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CategoryViewLinkRevisionEntity>(entity =>
        {
            entity.ToTable("CategoryViewLinkRevisions");
            DefaultOnModelCreatingMethod.ForBaseEntity(entity);
            DefaultOnModelCreatingMethod.ForBaseRevisionEntity(entity);
        });
    }
}
