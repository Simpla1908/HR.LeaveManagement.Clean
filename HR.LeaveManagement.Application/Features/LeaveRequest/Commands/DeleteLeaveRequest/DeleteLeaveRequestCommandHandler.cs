using HR.LeaveManagement.Application.Services;
using MediatR;


namespace HR.LeaveManagement.Application.Features.LeaveRequest.Commands.DeleteLeaveRequest;

public sealed class DeleteLeaveRequestCommandHandler(LeaveRequestWorkflow workflow)
    : IRequestHandler<DeleteLeaveRequestCommand, Unit>
{
    public async Task<Unit> Handle(DeleteLeaveRequestCommand request, CancellationToken cancellationToken)
    {
        await workflow.DeleteAsync(request.Id, cancellationToken);
        return Unit.Value;
    }
}
