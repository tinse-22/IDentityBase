using AutoMapper;

namespace BaseIdentity.Application.Mapping
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<ApplicationUser, UserResponse>().ReverseMap();
            CreateMap<ApplicationUser, CurrentUserResponse>().ReverseMap();
            CreateMap<UserRegisterRequest, ApplicationUser>()
                .ForMember(
                    dest => dest.Gender,
                    opt => opt.MapFrom(src => src.Gender.HasValue ? src.Gender.Value.ToString() : string.Empty))
                .ReverseMap();

        }
    }
}
