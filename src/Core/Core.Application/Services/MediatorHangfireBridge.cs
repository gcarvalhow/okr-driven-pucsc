using MediatR;
using System.ComponentModel;

namespace Core.Application.Services;

public class MediatorHangfireBridge(IMediator mediator)
{
    private readonly IMediator _mediator = mediator;
    public async Task Send(IRequest command)
        => await _mediator.Send(command);

    public async Task Send<T>(IRequest<T> command)
        => await _mediator.Send(command);

    [DisplayName("{0}")]
    public async Task Send(string _, IRequest command)
        => await _mediator.Send(command);
    public async Task Send<T>(string _, IRequest<T> command)
        => await _mediator.Send(command);
}