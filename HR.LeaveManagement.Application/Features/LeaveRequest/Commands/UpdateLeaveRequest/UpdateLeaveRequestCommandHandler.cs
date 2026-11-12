using HR.LeaveManagement.Application.Services;
using MediatR;


namespace HR.LeaveManagement.Application.Features.LeaveRequest.Commands.UpdateLeaveRequest;

public sealed class UpdateLeaveRequestCommandHandler(LeaveRequestWorkflow workflow)
    : IRequestHandler<UpdateLeaveRequestCommand, Unit>
{
    public async Task<Unit> Handle(UpdateLeaveRequestCommand request, CancellationToken cancellationToken)
    {
        await workflow.UpdateAsync(request, cancellationToken);
        return Unit.Value;
    }
}
