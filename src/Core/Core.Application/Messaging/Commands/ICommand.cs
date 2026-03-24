using Core.Shared.Results;
using MediatR;

namespace Core.Application.Messaging.Commands;

public interface ICommand : IRequest<Result>
{
}

public interface ICommand<TResponse> : IRequest<Result<TResponse>>
{
}