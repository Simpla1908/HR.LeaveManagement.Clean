using AutoMapper;
using HR.LeaveManagement.Application.Contracts.Identity;
using HR.LeaveManagement.Application.Exceptions;
using HR.LeaveManagement.Application.Contracts.Persistence;
using HR.LeaveManagement.Application.Features.LeaveAllocation.Queries.GetLeaveAllocations;
using MediatR;

namespace HR.LeaveManagement.Application.Features.LeaveAllocations.Handlers.Queries
{
    public class GetLeaveAllocationListHandler : IRequestHandler<GetLeaveAllocationListQuery, List<LeaveAllocationDto>>
    {
        private readonly ILeaveAllocationRepository _leaveAllocationRepository;
        private readonly IMapper _mapper;
        private readonly IUserService _userService;

        public GetLeaveAllocationListHandler(ILeaveAllocationRepository leaveAllocationRepository,
             IMapper mapper, IUserService userService)
        {
            _leaveAllocationRepository = leaveAllocationRepository;
            _mapper = mapper;
            _userService = userService;
        }

        public async Task<List<LeaveAllocationDto>> Handle(GetLeaveAllocationListQuery request, CancellationToken cancellationToken)
        {
            // To Add later
            // - Get records for specific user
            // - Get allocations per employee

            var leaveAllocations = request.IsLoggedInUser || !_userService.IsAdministrator
                ? await _leaveAllocationRepository.GetLeaveAllocationsWithDetails(_userService.UserId ?? throw new ForbiddenException())
                : await _leaveAllocationRepository.GetLeaveAllocationsWithDetails();
            var allocations = _mapper.Map<List<LeaveAllocationDto>>(leaveAllocations);

            return allocations;
        }
    }
}
