using AutoMapper;
using Roadkill.Api.Common.Request;
using Roadkill.Api.Common.Response;
using Roadkill.Core.Authorization;
using Roadkill.Core.Entities;

namespace Roadkill.Api.ObjectConverters
{
	public class MappingProfile : Profile
	{
		public MappingProfile()
		{
			CreateMap<PageRequest, Page>();
			CreateMap<UserRequest, RoadkillIdentityUser>();
			CreateMap<PageVersionRequest, PageVersion>();

			CreateMap<Page, PageResponse>();
			CreateMap<RoadkillIdentityUser, UserResponse>();
			CreateMap<PageVersion, PageVersionResponse>();
		}
	}
}
