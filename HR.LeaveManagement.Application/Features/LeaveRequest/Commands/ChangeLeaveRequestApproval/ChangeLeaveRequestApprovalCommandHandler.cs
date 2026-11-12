using HR.LeaveManagement.Application.Services;
using MediatR;


namespace HR.LeaveManagement.Application.Features.LeaveRequest.Commands.ChangeLeaveRequestApproval;

public sealed class ChangeLeaveRequestApprovalCommandHandler(LeaveRequestWorkflow workflow)
    : IRequestHandler<ChangeLeaveRequestApprovalCommand, Unit>
{
    public async Task<Unit> Handle(ChangeLeaveRequestApprovalCommand request, CancellationToken cancellationToken)
    {
        await workflow.ChangeApprovalAsync(request.Id, request.Approved, cancellationToken);
        return Unit.Value;
    }
}
