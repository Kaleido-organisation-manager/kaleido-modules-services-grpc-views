using Kaleido.Common.Services.Grpc.Constants;
using Kaleido.Common.Services.Grpc.Exceptions;
using Kaleido.Common.Services.Grpc.Handlers.Interfaces;
using Kaleido.Common.Services.Grpc.Models;
using Kaleido.Modules.Services.Grpc.Views.Common.Models;

namespace Kaleido.Modules.Services.Grpc.Views.Update;

public class UpdateManager : IUpdateManager
{
    private readonly IEntityLifecycleHandler<ViewEntity, ViewRevisionEntity> _viewHandler;
    private readonly IEntityLifecycleHandler<CategoryViewLinkEntity, CategoryViewLinkRevisionEntity> _categoryViewLinkHandler;

    public UpdateManager(
        IEntityLifecycleHandler<ViewEntity, ViewRevisionEntity> viewHandler,
        IEntityLifecycleHandler<CategoryViewLinkEntity, CategoryViewLinkRevisionEntity> categoryViewLinkHandler)
    {
        _viewHandler = viewHandler;
        _categoryViewLinkHandler = categoryViewLinkHandler;
    }

    public async Task<(EntityLifeCycleResult<ViewEntity, ViewRevisionEntity>, IEnumerable<EntityLifeCycleResult<CategoryViewLinkEntity, CategoryViewLinkRevisionEntity>>)> UpdateAsync(Guid key, ViewEntity viewEntity, IEnumerable<string> categoryKeys, CancellationToken cancellationToken = default)
    {
        var timestamp = new DateTime(DateTime.UtcNow.Ticks - (DateTime.UtcNow.Ticks % TimeSpan.TicksPerMillisecond), DateTimeKind.Utc);

        var viewRevisionEntity = new ViewRevisionEntity
        {
            Key = key,
            CreatedAt = timestamp,
        };

        EntityLifeCycleResult<ViewEntity, ViewRevisionEntity>? viewResult;
        try
        {
            viewResult = await _viewHandler.UpdateAsync(key, viewEntity, viewRevisionEntity, cancellationToken);
        }
        catch (NotModifiedException)
        {
            viewResult = await _viewHandler.GetAsync(key, cancellationToken: cancellationToken);
        }

        if (viewResult == null)
        {
            throw new EntityNotFoundException($"Could not find view entity with key {key} to update.");
        }

        var linkedCategories = await _categoryViewLinkHandler.FindAllAsync(link => link.ViewKey == key, cancellationToken: cancellationToken);
        linkedCategories = linkedCategories.GroupBy(l => l.Key).Select(l => l.OrderByDescending(x => x.Revision.Revision).First());

        // Get latest revision of linked categories
        var latestLinkedCategories = linkedCategories.GroupBy(x => x.Key).Select(x => x.OrderByDescending(y => y.Revision.Revision).First());

        // Get Categories to delete
        var categoriesToDelete = latestLinkedCategories.Where(x => !categoryKeys.Contains(x.Entity.CategoryKey.ToString()) && x.Revision.Action != RevisionAction.Deleted).ToList();

        // Get Categories to create
        var categoriesToCreate = categoryKeys.Where(x => !latestLinkedCategories.Any(y => y.Entity.CategoryKey.ToString() == x))
            .Select(x => new CategoryViewLinkEntity { CategoryKey = Guid.Parse(x), ViewKey = key })
            .ToList();

        // Get Categories to restore
        var categoriesToRestore = latestLinkedCategories.Where(x => categoryKeys.Contains(x.Entity.CategoryKey.ToString()) && x.Revision.Action == RevisionAction.Deleted).ToList();

        // Get the unchanged categories
        var unchangedCategories = latestLinkedCategories.Where(x =>
            !categoriesToDelete.Any(y => y.Key == x.Key) &&
            !categoriesToCreate.Any(y => y.CategoryKey == x.Entity.CategoryKey) &&
            !categoriesToRestore.Any(y => y.Key == x.Key) &&
            x.Revision.Action != RevisionAction.Deleted)
            .ToList();

        var updatedCategories = new List<EntityLifeCycleResult<CategoryViewLinkEntity, CategoryViewLinkRevisionEntity>>();


        foreach (var category in categoriesToDelete)
        {
            var categoryRevisionEntity = new CategoryViewLinkRevisionEntity
            {
                Key = category.Key,
                CreatedAt = timestamp,
            };

            var categoryResult = await _categoryViewLinkHandler.DeleteAsync(category.Key, categoryRevisionEntity, cancellationToken);
            updatedCategories.Add(categoryResult);
        }

        foreach (var category in categoriesToCreate)
        {
            var categoryRevisionEntity = new CategoryViewLinkRevisionEntity
            {
                Key = Guid.NewGuid(),
                CreatedAt = timestamp,
            };

            var categoryResult = await _categoryViewLinkHandler.CreateAsync(category, categoryRevisionEntity, cancellationToken);
            updatedCategories.Add(categoryResult);
        }

        foreach (var category in categoriesToRestore)
        {
            var categoryRevisionEntity = new CategoryViewLinkRevisionEntity
            {
                Key = category.Key,
                CreatedAt = timestamp,
            };

            var categoryResult = await _categoryViewLinkHandler.RestoreAsync(category.Key, categoryRevisionEntity, cancellationToken);
            updatedCategories.Add(categoryResult);
        }

        updatedCategories.AddRange(unchangedCategories.Select(x =>
        {
            x.Revision.Action = RevisionAction.Unmodified;
            return x;
        }));

        return (viewResult, updatedCategories);
    }
}