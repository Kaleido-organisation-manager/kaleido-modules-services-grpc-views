using AutoMapper;
using Google.Protobuf.WellKnownTypes;
using Kaleido.Common.Services.Grpc.Models;
using Kaleido.Grpc.Views;
using Kaleido.Modules.Services.Grpc.Views.Common.Models;

namespace Kaleido.Modules.Services.Grpc.Views.Common.Mappers;

public class ViewMappingProfile : Profile
{
    public ViewMappingProfile()
    {
        CreateMap<View, ViewEntity>();

        CreateMap<EntityLifeCycleResult<ViewEntity, ViewRevisionEntity>, EntityLifeCycleResult<ViewWithCategories, BaseRevisionEntity>>();
        CreateMap<IEnumerable<EntityLifeCycleResult<CategoryViewLinkEntity, CategoryViewLinkRevisionEntity>>, EntityLifeCycleResult<ViewWithCategories, BaseRevisionEntity>>()
            .ForPath(dest => dest.Entity.Categories, opt => opt.MapFrom(src => src));

        CreateMap<ViewEntity, ViewWithCategories>();

        CreateMap<View, CategoryViewLinkEntity>()
            .ForMember(dest => dest.CategoryKey, opt => opt.MapFrom(src => Guid.Parse(src.Categories.First())));

        CreateMap<EntityLifeCycleResult<ViewWithCategories, BaseRevisionEntity>, ViewResponse>()
            .ForMember(dest => dest.View, opt => opt.MapFrom(src => src.Entity))
            .ForMember(dest => dest.Revision, opt => opt.MapFrom(src => src.Revision));
        CreateMap<ViewWithCategories, LinkedViewResponse>();
        CreateMap<EntityLifeCycleResult<CategoryViewLinkEntity, CategoryViewLinkRevisionEntity>, CategoryViewLinkResponse>()
            .ForMember(dest => dest.CategoryLink, opt => opt.MapFrom(src => src.Entity))
            .ForMember(dest => dest.Revision, opt => opt.MapFrom(src => src.Revision));
        CreateMap<CategoryViewLinkEntity, CategoryViewLink>()
            .ForMember(dest => dest.Category, opt => opt.MapFrom(src => src.CategoryKey));
        CreateMap<BaseRevisionEntity, BaseRevision>();
        CreateMap<CategoryViewLinkRevisionEntity, BaseRevision>();
        CreateMap<ViewRevisionEntity, BaseRevision>();
        CreateMap<Timestamp, DateTime>().ConvertUsing(src => src.ToDateTime());
        CreateMap<DateTime, Timestamp>().ConvertUsing(src => Timestamp.FromDateTime(src.ToUniversalTime()));

        CreateMap<EntityLifeCycleResult<CategoryViewLinkEntity, CategoryViewLinkRevisionEntity>, EntityLifeCycleResult<CategoryViewLinkEntity, CategoryViewLinkRevisionEntity>>()
            .ForMember(dest => dest.Entity, opt => opt.MapFrom(src => src.Entity))
            .ForMember(dest => dest.Revision, opt => opt.MapFrom(src => src.Revision));
        CreateMap<CategoryViewLinkEntity, CategoryViewLinkEntity>();
        CreateMap<CategoryViewLinkRevisionEntity, CategoryViewLinkRevisionEntity>();

        CreateMap<EntityLifeCycleResult<ViewEntity, ViewRevisionEntity>, EntityLifeCycleResult<ViewEntity, ViewRevisionEntity>>()
            .ForMember(dest => dest.Entity, opt => opt.MapFrom(src => src.Entity))
            .ForMember(dest => dest.Revision, opt => opt.MapFrom(src => src.Revision));
        CreateMap<ViewRevisionEntity, ViewRevisionEntity>();
        CreateMap<ViewEntity, ViewEntity>();
    }
}
