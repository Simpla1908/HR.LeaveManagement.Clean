using HR.LeaveManagement.Application.Services;
using MediatR;


namespace HR.LeaveManagement.Application.Features.LeaveRequest.Commands.CancelLeaveRequest;

public sealed class CancelLeaveRequestCommandHandler(LeaveRequestWorkflow workflow)
    : IRequestHandler<CancelLeaveRequestCommand, Unit>
{
    public async Task<Unit> Handle(CancelLeaveRequestCommand request, CancellationToken cancellationToken)
    {
        await workflow.CancelAsync(request.Id, cancellationToken);
        return Unit.Value;
    }
}
