using AutoMapper;
using HR.LeaveManagement.Application.DTOs.LeaveRequest;
using HR.LeaveManagement.Application.Features.LeaveAllocation.Queries.GetLeaveAllocationDetails;
using HR.LeaveManagement.Application.Features.LeaveAllocation.Queries.GetLeaveAllocations;
using HR.LeaveManagement.Application.Features.LeaveAllocations.Requests.Commands;
using HR.LeaveManagement.Application.Features.LeaveRequest.Commands.UpdateLeaveRequest;
using HR.LeaveManagement.Application.Features.LeaveRequest.Queries.GetLeaveRequestDetail;
using HR.LeaveManagement.Application.Features.LeaveRequests.Requests.Commands;
using HR.LeaveManagement.Domain;

namespace HR.LeaveManagement.Application.MappingProfiles;

public sealed class LeaveManagementProfile : Profile
{
    public LeaveManagementProfile()
    {
        CreateMap<LeaveAllocation, LeaveAllocationDto>();
        CreateMap<LeaveAllocation, LeaveAllocationDetailsDto>();
        CreateMap<UpdateLeaveAllocationCommand, LeaveAllocation>(MemberList.Source);
        CreateMap<LeaveRequest, LeaveRequestListDto>()
            .ForMember(x => x.Employee, o => o.Ignore());
        CreateMap<LeaveRequest, LeaveRequestDetailsDto>()
            .ForMember(x => x.Employee, o => o.Ignore())
            .ForMember(x => x.DateActioned, o => o.Ignore());
        // The server owns identity, audit fields and lifecycle state.
        CreateMap<CreateLeaveRequestCommand, LeaveRequest>(MemberList.Source);
        CreateMap<UpdateLeaveRequestCommand, LeaveRequest>(MemberList.Source)
            .ForMember(x => x.Cancelled, o => o.Ignore())
            .ForSourceMember(x => x.Cancelled, o => o.DoNotValidate());
    }
}
