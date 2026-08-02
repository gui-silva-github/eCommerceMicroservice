using AutoMapper;
using eCommerce.OrdersMicroservice.BusinessLogicLayer.DTO;
using eCommerce.OrdersMicroservice.DataAccessLayer.Entities;

namespace eCommerce.OrdersMicroservice.BusinessLogicLayer.Mappers
{
    public class OrderToOrderResponseMappingProfile : Profile
    {
        public OrderToOrderResponseMappingProfile()
        {
            CreateMap<Order, OrderResponse>()
                .ForMember(dest => dest.PersonName, opt => opt.Ignore())
                .ForMember(dest => dest.Email, opt => opt.Ignore());
        }
    }
}
