
using AutoMapper;
using HR.LeaveManagement.Application.Features.LeaveType.Commands.CreateLeaveType;
using HR.LeaveManagement.Application.Features.LeaveType.Commands.UpdateLeaveType;
using HR.LeaveManagement.Application.Features.LeaveType.Queries.GetAllLeaveTypes;
using HR.LeaveManagement.Application.Features.LeaveType.Queries.GetLeaveTypeDetails;
using HR.LeaveManagement.Domain;

namespace HR.LeaveManagement.Application.MappingProfiles;

public class LeaveTypeProfile : Profile
{
    public LeaveTypeProfile()
    {
        CreateMap<LeaveTypeDto, LeaveType>(MemberList.Source).ReverseMap();

        CreateMap<LeaveType, LeaveTypeDetailsDto>();

        CreateMap<CreateLeaveTypeCommand, LeaveType>(MemberList.Source);

        CreateMap<UpdateLeaveTypeCommand, LeaveType>(MemberList.Source);
    }
}
