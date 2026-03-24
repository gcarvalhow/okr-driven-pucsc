using Core.Shared.Results;
using MediatR;

namespace Core.Application.Messaging.Queries;

public interface IQuery<TResponse> : IRequest<Result<TResponse>>
{
}