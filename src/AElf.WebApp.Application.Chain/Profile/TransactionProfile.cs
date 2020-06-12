using AElf.Types;
using AElf.WebApp.Application.Chain.Dto;
using AutoMapper;
using Google.Protobuf;

namespace AElf.WebApp.Application.Chain
{
    public class TransactionProfile : Profile
    {
        public TransactionProfile()
        {
            CreateMap<Transaction, TransactionDto>();

            CreateMap<TransactionResult, TransactionResultDto>()
                .ForMember(d => d.ReturnValue, opt => opt.MapFrom(s => s.ReturnValue.ToHex(false)))
                .ForMember(d => d.Bloom,
                    opt => opt.MapFrom(s =>
                        s.Bloom.Length == 0 ? ByteString.CopyFrom(new byte[256]).ToBase64() : s.Bloom.ToBase64()))
                .ForMember(d => d.Status, opt => opt.MapFrom(s => s.Status.ToString().ToUpper()));

            CreateMap<LogEvent, LogEventDto>();
        }
    }
}