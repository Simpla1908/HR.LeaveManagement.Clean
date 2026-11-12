using HR.LeaveManagement.Application.Services;
using MediatR;
using HR.LeaveManagement.Application.Features.LeaveRequests.Requests.Commands;

namespace HR.LeaveManagement.Application.Features.LeaveRequests.Handlers.Commands;

public sealed class CreateLeaveRequestCommandHandler(LeaveRequestWorkflow workflow)
    : IRequestHandler<CreateLeaveRequestCommand, Unit>
{
    public async Task<Unit> Handle(CreateLeaveRequestCommand request, CancellationToken cancellationToken)
    {
        await workflow.CreateAsync(request, cancellationToken);
        return Unit.Value;
    }
}
