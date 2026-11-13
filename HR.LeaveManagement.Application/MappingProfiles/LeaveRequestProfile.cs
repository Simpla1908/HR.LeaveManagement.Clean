using AutoMapper;
using HR.LeaveManagement.Application.DTOs.LeaveRequest;
using HR.LeaveManagement.Application.Features.LeaveRequest.Commands.UpdateLeaveRequest;
using HR.LeaveManagement.Application.Features.LeaveRequest.Queries.GetLeaveRequestDetail;
using HR.LeaveManagement.Application.Features.LeaveRequests.Requests.Commands;
using HR.LeaveManagement.Domain;

namespace HR.LeaveManagement.Application.MappingProfiles;

public sealed class LeaveRequestProfile : Profile
{
    public LeaveRequestProfile()
    {
        CreateMap<LeaveRequest, LeaveRequestListDto>()
            .ForMember(x => x.Employee, o => o.Ignore());
        CreateMap<LeaveRequest, LeaveRequestDetailsDto>()
            .ForMember(x => x.Employee, o => o.Ignore())
            .ForMember(x => x.DateActioned, o => o.Ignore());

        // Le serveur gère l'identité, les champs d'audit et l'état de la demande.
        CreateMap<CreateLeaveRequestCommand, LeaveRequest>(MemberList.Source);
        CreateMap<UpdateLeaveRequestCommand, LeaveRequest>(MemberList.Source)
            .ForMember(x => x.Cancelled, o => o.Ignore())
            .ForSourceMember(x => x.Cancelled, o => o.DoNotValidate());
    }
}
