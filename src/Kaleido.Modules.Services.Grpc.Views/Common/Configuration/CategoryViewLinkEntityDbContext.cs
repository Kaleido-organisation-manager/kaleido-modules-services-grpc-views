using Kaleido.Common.Services.Grpc.Configuration.Constants;
using Kaleido.Common.Services.Grpc.Configuration.Interfaces;
using Kaleido.Modules.Services.Grpc.Views.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace Kaleido.Modules.Services.Grpc.Views.Common.Configuration;

public class CategoryViewLinkEntityDbContext : DbContext, IKaleidoDbContext<CategoryViewLinkEntity>
{
    public DbSet<CategoryViewLinkEntity> Items { get; set; }

    public CategoryViewLinkEntityDbContext(DbContextOptions<CategoryViewLinkEntityDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CategoryViewLinkEntity>(entity =>
        {
            entity.Property(c => c.CategoryKey).IsRequired().HasColumnType("varchar(36)");
            entity.Property(c => c.ViewKey).IsRequired().HasColumnType("varchar(36)");
            entity.ToTable("CategoryViewLinks");
            DefaultOnModelCreatingMethod.ForBaseEntity(entity);
        });
    }
}
