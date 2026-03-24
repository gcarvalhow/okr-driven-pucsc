# Societiza API — Contexto Arquitetural Completo

> Documento gerado em 2026-03-24. Use como referência para estruturar projetos do zero com a mesma arquitetura.

---

## Visão Geral

**Stack**: .NET 8 · PostgreSQL · MongoDB · RabbitMQ · Hangfire
**Padrões**: Event-Driven Modular Monolith · DDD · CQRS · Clean Architecture · Event Sourcing

### Estrutura de diretórios

```
src/
├── Core/
│   ├── Core.Domain/
│   ├── Core.Application/
│   ├── Core.Infrastructure/
│   ├── Core.Persistence/
│   ├── Core.Shared/
│   └── Core.Endpoints/
├── Modules/
│   └── {Module}/
│       ├── {Module}.Domain/
│       ├── {Module}.Application/
│       ├── {Module}.Infrastructure/
│       ├── {Module}.Persistence/
│       └── {Module}.Shared/
└── Web/
    ├── Program.cs
    ├── Endpoints/
    ├── Middlewares/
    ├── Extensions/
    └── ServiceInstallers/
tests/
Docker/
```

---

## CAMADA CORE

### Core.Domain

Primitivos e contratos de domínio usados por todos os módulos.

---

#### `src/Core/Core.Domain/Primitives/Entity.cs`
```csharp
using Core.Domain.Primitives.Interfaces;

namespace Core.Domain.Primitives;

public class Entity : IEntity
{
    public Guid Id { get; protected set; } = Guid.NewGuid();

    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; protected set; }

    public bool IsDeleted { get; protected set; } = false;

    public override bool Equals(object? obj)
        => obj is Entity entity && Id.Equals(entity.Id);

    public override int GetHashCode()
        => HashCode.Combine(Id);
}
```

---

#### `src/Core/Core.Domain/Primitives/AggregateRoot.cs`
```csharp
using Core.Domain.Events.Interfaces;
using Core.Domain.Exceptions;
using Core.Domain.Primitives.Interfaces;

namespace Core.Domain.Primitives;

public abstract class AggregateRoot : Entity, IAggregateRoot
{
    private readonly Queue<IDomainEvent> _events = new();

    public ulong Version { get; private set; } = 0;

    public void LoadFromStream(List<IDomainEvent> events)
    {
        foreach (var @event in events.OrderBy(ev => ev.Version))
        {
            ApplyEvent(@event);
            Version = @event.Version;
        }
    }

    public bool TryDequeueEvent(out IDomainEvent @event)
        => _events.TryDequeue(out @event!);

    private void EnqueueEvent(IDomainEvent @event)
        => _events.Enqueue(@event);

    protected void RaiseEvent<TEvent>(Func<ulong, TEvent> func) where TEvent : IDomainEvent
        => RaiseEvent((func as Func<ulong, IDomainEvent>)!);

    protected void RaiseEvent(Func<ulong, IDomainEvent> onRaise)
    {
        if (IsDeleted)
            throw new AggregateIsDeletedException(Id);

        var @event = onRaise(++Version);

        ApplyEvent(@event);
        EnqueueEvent(@event);
    }

    protected abstract void ApplyEvent(IDomainEvent @event);
}
```

---

#### `src/Core/Core.Domain/Primitives/Interfaces/IEntity.cs`
```csharp
namespace Core.Domain.Primitives.Interfaces;

public interface IEntity
{
    Guid Id { get; }
    DateTimeOffset CreatedAt { get; }
    DateTimeOffset? UpdatedAt { get; }
    bool IsDeleted { get; }
}
```

#### `src/Core/Core.Domain/Primitives/Interfaces/IAggregateRoot.cs`
```csharp
using Core.Domain.Events.Interfaces;

namespace Core.Domain.Primitives.Interfaces;

public interface IAggregateRoot : IEntity
{
    ulong Version { get; }
    void LoadFromStream(List<IDomainEvent> events);
    bool TryDequeueEvent(out IDomainEvent @event);
}
```

#### `src/Core/Core.Domain/Primitives/Interfaces/IProjectionModel.cs`
```csharp
namespace Core.Domain.Primitives.Interfaces;

public interface IProjectionModel
{
    Guid Id { get; }
}
```

---

#### `src/Core/Core.Domain/Events/Message.cs`
```csharp
namespace Core.Domain.Events;

public abstract record Message
{
    public DateTimeOffset Timestamp { get; private init; } = DateTimeOffset.UtcNow;
}
```

#### `src/Core/Core.Domain/Events/Event.cs`
```csharp
using Core.Domain.Events.Interfaces;

namespace Core.Domain.Events;

public abstract record Event : Message, IEvent;
```

#### `src/Core/Core.Domain/Events/Interfaces/IDomainEvent.cs`
```csharp
namespace Core.Domain.Events.Interfaces;

public interface IDomainEvent : IEvent
{
    ulong Version { get; }
}
```

#### `src/Core/Core.Domain/Events/Interfaces/IEvent.cs`
```csharp
namespace Core.Domain.Events.Interfaces;

public interface IEvent { }
```

#### `src/Core/Core.Domain/Events/Interfaces/IDelayedEvent.cs`
```csharp
namespace Core.Domain.Events.Interfaces;

public interface IDelayedEvent : IEvent { }
```

---

#### `src/Core/Core.Domain/EventStore/StoreEvent.cs`
```csharp
using Core.Domain.Events.Interfaces;
using Core.Domain.Primitives.Interfaces;

namespace Core.Domain.EventStore;

public record StoreEvent<TAggregate>(Guid AggregateId, string EventType, IDomainEvent Event, ulong Version, DateTimeOffset Timestamp)
    where TAggregate : IAggregateRoot
{
    public static StoreEvent<TAggregate> Create(TAggregate aggregate, IDomainEvent @event)
        => new(aggregate.Id, @event.GetType().Name, @event, aggregate.Version, @event.Timestamp);
}
```

#### `src/Core/Core.Domain/EventStore/Snapshot.cs`
```csharp
using Core.Domain.Primitives.Interfaces;

namespace Core.Domain.EventStore;

public record Snapshot<TAggregate>(Guid AggregateId, TAggregate Aggregate, ulong Version, DateTimeOffset Timestamp)
    where TAggregate : IAggregateRoot
{
    public static Snapshot<TAggregate> Create(TAggregate aggregate, StoreEvent<TAggregate> @event)
        => new(aggregate.Id, aggregate, @event.Version, @event.Timestamp);
}
```

---

#### `src/Core/Core.Domain/Pagination/Paging.cs`
```csharp
namespace Core.Domain.Pagination;

public record Paging
{
    private const int UpperSize = 100;
    private const int DefaultSize = 10;
    private const int DefaultNumber = 1;
    private const int Zero = 0;

    public Paging(int size = DefaultSize, int number = DefaultNumber)
    {
        Size = size switch
        {
            Zero => DefaultSize,
            > UpperSize => UpperSize,
            _ => size
        };

        Number = number is Zero ? DefaultNumber : number;
    }

    public int Size { get; }
    public int Number { get; }
}
```

#### `src/Core/Core.Domain/Exceptions/AggregateIsDeletedException.cs`
```csharp
namespace Core.Domain.Exceptions;

public class AggregateIsDeletedException(Guid id)
    : Exception($"Aggregate with id '{id}' is deleted and cannot be modified.");
```

---

### Core.Shared

Tipos compartilhados: Result, Errors, Responses, Extensions.

---

#### `src/Core/Core.Shared/Errors/Error.cs`
```csharp
namespace Core.Shared.Errors;

public class Error(string code, string message) : IEquatable<Error>
{
    public static readonly Error None = new(string.Empty, string.Empty);
    public static readonly Error NullValue = new("Error.NullValue", "The specified result value is null.");
    public static readonly Error ConditionNotMet = new("Error.ConditionNotMet", "The specified condition was not met.");

    public string Code { get; } = code;
    public string Message { get; } = message;

    public static implicit operator string(Error error) => error.Code;

    public static bool operator ==(Error? a, Error? b)
    {
        if (a is null && b is null) return true;
        if (a is null || b is null) return false;
        return a.Equals(b);
    }

    public static bool operator !=(Error? a, Error? b) => !(a == b);

    public virtual bool Equals(Error? other)
    {
        if (other is null) return false;
        return Code == other.Code && Message == other.Message;
    }

    public override bool Equals(object? obj) => obj is Error error && Equals(error);
    public override int GetHashCode() => HashCode.Combine(Code, Message);
    public override string ToString() => Code;
}
```

#### `src/Core/Core.Shared/Errors/NotFoundError.cs`
```csharp
namespace Core.Shared.Errors;

public sealed class NotFoundError(Error error) : Error(error.Code, error.Message) { }
```

#### `src/Core/Core.Shared/Errors/ConflictError.cs`
```csharp
namespace Core.Shared.Errors;

public sealed class ConflictError(Error error) : Error(error.Code, error.Message) { }
```

#### `src/Core/Core.Shared/Errors/NoContentError.cs`
```csharp
namespace Core.Shared.Errors;

public sealed class NoContentError(Error error) : Error(error.Code, error.Message) { }
```

---

#### `src/Core/Core.Shared/Results/Result.cs`
```csharp
using Core.Shared.Errors;

namespace Core.Shared.Results;

public class Result
{
    public bool IsSuccess { get; }
    public Error Error { get; }

    protected internal Result(bool isSuccess, Error error)
    {
        if (isSuccess && error != Error.None) throw new InvalidOperationException();
        if (!isSuccess && error == Error.None) throw new InvalidOperationException();

        IsSuccess = isSuccess;
        Error = error;
    }

    public static Result Success() => new(true, Error.None);
    public static Result<TValue> Success<TValue>(TValue value) => new(value, true, Error.None);

    public static Result Failure(Error error) => new(false, error);
    public static Result<TValue> Failure<TValue>(Error error) => new(default, false, error);

    public static Result Create(bool condition) => condition ? Success() : Failure(Error.ConditionNotMet);
    public static Result<TValue> Create<TValue>(TValue? value) => value is not null ? Success(value) : Failure<TValue>(Error.NullValue);

    public static async Task<Result> FirstFailureOrSuccess(params Func<Task<Result>>[] results)
    {
        foreach (Func<Task<Result>> resultTask in results)
        {
            Result result = await resultTask();
            if (!result.IsSuccess) return result;
        }
        return Success();
    }
}

public class Result<TValue> : Result
{
    private readonly TValue? _value;

    protected internal Result(TValue? value, bool isSuccess, Error error)
        : base(isSuccess, error) =>
        _value = value;

    public TValue Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("The value of a failure result can not be accessed.");

    public static implicit operator Result<TValue>(TValue? value) => Create(value);
}
```

#### `src/Core/Core.Shared/Results/ValidationResult.cs`
```csharp
using Core.Shared.Errors;
using Core.Shared.Results.Interfaces;

namespace Core.Shared.Results;

public sealed class ValidationResult : Result, IValidationResult
{
    public Error[] Errors { get; }

    private ValidationResult(Error[] errors)
        : base(false, IValidationResult.ValidationError) =>
        Errors = errors;

    public static ValidationResult WithErrors(Error[] errors) => new(errors);
}

public sealed class ValidationResult<TValue> : Result<TValue>, IValidationResult
{
    public Error[] Errors { get; }

    private ValidationResult(Error[] errors)
        : base(default, false, IValidationResult.ValidationError) =>
        Errors = errors;

    public static ValidationResult<TValue> WithErrors(Error[] errors) => new(errors);
}
```

#### `src/Core/Core.Shared/Results/Interfaces/IValidationResult.cs`
```csharp
using Core.Shared.Errors;

namespace Core.Shared.Results.Interfaces;

public interface IValidationResult
{
    public static readonly Error ValidationError = new("ValidationError", "One or more validation errors occurred.");
    Error[] Errors { get; }
}
```

#### `src/Core/Core.Shared/Results/ResultExtensions.cs`
```csharp
using Core.Shared.Errors;

namespace Core.Shared.Results;

public static class ResultExtensions
{
    public static Result<TOut> Map<TOut>(this Result result, Func<TOut> func) =>
        result.IsSuccess ? func() : Result.Failure<TOut>(result.Error);

    public static async Task<Result<TOut>> Map<TOut>(this Task<Result> resultTask, Func<TOut> func) =>
        (await resultTask).Map(func);

    public static Result<TOut> Map<TIn, TOut>(this Result<TIn> result, Func<TIn, TOut> func) =>
        result.IsSuccess ? func(result.Value) : Result.Failure<TOut>(result.Error);

    public static async Task<Result<TOut>> Map<TIn, TOut>(this Task<Result<TIn>> resultTask, Func<TIn, TOut> func) =>
        (await resultTask).Map(func);

    public static Result<TOut> Bind<TIn, TOut>(this Result<TIn> result, Func<TIn, Result<TOut>> func) =>
        result.IsSuccess ? func(result.Value) : Result.Failure<TOut>(result.Error);

    public static async Task<Result<TOut>> Bind<TIn, TOut>(this Result<TIn> result, Func<TIn, Task<Result<TOut>>> func) =>
        result.IsSuccess ? await func(result.Value) : Result.Failure<TOut>(result.Error);

    public static async Task<Result<TOut>> Bind<TOut>(this Result result, Func<Task<Result<TOut>>> func) =>
        result.IsSuccess ? await func() : Result.Failure<TOut>(result.Error);

    public static async Task<Result<TOut>> Bind<TIn, TOut>(this Task<Result<TIn>> resultTask, Func<TIn, Task<Result<TOut>>> func) =>
        await (await resultTask).Bind(func);

    public static Result<TIn> Filter<TIn>(this Result<TIn> result, Func<TIn, bool> predicate)
    {
        if (!result.IsSuccess) return result;
        return predicate(result.Value) ? result : Result.Failure<TIn>(Error.ConditionNotMet);
    }

    // Match — terminal combinador usado nos Endpoints
    public static TOut Match<TIn, TOut>(this Result<TIn> result, Func<TIn, TOut> onSuccess, Func<Result, TOut> onFailure) =>
        result.IsSuccess ? onSuccess(result.Value) : onFailure(result);

    public static async Task<TOut> Match<TIn, TOut>(this Task<Result<TIn>> resultTask, Func<TIn, TOut> onSuccess, Func<Result, TOut> onFailure) =>
        (await resultTask).Match(onSuccess, onFailure);
}
```

---

#### `src/Core/Core.Shared/Response/IdentifierResponse.cs`
```csharp
namespace Core.Shared.Response;

public sealed record IdentifierResponse(Guid Id);
```

#### `src/Core/Core.Shared/Extensions/FunctionalExtensions.cs`
```csharp
namespace Core.Shared.Extensions;

public static class FunctionalExtensions
{
    public static T Tap<T>(this T instance, Action action)
    {
        action();
        return instance;
    }

    public static T Tap<T>(this T instance, Action<T> action)
    {
        action(instance);
        return instance;
    }

    public static void ForEach<T>(this IEnumerable<T> collection, Action<T> action)
    {
        foreach (T element in collection)
            action(element);
    }
}
```

#### `src/Core/Core.Shared/Extensions/StringExtensions.cs`
```csharp
using System.Text.RegularExpressions;

namespace Core.Shared.Extensions;

public static partial class StringExtensions
{
    [GeneratedRegex(@"[^0-9]")]
    private static partial Regex OnlyDigitsRegex();

    public static string OnlyDigits(this string input)
        => string.IsNullOrWhiteSpace(input)
            ? string.Empty
            : OnlyDigitsRegex().Replace(input, string.Empty);

    public static bool IsValidCnpj(this string cnpj)
    {
        if (string.IsNullOrWhiteSpace(cnpj)) return false;
        cnpj = cnpj.OnlyDigits();
        if (cnpj.Length != 14) return false;
        if (cnpj.Distinct().Count() == 1) return false;

        var m1 = new[] { 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };
        var m2 = new[] { 6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };
        var temp = cnpj[..12];
        var sum = temp.Select((c, i) => int.Parse(c.ToString()) * m1[i]).Sum();
        var r = sum % 11; r = r < 2 ? 0 : 11 - r;
        var d = r.ToString();
        temp += d;
        sum = temp.Select((c, i) => int.Parse(c.ToString()) * m2[i]).Sum();
        r = sum % 11; r = r < 2 ? 0 : 11 - r;
        d += r.ToString();
        return cnpj.EndsWith(d);
    }
}
```

---

### Core.Application

Contratos MediatR, behaviors de pipeline, serviços de aplicação.

---

#### `src/Core/Core.Application/Messaging/Commands/ICommand.cs`
```csharp
using Core.Shared.Results;
using MediatR;

namespace Core.Application.Messaging.Commands;

public interface ICommand : IRequest<Result> { }
public interface ICommand<TResponse> : IRequest<Result<TResponse>> { }
```

#### `src/Core/Core.Application/Messaging/Commands/ICommandHandler.cs`
```csharp
using Core.Shared.Results;
using MediatR;

namespace Core.Application.Messaging.Commands;

public interface ICommandHandler<in TCommand> : IRequestHandler<TCommand, Result>
    where TCommand : ICommand { }

public interface ICommandHandler<in TCommand, TResponse> : IRequestHandler<TCommand, Result<TResponse>>
    where TCommand : ICommand<TResponse> { }
```

#### `src/Core/Core.Application/Messaging/Queries/IQuery.cs`
```csharp
using Core.Shared.Results;
using MediatR;

namespace Core.Application.Messaging.Queries;

public interface IQuery<TResponse> : IRequest<Result<TResponse>> { }
```

#### `src/Core/Core.Application/Messaging/Queries/IQueryHandler.cs`
```csharp
using Core.Shared.Results;
using MediatR;

namespace Core.Application.Messaging.Queries;

public interface IQueryHandler<in TQuery, TResponse> : IRequestHandler<TQuery, Result<TResponse>>
    where TQuery : IQuery<TResponse> { }
```

---

#### `src/Core/Core.Application/EventBus/IEventBus.cs`
```csharp
using Core.Domain.Events.Interfaces;

namespace Core.Application.EventBus;

public interface IEventBus
{
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken)
        where TEvent : class, IEvent;

    Task SchedulePublishAsync<TEvent>(TEvent @event, DateTimeOffset scheduledTime, CancellationToken cancellationToken)
        where TEvent : class, IDelayedEvent;
}
```

#### `src/Core/Core.Application/EventBus/IEventHandler.cs`
```csharp
using Core.Domain.Events.Interfaces;

namespace Core.Application.EventBus;

public interface IEventHandler<in TEvent>
    where TEvent : IEvent
{
    Task Handle(TEvent @event, CancellationToken cancellationToken = default);
}
```

#### `src/Core/Core.Application/EventStore/IEventStore.cs`
```csharp
using Core.Domain.Events.Interfaces;
using Core.Domain.EventStore;
using Core.Domain.Primitives.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Core.Application.EventStore;

public interface IEventStore<TContext> where TContext : DbContext
{
    Task AppendAsync<TAggregate>(StoreEvent<TAggregate> storeEvent, CancellationToken cancellationToken)
        where TAggregate : IAggregateRoot;

    Task AppendAsync<TAggregate>(Snapshot<TAggregate> snapshot, CancellationToken cancellationToken)
        where TAggregate : IAggregateRoot;

    Task<List<IDomainEvent>> GetStreamAsync<TAggregate>(Guid id, ulong version, CancellationToken cancellationToken)
        where TAggregate : IAggregateRoot;

    Task<Snapshot<TAggregate>?> GetSnapshotAsync<TAggregate>(Guid id, CancellationToken cancellationToken)
        where TAggregate : IAggregateRoot;

    IAsyncEnumerable<Guid> StreamAggregatesId<TAggregate>()
        where TAggregate : IAggregateRoot;

    Task<Guid> StreamAggregateId<TAggregate>(Expression<Func<StoreEvent<TAggregate>, bool>> predicate, CancellationToken cancellationToken)
        where TAggregate : IAggregateRoot;
}
```

#### `src/Core/Core.Application/UnitOfWork/IUnitOfWork.cs`
```csharp
using Microsoft.EntityFrameworkCore;

namespace Core.Application.UnitOfWork;

public interface IUnitOfWork<TContext> where TContext : DbContext
{
    Task ExecuteAsync(Func<CancellationToken, Task> operationAsync, CancellationToken cancellationToken);
}
```

---

#### `src/Core/Core.Application/Services/Interfaces/IApplicationService.cs`
```csharp
using Core.Domain.Events.Interfaces;
using Core.Domain.Primitives.Interfaces;
using Core.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace Core.Application.Services.Interfaces;

public interface IApplicationService<TContext> where TContext : DbContext
{
    Task AppendEventsAsync<TAggregate>(TAggregate aggregate, CancellationToken cancellationToken)
        where TAggregate : IAggregateRoot;

    Task<Result<TAggregate>> LoadAggregateAsync<TAggregate>(Guid id, CancellationToken cancellationToken)
        where TAggregate : class, IAggregateRoot, new();

    IAsyncEnumerable<Guid> StreamAggregatesId<TAggregate>()
        where TAggregate : IAggregateRoot;

    Task PublishEventAsync(IEvent @event, CancellationToken cancellationToken);

    Task SchedulePublishAsync(IDelayedEvent @event, DateTimeOffset scheduledTime, CancellationToken cancellationToken);
}
```

#### `src/Core/Core.Application/Services/ApplicationService.cs`
```csharp
using Core.Application.EventBus;
using Core.Application.EventStore;
using Core.Application.Services.Interfaces;
using Core.Application.UnitOfWork;
using Core.Domain.Events.Interfaces;
using Core.Domain.EventStore;
using Core.Domain.Primitives.Interfaces;
using Core.Shared.Errors;
using Core.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace Core.Application.Services;

public class ApplicationService<TContext>(
    IEventStore<TContext> eventStore,
    IEventBus eventBusGateway,
    IUnitOfWork<TContext> unitOfWork) : IApplicationService<TContext>
    where TContext : DbContext
{
    public async Task<Result<TAggregate>> LoadAggregateAsync<TAggregate>(Guid id, CancellationToken cancellationToken)
        where TAggregate : class, IAggregateRoot, new()
    {
        var snapshot = await eventStore.GetSnapshotAsync<TAggregate>(id, cancellationToken);
        var events = await eventStore.GetStreamAsync<TAggregate>(id, snapshot?.Version ?? 0, cancellationToken);

        if (snapshot is null && events is { Count: 0 })
            return Result.Failure<TAggregate>(new NotFoundError(new("Aggregate.NotFound", $"Aggregate {typeof(TAggregate).Name} not found")));

        var aggregate = snapshot?.Aggregate ?? new();
        aggregate.LoadFromStream(events);

        if (aggregate.IsDeleted)
            return Result.Failure<TAggregate>(new ConflictError(new("Aggregate.Deleted", $"Aggregate {typeof(TAggregate).Name} is deleted")));

        return Result.Success(aggregate);
    }

    public async Task AppendEventsAsync<TAggregate>(TAggregate aggregate, CancellationToken cancellationToken)
        where TAggregate : IAggregateRoot
    {
        var eventsToPublish = new List<IEvent>();

        await unitOfWork.ExecuteAsync(
            operationAsync: async ct =>
            {
                while (aggregate.TryDequeueEvent(out var @event))
                {
                    var storeEvent = StoreEvent<TAggregate>.Create(aggregate, @event);
                    await eventStore.AppendAsync(storeEvent, ct);

                    // Snapshot automático a cada 10 eventos
                    if (storeEvent.Version % 10 is 0)
                    {
                        var snapshot = Snapshot<TAggregate>.Create(aggregate, storeEvent);
                        await eventStore.AppendAsync(snapshot, ct);
                    }

                    eventsToPublish.Add(@event);
                }
            },
            cancellationToken: cancellationToken);

        foreach (var @event in eventsToPublish)
            await eventBusGateway.PublishAsync(@event, cancellationToken);
    }

    public IAsyncEnumerable<Guid> StreamAggregatesId<TAggregate>()
        where TAggregate : IAggregateRoot
            => eventStore.StreamAggregatesId<TAggregate>();

    public Task PublishEventAsync(IEvent @event, CancellationToken cancellationToken)
        => eventBusGateway.PublishAsync(@event, cancellationToken);

    public Task SchedulePublishAsync(IDelayedEvent @event, DateTimeOffset scheduledTime, CancellationToken cancellationToken)
        => eventBusGateway.SchedulePublishAsync(@event, scheduledTime, cancellationToken);
}
```

---

#### `src/Core/Core.Application/Services/Interfaces/IJobSchedulerService.cs`
```csharp
using MediatR;

namespace Core.Application.Services.Interfaces;

public interface IJobSchedulerService
{
    void Enqueue(IRequest request);
    void Enqueue<T>(IRequest<T> request);
    void Enqueue(string jobName, IRequest request);
    void Schedule(string jobName, TimeSpan scheduleAt, IRequest request);
    void Schedule<T>(string jobName, TimeSpan scheduleAt, IRequest<T> request);
    void Schedule(string jobName, DateTimeOffset scheduleAt, IRequest request);
    void Schedule<T>(string jobName, DateTimeOffset scheduleAt, IRequest<T> request);
    void ScheduleRecurring<T>(string jobName, string cronExpression, IRequest<T> request);
}
```

#### `src/Core/Core.Application/Services/Interfaces/IEmailService.cs`
```csharp
namespace Core.Application.Services.Interfaces;

public interface IEmailService
{
    Task SendAsync(string toEmail, string toName, string subject, string body, CancellationToken cancellationToken);
}
```

---

#### `src/Core/Core.Application/Services/MediatorHangfireBridge.cs`
```csharp
using MediatR;
using System.ComponentModel;

namespace Core.Application.Services;

public class MediatorHangfireBridge(IMediator mediator)
{
    private readonly IMediator _mediator = mediator;

    public async Task Send(IRequest command) => await _mediator.Send(command);
    public async Task Send<T>(IRequest<T> command) => await _mediator.Send(command);

    [DisplayName("{0}")]
    public async Task Send(string _, IRequest command) => await _mediator.Send(command);
    public async Task Send<T>(string _, IRequest<T> command) => await _mediator.Send(command);
}
```

#### `src/Core/Core.Application/Services/JobSchedulerService.cs`
```csharp
using Core.Application.Services.Interfaces;
using Hangfire;
using MediatR;

namespace Core.Application.Services;

public class JobSchedulerService : IJobSchedulerService
{
    public void Enqueue(string jobName, IRequest request)
        => new BackgroundJobClient().Enqueue<MediatorHangfireBridge>(b => b.Send(jobName, request));

    public void Enqueue(IRequest request)
        => new BackgroundJobClient().Enqueue<MediatorHangfireBridge>(b => b.Send(request));

    public void Enqueue<T>(IRequest<T> request)
        => new BackgroundJobClient().Enqueue<MediatorHangfireBridge>(b => b.Send(request));

    public void Schedule(string jobName, TimeSpan scheduleAt, IRequest request)
        => new BackgroundJobClient().Schedule<MediatorHangfireBridge>(b => b.Send(jobName, request), scheduleAt);

    public void Schedule<T>(string jobName, TimeSpan scheduleAt, IRequest<T> request)
        => new BackgroundJobClient().Schedule<MediatorHangfireBridge>(b => b.Send(jobName, request), scheduleAt);

    public void Schedule(string jobName, DateTimeOffset scheduleAt, IRequest request)
        => new BackgroundJobClient().Schedule<MediatorHangfireBridge>(b => b.Send(jobName, request), scheduleAt);

    public void Schedule<T>(string jobName, DateTimeOffset scheduleAt, IRequest<T> request)
        => new BackgroundJobClient().Schedule<MediatorHangfireBridge>(b => b.Send(jobName, request), scheduleAt);

    public void ScheduleRecurring<T>(string jobName, string cronExpression, IRequest<T> request)
        => new RecurringJobManager().AddOrUpdate<MediatorHangfireBridge>(jobName, b => b.Send(jobName, request), cronExpression);
}
```

---

#### `src/Core/Core.Application/Behaviors/ValidationPipelineBehavior.cs`
```csharp
using Core.Application.Infrastructure;
using Core.Shared.Errors;
using Core.Shared.Results;
using FluentValidation;
using MediatR;

namespace Core.Application.Behaviors;

public sealed class ValidationPipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : class, IRequest<TResponse>
    where TResponse : Result
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationPipelineBehavior(IEnumerable<IValidator<TRequest>> validators)
        => _validators = validators;

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (!_validators.Any()) return await next();

        var context = new ValidationContext<TRequest>(request);
        var errors = await ValidateAsync(context, cancellationToken);

        if (errors.Length > 0)
            return ValidationResultFactory.Create<TResponse>(errors);

        return await next();
    }

    private async Task<Error[]> ValidateAsync(ValidationContext<TRequest> context, CancellationToken cancellationToken)
    {
        var results = await Task.WhenAll(_validators.Select(v => v.ValidateAsync(context, cancellationToken)));
        return results
            .SelectMany(r => r.Errors)
            .Where(e => e != null)
            .Select(e => new Error(e.ErrorCode, e.ErrorMessage))
            .Distinct()
            .ToArray();
    }
}
```

#### `src/Core/Core.Application/Infrastructure/ValidationResultFactory.cs`
```csharp
using Core.Shared.Errors;
using Core.Shared.Results;

namespace Core.Application.Infrastructure;

public static class ValidationResultFactory
{
    public static TResponse Create<TResponse>(Error[] errors) where TResponse : Result
    {
        if (typeof(TResponse) == typeof(Result))
            return (TResponse)(Result)ValidationResult.WithErrors(errors);

        var validationResultType = typeof(ValidationResult<>)
            .MakeGenericType(typeof(TResponse).GenericTypeArguments[0]);

        return (TResponse)validationResultType
            .GetMethod(nameof(ValidationResult.WithErrors))!
            .Invoke(null, [errors])!;
    }
}
```

---

#### `src/Core/Core.Application/Pagination/PagedResult.cs`
```csharp
using Core.Domain.Pagination;
using Core.Domain.Pagination.Interfaces;
using System.Text.Json.Serialization;

namespace Core.Application.Pagination;

public record PagedResult<TItem>(IReadOnlyCollection<TItem> Items, Paging Paging) : IPagedResult<TItem>
    where TItem : class
{
    public Page Page => new()
    {
        PageNumber = Paging.Number,
        PageSize = Paging.Size,
        HasNextPage = Items.Count > Paging.Size,
        HasPreviousPage = Paging.Number > 0
    };

    [JsonIgnore]
    private Paging Paging { get; } = Paging;

    public static IPagedResult<TItem> Create(Paging paging, IQueryable<TItem> source)
        => new PagedResult<TItem>(ApplyPagination(paging, source)?.ToList(), paging);

    private static IQueryable<TItem> ApplyPagination(Paging paging, IQueryable<TItem> source)
        => source.Skip(paging.Size * (paging.Number - 1)).Take(paging.Size + 1);
}
```

---

#### `src/Core/Core.Application/ServiceLifetimes/ITransient.cs`
```csharp
namespace Core.Application.ServiceLifetimes;

// Marker interface — implementar para auto-registro Transient via Scrutor
public interface ITransient { }
```

#### `src/Core/Core.Application/ServiceLifetimes/IScoped.cs`
```csharp
namespace Core.Application.ServiceLifetimes;

public interface IScoped { }
```

#### `src/Core/Core.Application/ServiceLifetimes/ISingleton.cs`
```csharp
namespace Core.Application.ServiceLifetimes;

public interface ISingleton { }
```

---

### Core.Infrastructure

Service installers, EventBus, extensões de DI.

---

#### `src/Core/Core.Infrastructure/Configurations/IServiceInstaller.cs`
```csharp
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Core.Infrastructure.Configurations;

public interface IServiceInstaller
{
    void Install(IServiceCollection services, IConfiguration configuration);
}
```

#### `src/Core/Core.Infrastructure/Configurations/IModuleInstaller.cs`
```csharp
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Core.Infrastructure.Configurations;

public interface IModuleInstaller
{
    void Install(IServiceCollection services, IConfiguration configuration);
}
```

#### `src/Core/Core.Infrastructure/Configurations/InstanceFactory.cs`
```csharp
using System.Reflection;

namespace Core.Infrastructure.Configurations;

internal static class InstanceFactory
{
    internal static IEnumerable<T> CreateFromAssemblies<T>(params Assembly[] assemblies) =>
        assemblies
            .SelectMany(a => a.DefinedTypes)
            .Where(IsAssignableToType<T>)
            .Select(type => (T)Activator.CreateInstance(type)!)
            .Cast<T>();

    private static bool IsAssignableToType<T>(TypeInfo typeInfo) =>
        typeof(T).IsAssignableFrom(typeInfo) && !typeInfo.IsInterface && !typeInfo.IsAbstract;
}
```

---

#### `src/Core/Core.Infrastructure/Extensions/ServiceCollectionExtensions.cs`
```csharp
using Core.Application.ServiceLifetimes;
using Core.Infrastructure.Configurations;
using Core.Shared.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Scrutor;
using System.Reflection;

namespace Core.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection InstallServicesFromAssemblies(
        this IServiceCollection services,
        IConfiguration configuration,
        params Assembly[] assemblies) =>
        services.Tap(() =>
            InstanceFactory
                .CreateFromAssemblies<IServiceInstaller>(assemblies)
                .ForEach(i => i.Install(services, configuration)));

    public static IServiceCollection InstallModulesFromAssemblies(
        this IServiceCollection services,
        IConfiguration configuration,
        params Assembly[] assemblies) =>
        services.Tap(() =>
            InstanceFactory
                .CreateFromAssemblies<IModuleInstaller>(assemblies)
                .ForEach(m => m.Install(services, configuration)));

    public static IServiceCollection AddTransientAsMatchingInterfaces(
        this IServiceCollection services, Assembly assembly) =>
        services.Scan(scan =>
            scan.FromAssemblies(assembly)
                .AddClasses(f => f.AssignableTo<ITransient>(), false)
                .UsingRegistrationStrategy(RegistrationStrategy.Throw)
                .AsMatchingInterface()
                .WithTransientLifetime());

    public static IServiceCollection AddScopedAsMatchingInterfaces(
        this IServiceCollection services, Assembly assembly) =>
        services.Scan(scan =>
            scan.FromAssemblies(assembly)
                .AddClasses(f => f.AssignableTo<IScoped>(), false)
                .UsingRegistrationStrategy(RegistrationStrategy.Throw)
                .AsMatchingInterface()
                .WithScopedLifetime());

    public static IServiceCollection AddSingletonAsMatchingInterfaces(
        this IServiceCollection services, Assembly assembly) =>
        services.Scan(scan =>
            scan.FromAssemblies(assembly)
                .AddClasses(f => f.AssignableTo<ISingleton>(), false)
                .UsingRegistrationStrategy(RegistrationStrategy.Throw)
                .AsMatchingInterface()
                .WithSingletonLifetime());
}
```

---

#### `src/Core/Core.Infrastructure/EventBus/EventBus.cs`
```csharp
using Core.Application.EventBus;
using Core.Application.ServiceLifetimes;
using Core.Domain.Events.Interfaces;
using MassTransit;

namespace Core.Infrastructure.EventBus;

public class EventBus(IBus bus, IPublishEndpoint publishEndpoint) : IEventBus, ITransient
{
    public Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken)
        where TEvent : class, IEvent
            => publishEndpoint.Publish(@event, @event.GetType(), cancellationToken);

    public Task SchedulePublishAsync<TEvent>(TEvent @event, DateTimeOffset scheduledTime, CancellationToken cancellationToken)
        where TEvent : class, IDelayedEvent
            => publishEndpoint.CreateMessageScheduler(bus.Topology)
                .SchedulePublish(scheduledTime.UtcDateTime, @event, cancellationToken);
}
```

---

### Core.Persistence

EventStore, UnitOfWork, Projection (MongoDB).

---

#### `src/Core/Core.Persistence/EventStore/EventStore.cs`
```csharp
using Core.Application.EventStore;
using Core.Application.ServiceLifetimes;
using Core.Domain.Events.Interfaces;
using Core.Domain.EventStore;
using Core.Domain.Primitives.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Core.Persistence.EventStore;

public class EventStore<TContext>(TContext dbContext) : IEventStore<TContext>, ITransient
    where TContext : DbContext
{
    public async Task AppendAsync<TAggregate>(StoreEvent<TAggregate> storeEvent, CancellationToken cancellationToken)
        where TAggregate : IAggregateRoot
    {
        await dbContext.Set<StoreEvent<TAggregate>>().AddAsync(storeEvent, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task AppendAsync<TAggregate>(Snapshot<TAggregate> snapshot, CancellationToken cancellationToken)
        where TAggregate : IAggregateRoot
    {
        await dbContext.Set<Snapshot<TAggregate>>().AddAsync(snapshot, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<List<IDomainEvent>> GetStreamAsync<TAggregate>(Guid id, ulong version, CancellationToken cancellationToken)
        where TAggregate : IAggregateRoot
            => dbContext.Set<StoreEvent<TAggregate>>()
                .AsNoTracking()
                .Where(e => e.AggregateId.Equals(id) && e.Version > version)
                .Select(e => e.Event)
                .ToListAsync(cancellationToken);

    public Task<Snapshot<TAggregate>?> GetSnapshotAsync<TAggregate>(Guid id, CancellationToken cancellationToken)
        where TAggregate : IAggregateRoot
            => dbContext.Set<Snapshot<TAggregate>>()
                .AsNoTracking()
                .Where(s => s.AggregateId.Equals(id))
                .OrderByDescending(s => s.Version)
                .FirstOrDefaultAsync(cancellationToken);

    public IAsyncEnumerable<Guid> StreamAggregatesId<TAggregate>()
        where TAggregate : IAggregateRoot
            => dbContext.Set<StoreEvent<TAggregate>>()
                .AsNoTracking()
                .Select(e => e.AggregateId)
                .Distinct()
                .AsAsyncEnumerable();

    public Task<Guid> StreamAggregateId<TAggregate>(Expression<Func<StoreEvent<TAggregate>, bool>> predicate, CancellationToken cancellationToken)
        where TAggregate : IAggregateRoot
            => dbContext.Set<StoreEvent<TAggregate>>()
                .AsNoTracking()
                .Where(predicate)
                .Select(e => e.AggregateId)
                .FirstOrDefaultAsync(cancellationToken);
}
```

---

#### `src/Core/Core.Persistence/UnitOfWork/UnitOfWork.cs`
```csharp
using Core.Application.ServiceLifetimes;
using Core.Application.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Core.Persistence.UnitOfWork;

public class UnitOfWork<TContext>(TContext dbContext) : IUnitOfWork<TContext>, IScoped
    where TContext : DbContext
{
    private readonly DatabaseFacade _database = dbContext.Database;

    public Task ExecuteAsync(Func<CancellationToken, Task> operationAsync, CancellationToken cancellationToken)
        => _database.CreateExecutionStrategy().ExecuteAsync(ct => ExecuteTransactionAsync(operationAsync, ct), cancellationToken);

    private async Task ExecuteTransactionAsync(Func<CancellationToken, Task> operationAsync, CancellationToken cancellationToken)
    {
        await using var transaction = await _database.BeginTransactionAsync(cancellationToken);
        try
        {
            await operationAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
```

---

#### `src/Core/Core.Persistence/Projection/Abstractions/IMongoDbContext.cs`
```csharp
using MongoDB.Driver;

namespace Core.Persistence.Projection.Abstractions;

public interface IMongoDbContext
{
    IMongoClient MongoClient { get; }
    string DatabaseName { get; }
    IMongoCollection<T> GetCollection<T>();
}
```

#### `src/Core/Core.Persistence/Projection/Abstractions/MongoDbContext.cs`
```csharp
using MongoDB.Driver;
using System.Security.Authentication;

namespace Core.Persistence.Projection.Abstractions;

public abstract class MongoDbContext : IMongoDbContext
{
    public readonly IMongoClient _mongoClient;
    public readonly IMongoDatabase _mongoDatabase;

    protected MongoDbContext(string connectionString)
    {
        MongoUrl mongoUrl = new(connectionString);
        MongoClientSettings settings = MongoClientSettings.FromUrl(mongoUrl);

        settings.SslSettings = new SslSettings
        {
            EnabledSslProtocols = SslProtocols.Tls13
        };

        _mongoClient = new MongoClient(settings);
        _mongoDatabase = _mongoClient.GetDatabase(mongoUrl.DatabaseName);
    }

    public IMongoClient MongoClient => _mongoClient;
    public string DatabaseName => _mongoDatabase.DatabaseNamespace.DatabaseName;

    // Usa o nome do tipo como nome da collection
    public IMongoCollection<T> GetCollection<T>()
        => _mongoDatabase.GetCollection<T>(typeof(T).Name);
}
```

---

#### `src/Core/Core.Persistence/Projection/Projection.cs`
```csharp
using Core.Domain.Primitives.Interfaces;
using Core.Persistence.Projection.Abstractions;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using System.Linq.Expressions;

namespace Core.Persistence.Projection;

public class Projection<TProjection>(IMongoDbContext context)
    where TProjection : IProjectionModel
{
    private readonly IMongoCollection<TProjection> _collection = context.GetCollection<TProjection>();

    public Task<TProjection?> GetByIdAsync<TId>(TId id, CancellationToken ct = default) where TId : struct
        => FindAsync(p => p.Id.Equals(id), ct);

    public Task<TProjection?> FindAsync(Expression<Func<TProjection, bool>> predicate, CancellationToken ct = default)
        => _collection.AsQueryable().Where(predicate).FirstOrDefaultAsync(ct);

    public Task<List<TProjection>> ListAsync(CancellationToken ct = default)
        => _collection.AsQueryable().ToListAsync(cancellationToken: ct);

    public Task<List<TProjection>> ListAsync(Expression<Func<TProjection, bool>> predicate, CancellationToken ct = default)
        => _collection.AsQueryable().Where(predicate).ToListAsync(cancellationToken: ct);

    public Task UpdateOneFieldAsync<TField>(
        Expression<Func<TProjection, bool>> filter,
        Expression<Func<TProjection, TField>> field,
        TField value,
        CancellationToken ct = default)
        => _collection.UpdateOneAsync(
            filter: filter,
            update: new ObjectUpdateDefinition<TProjection>(new()).Set(field, value),
            cancellationToken: ct);

    public Task UpdateOneFieldAsync<TField, TId>(
        TId id,
        Expression<Func<TProjection, TField>> field,
        TField value,
        CancellationToken ct = default) where TId : struct
        => _collection.UpdateOneAsync(
            filter: p => p.Id.Equals(id),
            update: new ObjectUpdateDefinition<TProjection>(new()).Set(field, value),
            cancellationToken: ct);

    public async Task UpdateFieldsAsync<TId>(TId id, Action<FieldUpdateBuilder<TProjection>> buildUpdates, CancellationToken ct = default)
        where TId : struct
    {
        var builder = new FieldUpdateBuilder<TProjection>();
        buildUpdates(builder);
        var updateDefinition = builder.Build();
        if (updateDefinition is not null)
            await _collection.UpdateOneAsync(p => p.Id.Equals(id), updateDefinition, cancellationToken: ct);
    }

    public ValueTask ReplaceInsertAsync(TProjection replacement, CancellationToken ct = default)
        => OnReplaceAsync(replacement, p => p.Id == replacement.Id, ct);

    public ValueTask ReplaceInsertAsync(TProjection replacement, Expression<Func<TProjection, bool>> filter, CancellationToken ct = default)
        => OnReplaceAsync(replacement, filter, ct);

    public Task DeleteAsync<TId>(TId id, CancellationToken ct = default) where TId : struct
        => _collection.DeleteOneAsync(p => p.Id.Equals(id), ct);

    public Task DeleteAsync(Expression<Func<TProjection, bool>> filter, CancellationToken ct = default)
        => _collection.DeleteManyAsync(filter, ct);

    private async ValueTask OnReplaceAsync(TProjection replacement, Expression<Func<TProjection, bool>> filter, CancellationToken ct = default)
        => await _collection.ReplaceOneAsync(filter, replacement, new ReplaceOptions { IsUpsert = true }, ct);

    public IMongoCollection<TProjection> GetCollection() => _collection;
}
```

> **Nota**: `FieldUpdateBuilder<TProjection>` é uma classe auxiliar que acumula `Set(x => x.Field, value)` e constrói um `UpdateDefinition<TProjection>` do MongoDB Driver.

---

#### `src/Core/Core.Persistence/Configurations/StoreEventConfiguration.cs`
```csharp
using Core.Domain.EventStore;
using Core.Domain.Primitives;
using Core.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Persistence.Configurations;

public abstract class StoreEventConfiguration<TAggregate> : IEntityTypeConfiguration<StoreEvent<TAggregate>>
    where TAggregate : AggregateRoot
{
    public void Configure(EntityTypeBuilder<StoreEvent<TAggregate>> builder)
    {
        builder.ToTable($"{typeof(TAggregate).Name}StoreEvents");
        builder.HasKey(e => new { e.Version, e.AggregateId });
        builder.Property(e => e.AggregateId).IsRequired();
        builder.Property(e => e.Event).IsRequired().HasConversion<EventConverter>();
        builder.Property(e => e.EventType).HasMaxLength(50).IsUnicode(false).IsRequired();
        builder.Property(e => e.Timestamp).IsRequired();
        builder.Property(e => e.Version).IsRequired();
    }
}
```

#### `src/Core/Core.Persistence/Converters/EventConverter.cs`
```csharp
using Core.Domain.Events.Interfaces;
using Core.Infrastructure.JsonConverters;
using JsonNet.ContractResolvers;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Newtonsoft.Json;

namespace Core.Persistence.Converters;

public class EventConverter() : ValueConverter<IDomainEvent?, string>(
    @event => JsonConvert.SerializeObject(@event, typeof(IDomainEvent), SerializerSettings()),
    jsonString => JsonConvert.DeserializeObject<IDomainEvent>(jsonString, DeserializerSettings()))
{
    private static JsonSerializerSettings SerializerSettings() => new()
    {
        TypeNameHandling = TypeNameHandling.Auto,
        Converters = { new DateOnlyJsonConverter() }
    };

    private static JsonSerializerSettings DeserializerSettings() => new()
    {
        TypeNameHandling = TypeNameHandling.Auto,
        ContractResolver = new PrivateSetterContractResolver(),
        Converters = { new DateOnlyJsonConverter() }
    };
}
```

---

#### `src/Core/Core.Persistence/PersistenceServiceInstaller.cs`
```csharp
using Core.Infrastructure.Configurations;
using Core.Infrastructure.Extensions;
using Core.Persistence.Options.Configurations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace Core.Persistence;

internal sealed class PersistenceServiceInstaller : IServiceInstaller
{
    public void Install(IServiceCollection services, IConfiguration configuration)
    {
        services
            .ConfigureOptions<ConnectionStringConfiguration>()
            .AddTransientAsMatchingInterfaces(AssemblyReference.Assembly);

        // MongoDB: serializa Guid como string
        BsonSerializer.RegisterSerializer(new GuidSerializer(BsonType.String));
    }
}
```

---

### Core.Endpoints

Extensões para `EndpointBase` (Ardalis) e mapeamento de `Result` → HTTP.

---

#### `src/Core/Core.Endpoints/Extensions/EndpointBaseExtensions.cs`
```csharp
using Ardalis.ApiEndpoints;
using Core.Shared.Errors;
using Core.Shared.Results;
using Core.Shared.Results.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Core.Endpoints.Extensions;

public static class EndpointBaseExtensions
{
    public static ActionResult HandleFailure(this EndpointBase endpoint, Result result) =>
        result switch
        {
            { IsSuccess: true } => throw new InvalidOperationException("Cannot handle a success result."),
            IValidationResult validationResult =>
                endpoint.BadRequest(CreateProblemDetails(
                    "Validation Error", StatusCodes.Status400BadRequest,
                    IValidationResult.ValidationError, validationResult.Errors)),
            var notFound when notFound.Error is NotFoundError =>
                endpoint.NotFound(CreateProblemDetails("Not Found", StatusCodes.Status404NotFound, notFound.Error)),
            var conflict when conflict.Error is ConflictError =>
                endpoint.Conflict(CreateProblemDetails("Conflict", StatusCodes.Status409Conflict, conflict.Error)),
            var bad =>
                endpoint.BadRequest(CreateProblemDetails("Bad Request", StatusCodes.Status400BadRequest, bad.Error))
        };

    private static ProblemDetails CreateProblemDetails(string title, int status, Error error, Error[]? errors = null) =>
        new()
        {
            Title = title,
            Type = error.Code,
            Detail = error.Message,
            Status = status,
            Extensions = { { nameof(errors), errors } }
        };
}
```

---

## MÓDULOS

Cada módulo segue estritamente as 5 camadas abaixo.
O módulo **Accountancy** é o template de referência.

---

### {Module}.Domain

#### `src/Modules/{Module}/{Module}.Domain/Aggregates/{Aggregate}.cs`

```csharp
// Padrão completo — exemplo Accountancy
using {Module}.Domain.Events;
using Core.Domain.Events.Interfaces;
using Core.Domain.Primitives;
using Core.Shared.Extensions;

namespace {Module}.Domain.Aggregates;

public class {Aggregate} : AggregateRoot
{
    // --- propriedades ---
    public string Field { get; private set; } = string.Empty;

    // --- factory ---
    public static {Aggregate} Create(string field)
    {
        {Aggregate} aggregate = new();
        aggregate.RaiseEvent<DomainEvents.{Aggregate}Created>(version => new(
            Id: aggregate.Id,
            Field: field,
            CreatedAt: aggregate.CreatedAt,
            Version: version));
        return aggregate;
    }

    // --- mutations ---
    public void Update(string field)
        => RaiseEvent<DomainEvents.{Aggregate}Updated>(version => new(Id, field, DateTimeOffset.UtcNow, version));

    public void Delete()
        => RaiseEvent<DomainEvents.{Aggregate}Deleted>(version => new(Id, version));

    // --- event application ---
    protected override void ApplyEvent(IDomainEvent @event) => When(@event as dynamic);

    private void When(DomainEvents.{Aggregate}Created @event) { Id = @event.Id; Field = @event.Field; }
    private void When(DomainEvents.{Aggregate}Updated @event) { Field = @event.Field; UpdatedAt = @event.UpdatedAt; }
    private void When(DomainEvents.{Aggregate}Deleted @event) => IsDeleted = true;
}
```

#### `src/Modules/{Module}/{Module}.Domain/Events/DomainEvents.cs`

```csharp
using Core.Domain.Events;
using Core.Domain.Events.Interfaces;

namespace {Module}.Domain.Events;

public static class DomainEvents
{
    public sealed record {Aggregate}Created(
        Guid Id,
        string Field,
        DateTimeOffset CreatedAt,
        ulong Version
    ) : Message, IDomainEvent;

    public sealed record {Aggregate}Updated(
        Guid Id,
        string Field,
        DateTimeOffset UpdatedAt,
        ulong Version
    ) : Message, IDomainEvent;

    public sealed record {Aggregate}Deleted(
        Guid Id,
        ulong Version
    ) : Message, IDomainEvent;
}
```

#### `src/Modules/{Module}/{Module}.Domain/Projection/ProjectionModel.cs`

```csharp
using Core.Domain.Primitives.Interfaces;

namespace {Module}.Domain.Projection;

public static class ProjectionModel
{
    public record {Aggregate}(
        Guid Id,
        string Field,
        DateTimeOffset CreatedAt,
        DateTimeOffset? UpdatedAt,
        bool IsDeleted = false
    ) : IProjectionModel { }
}
```

---

### {Module}.Shared

#### `src/Modules/{Module}/{Module}.Shared/Commands/Create{Aggregate}Command.cs`

```csharp
using Core.Application.Messaging.Commands;
using Core.Shared.Response;

namespace {Module}.Shared.Commands;

public sealed record Create{Aggregate}Command(
    string Field
) : ICommand<IdentifierResponse>;
```

#### `src/Modules/{Module}/{Module}.Shared/Queries/Get{Aggregate}ByIdQuery.cs`

```csharp
using Core.Application.Messaging.Queries;
using {Module}.Shared.Response;

namespace {Module}.Shared.Queries;

public sealed record Get{Aggregate}ByIdQuery(Guid Id) : IQuery<{Aggregate}DetailResponse>;
```

#### `src/Modules/{Module}/{Module}.Shared/Response/{Aggregate}DetailResponse.cs`

```csharp
namespace {Module}.Shared.Response;

public sealed record {Aggregate}DetailResponse(
    Guid Id,
    string Field,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt
);
```

---

### {Module}.Application

#### `src/Modules/{Module}/{Module}.Application/Services/I{Module}ApplicationService.cs`

```csharp
using Core.Application.Services.Interfaces;
using {Module}.Persistence.Context;

namespace {Module}.Application.Services;

public interface I{Module}ApplicationService : IApplicationService<{Module}DbContext> { }
```

#### `src/Modules/{Module}/{Module}.Application/UseCases/Commands/Create{Aggregate}Handler.cs`

```csharp
using {Module}.Application.Services;
using {Module}.Domain.Projection;
using {Module}.Persistence.Projection;
using {Module}.Shared.Commands;
using Core.Application.Messaging.Commands;
using Core.Shared.Response;
using Core.Shared.Results;

namespace {Module}.Application.UseCases.Commands;

using {Aggregate}Aggregate = {Module}.Domain.Aggregates.{Aggregate};

public class Create{Aggregate}Handler(
    I{Module}ApplicationService applicationService,
    I{Module}Projection<ProjectionModel.{Aggregate}> projection
) : ICommandHandler<Create{Aggregate}Command, IdentifierResponse>
{
    public async Task<Result<IdentifierResponse>> Handle(Create{Aggregate}Command request, CancellationToken ct)
    {
        // 1. validação de negócio (ex: unicidade)
        var existing = await projection.FindAsync(x => x.Field == request.Field, ct);
        if (existing is not null)
            return Result.Failure<IdentifierResponse>(new Core.Shared.Errors.ConflictError(
                new("FieldAlreadyExists", "A record with the same field already exists")));

        // 2. criar agregado
        var aggregate = {Aggregate}Aggregate.Create(request.Field);

        // 3. persistir eventos + publicar no bus
        await applicationService.AppendEventsAsync(aggregate, ct);

        return Result.Success(new IdentifierResponse(aggregate.Id));
    }
}
```

#### `src/Modules/{Module}/{Module}.Application/UseCases/Events/Projection{Aggregate}EventHandler.cs`

```csharp
using {Module}.Domain.Events;
using {Module}.Domain.Projection;
using {Module}.Persistence.Projection;
using Core.Application.EventBus;
using Microsoft.Extensions.Logging;

namespace {Module}.Application.UseCases.Events;

public interface IProjection{Aggregate}EventHandler :
    IEventHandler<DomainEvents.{Aggregate}Created>,
    IEventHandler<DomainEvents.{Aggregate}Updated>,
    IEventHandler<DomainEvents.{Aggregate}Deleted>;

public class Projection{Aggregate}EventHandler(
    I{Module}Projection<ProjectionModel.{Aggregate}> projection,
    ILogger<Projection{Aggregate}EventHandler> logger
) : IProjection{Aggregate}EventHandler
{
    public async Task Handle(DomainEvents.{Aggregate}Created @event, CancellationToken ct = default)
    {
        try
        {
            await projection.ReplaceInsertAsync(new(
                @event.Id, @event.Field, @event.CreatedAt, null), ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create projection: {Id}.", @event.Id);
            throw new InvalidOperationException($"Failed to create projection: {@event.Id}.", ex);
        }
    }

    public async Task Handle(DomainEvents.{Aggregate}Updated @event, CancellationToken ct = default)
    {
        try
        {
            await projection.UpdateFieldsAsync(@event.Id, builder => builder
                .Set(x => x.Field, @event.Field)
                .Set(x => x.UpdatedAt, @event.UpdatedAt), ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to update projection: {Id}.", @event.Id);
            throw;
        }
    }

    public async Task Handle(DomainEvents.{Aggregate}Deleted @event, CancellationToken ct = default)
    {
        try
        {
            await projection.UpdateOneFieldAsync(@event.Id, x => x.IsDeleted, true, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to delete projection: {Id}.", @event.Id);
            throw;
        }
    }
}
```

#### `src/Modules/{Module}/{Module}.Application/UseCases/Validators/Create{Aggregate}Validator.cs`

```csharp
using {Module}.Shared.Commands;
using FluentValidation;

namespace {Module}.Application.UseCases.Validators;

public sealed class Create{Aggregate}Validator : AbstractValidator<Create{Aggregate}Command>
{
    public Create{Aggregate}Validator()
    {
        RuleFor(x => x.Field)
            .NotEmpty().WithMessage("Field is required")
            .MaximumLength(150).WithMessage("Field cannot exceed 150 characters");
    }
}
```

---

### {Module}.Infrastructure

#### `src/Modules/{Module}/{Module}.Infrastructure/{Module}ModuleInstaller.cs`

```csharp
using Core.Infrastructure.Configurations;
using Core.Infrastructure.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace {Module}.Infrastructure;

public sealed class {Module}ModuleInstaller : IModuleInstaller
{
    public void Install(IServiceCollection services, IConfiguration configuration)
    {
        // Instala IServiceInstaller de Infrastructure
        services.InstallServicesFromAssemblies(configuration, AssemblyReference.Assembly);

        // Auto-registro por lifetime markers — Infrastructure
        services
            .AddTransientAsMatchingInterfaces(AssemblyReference.Assembly)
            .AddScopedAsMatchingInterfaces(AssemblyReference.Assembly);

        // Auto-registro por lifetime markers — Persistence
        services
            .AddTransientAsMatchingInterfaces(Persistence.AssemblyReference.Assembly)
            .AddScopedAsMatchingInterfaces(Persistence.AssemblyReference.Assembly);
    }
}
```

#### `src/Modules/{Module}/{Module}.Infrastructure/Services/{Module}ApplicationService.cs`

```csharp
using Core.Application.EventBus;
using Core.Application.EventStore;
using Core.Application.Services;
using Core.Application.UnitOfWork;
using {Module}.Application.Services;
using {Module}.Persistence.Context;

namespace {Module}.Infrastructure.Services;

public class {Module}ApplicationService(
    IEventStore<{Module}DbContext> eventStore,
    IEventBus eventBusGateway,
    IUnitOfWork<{Module}DbContext> unitOfWork)
    : ApplicationService<{Module}DbContext>(eventStore, eventBusGateway, unitOfWork),
      I{Module}ApplicationService
{
}
```

#### `src/Modules/{Module}/{Module}.Infrastructure/ServiceInstallers/PersistenceServiceInstaller.cs`

```csharp
using Core.Application.EventStore;
using Core.Application.UnitOfWork;
using Core.Infrastructure.Configurations;
using Core.Persistence.EventStore;
using Core.Persistence.Options;
using Core.Persistence.UnitOfWork;
using {Module}.Persistence.Constants;
using {Module}.Persistence.Context;
using {Module}.Persistence.Context.Interfaces;
using {Module}.Persistence.Projection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace {Module}.Infrastructure.ServiceInstallers;

internal sealed class PersistenceServiceInstaller : IServiceInstaller
{
    public void Install(IServiceCollection services, IConfiguration configuration)
    {
        // Projection generic repository
        services.AddScoped(typeof(I{Module}Projection<>), typeof({Module}Projection<>));

        // Event Store + Unit of Work
        services.AddScoped(typeof(IEventStore<{Module}DbContext>), typeof(EventStore<{Module}DbContext>));
        services.AddScoped(typeof(IUnitOfWork<{Module}DbContext>), typeof(UnitOfWork<{Module}DbContext>));

        // MongoDB projection context
        services.AddScoped<I{Module}ProjectionDbContext>(provider =>
        {
            var config = provider.GetRequiredService<IConfiguration>();
            string connStr = config.GetSection("Projections").GetValue<string>("{Module}")!;
            return new {Module}ProjectionDbContext(connStr);
        });

        // PostgreSQL event store context
        services.AddDbContext<{Module}DbContext>((provider, builder) =>
        {
            var connStr = provider.GetService<IOptions<ConnectionStringOptions>>()!.Value;
            builder.UseNpgsql(
                connectionString: connStr,
                o => o.WithMigrationHistoryTableInSchema(Schemas.{Module}));
        });
    }
}
```

---

### {Module}.Persistence

#### `src/Modules/{Module}/{Module}.Persistence/Context/{Module}DbContext.cs`

```csharp
using {Module}.Persistence.Constants;
using Microsoft.EntityFrameworkCore;

namespace {Module}.Persistence.Context;

public sealed class {Module}DbContext(DbContextOptions<{Module}DbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schemas.{Module});
        modelBuilder.ApplyConfigurationsFromAssembly(AssemblyReference.Assembly);
    }
}
```

#### `src/Modules/{Module}/{Module}.Persistence/Context/Interfaces/I{Module}ProjectionDbContext.cs`

```csharp
using Core.Persistence.Projection.Abstractions;

namespace {Module}.Persistence.Context.Interfaces;

public interface I{Module}ProjectionDbContext : IMongoDbContext { }
```

#### `src/Modules/{Module}/{Module}.Persistence/Context/{Module}ProjectionDbContext.cs`

```csharp
using Core.Persistence.Projection.Abstractions;
using {Module}.Persistence.Context.Interfaces;

namespace {Module}.Persistence.Context;

public class {Module}ProjectionDbContext(string connectionString)
    : MongoDbContext(connectionString), I{Module}ProjectionDbContext
{
}
```

#### `src/Modules/{Module}/{Module}.Persistence/Projection/I{Module}Projection.cs`

```csharp
using Core.Domain.Primitives.Interfaces;
using Core.Persistence.Projection;

namespace {Module}.Persistence.Projection;

public interface I{Module}Projection<TProjection> : IProjection<TProjection>
    where TProjection : IProjectionModel { }
```

#### `src/Modules/{Module}/{Module}.Persistence/Projection/{Module}Projection.cs`

```csharp
using Core.Domain.Primitives.Interfaces;
using Core.Persistence.Projection;
using {Module}.Persistence.Context.Interfaces;

namespace {Module}.Persistence.Projection;

public class {Module}Projection<TProjection>(I{Module}ProjectionDbContext context)
    : Projection<TProjection>(context), I{Module}Projection<TProjection>
    where TProjection : IProjectionModel
{
}
```

#### `src/Modules/{Module}/{Module}.Persistence/Constants/Schemas.cs`

```csharp
namespace {Module}.Persistence.Constants;

public static class Schemas
{
    public const string {Module} = "{module_lowercase}";
}
```

#### `src/Modules/{Module}/{Module}.Persistence/Configurations/StoreEventConfiguration.cs`

```csharp
using Core.Persistence.Configurations;
using {Module}.Domain.Aggregates;

namespace {Module}.Persistence.Configurations;

public sealed class {Aggregate}StoreEventConfiguration : StoreEventConfiguration<{Aggregate}> { }
```

---

## CAMADA WEB

### `src/Web/Program.cs`

```csharp
using Asp.Versioning;
using Core.Infrastructure.Extensions;
using DotNetEnv;
using Serilog;
using Serilog.Events;
using Web.Extensions;

if (File.Exists(".env")) Env.Load();
if (File.Exists(".env.local")) Env.Load(".env.local");

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, cfg) => cfg.ReadFrom.Configuration(ctx.Configuration));

builder.Services.AddHttpContextAccessor();
builder.Services.AddCorrelationId();

builder.Services
    .AddControllers()
    .AddApplicationPart(Web.AssemblyReference.Assembly);

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddHttpLogging(logging =>
    {
        logging.LoggingFields = Microsoft.AspNetCore.HttpLogging.HttpLoggingFields.All;
        logging.RequestBodyLogLimit = 4096;
        logging.ResponseBodyLogLimit = 4096;
    });
}

builder.Services
    .InstallServicesFromAssemblies(
        builder.Configuration,
        Web.AssemblyReference.Assembly,
        Core.Infrastructure.AssemblyReference.Assembly,
        Core.Persistence.AssemblyReference.Assembly)
    .InstallModulesFromAssemblies(
        builder.Configuration,
        {Module}.Infrastructure.AssemblyReference.Assembly);  // adicionar por módulo

builder.Services
    .AddApiVersioning(o =>
    {
        o.DefaultApiVersion = new ApiVersion(1, 0);
        o.AssumeDefaultVersionWhenUnspecified = true;
        o.ReportApiVersions = true;
        o.ApiVersionReader = ApiVersionReader.Combine(
            new UrlSegmentApiVersionReader(),
            new HeaderApiVersionReader("X-Api-Version"));
    })
    .AddApiExplorer(o =>
    {
        o.GroupNameFormat = "'v'VVV";
        o.SubstituteApiVersionInUrl = true;
    });

builder.Services.AddCors(o =>
    o.AddPolicy("AllowAll", p => p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

var app = builder.Build();

app.UseMiddlewares();

if (app.Environment.IsDevelopment())
{
    app.UseHttpLogging();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseSerilogRequestLogging(o =>
{
    o.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
    o.GetLevel = (_, _, _) => LogEventLevel.Information;
    o.EnrichDiagnosticContext = (diag, http) =>
    {
        diag.Set("RequestHost", http.Request.Host.Value);
        diag.Set("RequestScheme", http.Request.Scheme);
        diag.Set("UserAgent", http.Request.Headers["User-Agent"].FirstOrDefault() ?? "Unknown");
    };
});

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

try
{
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Unhandled exception during startup");
    await app.StopAsync();
}
finally
{
    await Log.CloseAndFlushAsync();
    await app.DisposeAsync();
}
```

---

### `src/Web/Extensions/MiddlewareExtensions.cs`

```csharp
using CorrelationId;
using Web.Middlewares;

namespace Web.Extensions;

public static class MiddlewareExtensions
{
    public static IApplicationBuilder UseMiddlewares(this IApplicationBuilder app)
        => app.UsePathBase("/api")
              .UseCorrelationId()
              .UseMiddleware<RequestTimestampMiddleware>()
              .UseMiddleware<ExceptionHandlingMiddleware>();
}
```

---

### `src/Web/Middlewares/ExceptionHandlingMiddleware.cs`

```csharp
using FluentValidation;
using System.Net;
using System.Text.Json;

namespace Web.Middlewares;

public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try { await next(context); }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var response = exception switch
        {
            ValidationException vex => (object)new
            {
                StatusCode = (int)HttpStatusCode.BadRequest,
                Message = "Validation failed",
                Errors = vex.Errors.Select(e => new { Field = e.PropertyName, Error = e.ErrorMessage })
            },
            UnauthorizedAccessException => new { StatusCode = (int)HttpStatusCode.Unauthorized, Message = "Unauthorized" },
            KeyNotFoundException => new { StatusCode = (int)HttpStatusCode.NotFound, Message = "Not found" },
            _ => new { StatusCode = (int)HttpStatusCode.InternalServerError, Message = "An error occurred" }
        };

        context.Response.StatusCode = (int)response.GetType().GetProperty("StatusCode")!.GetValue(response)!;
        await context.Response.WriteAsync(JsonSerializer.Serialize(response,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
    }
}
```

---

### `src/Web/Endpoints/Routes/{Module}Routes.cs`

```csharp
namespace Web.Endpoints.Routes;

public static class {Module}Routes
{
    private const string BaseUri = "v{version:apiVersion}/{module_path}";

    public const string Create{Aggregate} = BaseUri;
    public const string Update{Aggregate} = BaseUri + "/{aggregateId:guid}";
    public const string Delete{Aggregate} = BaseUri + "/{aggregateId:guid}";
    public const string GetAll{Aggregates} = BaseUri;
    public const string Get{Aggregate}ById = BaseUri + "/{aggregateId:guid}";
}
```

---

### `src/Web/Endpoints/Modules/{Module}/Create{Aggregate}Endpoint.cs`

```csharp
using {Module}.Shared.Commands;
using Ardalis.ApiEndpoints;
using Asp.Versioning;
using Core.Endpoints.Extensions;
using Core.Shared.Response;
using Core.Shared.Results;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Web.Endpoints.Modules.{Module}.Requests.Commands;
using Web.Endpoints.Routes;

namespace Web.Endpoints.Modules.{Module};

public sealed class Create{Aggregate}Endpoint(ISender sender) : EndpointBaseAsync
    .WithRequest<Create{Aggregate}Request>
    .WithActionResult<IdentifierResponse>
{
    [ApiVersion("1.0")]
    [HttpPost({Module}Routes.Create{Aggregate})]
    [ProducesResponseType(typeof(IdentifierResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [SwaggerOperation(
        Summary = "Criar {aggregate}",
        Description = "Cadastra um novo {aggregate} no sistema.",
        Tags = [Tags.{Module}])]
    public override async Task<ActionResult<IdentifierResponse>> HandleAsync(
        Create{Aggregate}Request request,
        CancellationToken cancellationToken)
        => await Result.Create(request)
            .Map(r => new Create{Aggregate}Command(r.Field))
            .Bind(cmd => sender.Send(cmd, cancellationToken))
            .Match(Ok, this.HandleFailure);
}
```

---

### `src/Web/ServiceInstallers/EventBus/EventBusServiceInstaller.cs`

```csharp
using Core.Infrastructure.Configurations;
using Core.Infrastructure.JsonConverters;
using MassTransit;
using Newtonsoft.Json;
using Web.ServiceInstallers.EventBus.Options;
using Web.ServiceInstallers.EventBus.Options.Configurations;
using Web.ServiceInstallers.EventBus.PipeObservers;
using Web.ServiceInstallers.EventBus.PipeFilters;
using Core.Domain.Events.Interfaces;
using Microsoft.Extensions.Options;

namespace Web.ServiceInstallers.EventBus;

internal sealed class EventBusServiceInstaller : IServiceInstaller
{
    public void Install(IServiceCollection services, IConfiguration configuration) =>
        services
            .ConfigureOptions<MassTransitHostOptionsConfiguration>()
            .ConfigureOptions<EventBusOptionsConfiguration>()
            .AddMassTransit(bus =>
            {
                bus.SetKebabCaseEndpointNameFormatter();
                bus.AddConsumers(AssemblyReference.Assembly);
                bus.AddPublishMessageScheduler();
                bus.AddHangfireConsumers();

                bus.UsingRabbitMq((ctx, cfg) =>
                {
                    var opts = ctx.GetRequiredService<IOptions<EventBusOptions>>().Value;
                    cfg.Host(opts.ConnectionString);

                    cfg.UsePublishMessageScheduler();
                    cfg.UseMessageRetry(r => r.Immediate(5));
                    cfg.UseDelayedRedelivery(r => r.Intervals(
                        TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10),
                        TimeSpan.FromSeconds(30), TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(5),
                        TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(30), TimeSpan.FromMinutes(60)));

                    cfg.UseNewtonsoftJsonSerializer();
                    cfg.ConfigureNewtonsoftJsonSerializer(s =>
                    {
                        s.Converters.Add(new TypeNameHandlingConverter(TypeNameHandling.Objects));
                        s.Converters.Add(new DateOnlyJsonConverter());
                        return s;
                    });

                    cfg.UsePublishFilter(typeof(TraceIdentifierFilter<>), ctx);
                    cfg.ConnectReceiveObserver(new LoggingReceiveObserver());
                    cfg.ConnectConsumeObserver(new LoggingConsumeObserver());
                    cfg.ConnectPublishObserver(new LoggingPublishObserver());
                    cfg.ConnectSendObserver(new LoggingSendObserver());

                    cfg.AddEventReceiveEndpointsFromAssemblies(ctx, AssemblyReference.Assembly);
                    cfg.ConfigureEndpoints(ctx);

                    cfg.ConfigurePublish(pipe => pipe.AddPipeSpecification(
                        new DelegatePipeSpecification<PublishContext<IEvent>>(
                            ctx => ctx.CorrelationId = ctx.InitiatorId)));
                });
            });
}
```

---

### `src/Web/ServiceInstallers/EventBus/Consumers/{Module}Module/Projection{Aggregate}Consumer.cs`

```csharp
using {Module}.Application.UseCases.Events;
using {Module}.Domain.Events;
using MassTransit;

namespace Web.ServiceInstallers.EventBus.Consumers.{Module}Module;

public class Projection{Aggregate}Consumer(IProjection{Aggregate}EventHandler eventHandler) :
    IConsumer<DomainEvents.{Aggregate}Created>,
    IConsumer<DomainEvents.{Aggregate}Updated>,
    IConsumer<DomainEvents.{Aggregate}Deleted>
{
    public Task Consume(ConsumeContext<DomainEvents.{Aggregate}Created> ctx)
        => eventHandler.Handle(ctx.Message, ctx.CancellationToken);

    public Task Consume(ConsumeContext<DomainEvents.{Aggregate}Updated> ctx)
        => eventHandler.Handle(ctx.Message, ctx.CancellationToken);

    public Task Consume(ConsumeContext<DomainEvents.{Aggregate}Deleted> ctx)
        => eventHandler.Handle(ctx.Message, ctx.CancellationToken);
}
```

---

### `src/Web/ServiceInstallers/BackgroundJobs/BackgroundJobsServiceInstaller.cs`

```csharp
using Core.Application.Services;
using Core.Application.Services.Interfaces;
using Core.Infrastructure.Configurations;
using Hangfire;
using Hangfire.PostgreSql;
using Newtonsoft.Json;

namespace Web.ServiceInstallers.BackgroundJobs;

internal sealed class BackgroundJobsServiceInstaller : IServiceInstaller
{
    public void Install(IServiceCollection services, IConfiguration configuration)
    {
        services.AddHangfire(x =>
        {
            x.SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
             .UseSimpleAssemblyNameTypeSerializer()
             .UseRecommendedSerializerSettings()
             .UsePostgreSqlStorage(opts =>
                 opts.UseNpgsqlConnection(configuration.GetConnectionString("Hangfire")),
             new PostgreSqlStorageOptions
             {
                 SchemaName = "hangfire",
                 PrepareSchemaIfNecessary = true,
                 QueuePollInterval = TimeSpan.FromSeconds(15),
             });

            x.UseSerializerSettings(new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All });
        });

        services.AddHangfireServer(opts =>
            opts.WorkerCount = Math.Min(Environment.ProcessorCount * 3, 10));

        services.AddScoped<IJobSchedulerService, JobSchedulerService>();
        services.AddScoped<MediatorHangfireBridge>();
    }
}
```

---

## FLUXO DE DADOS

### Comando (Write)

```
HTTP POST /api/v1/{module}
  → {Aggregate}Endpoint.HandleAsync()
    → Result.Create(request).Map(→ Command).Bind(→ sender.Send())
      → ValidationPipelineBehavior (FluentValidation)
        → Create{Aggregate}Handler.Handle()
          → projection.FindAsync() — validação de negócio
          → {Aggregate}.Create() — domain aggregate
          → applicationService.AppendEventsAsync()
            → UnitOfWork.ExecuteAsync (PostgreSQL transaction)
              → EventStore.AppendAsync(StoreEvent)      ← persiste evento
              → EventStore.AppendAsync(Snapshot) [v%10] ← snapshot automático
            → EventBus.PublishAsync({Aggregate}Created) ← publica no RabbitMQ
          → return Result.Success(IdentifierResponse)
  → HTTP 200 OK { id: guid }

RabbitMQ → Projection{Aggregate}Consumer.Consume()
  → Projection{Aggregate}EventHandler.Handle()
    → projection.ReplaceInsertAsync(...)  ← MongoDB atualizado
```

### Query (Read)

```
HTTP GET /api/v1/{module}/{id}
  → Get{Aggregate}ByIdEndpoint.HandleAsync()
    → Get{Aggregate}ByIdQuery (MediatR)
      → Get{Aggregate}ByIdHandler.Handle()
        → projection.GetByIdAsync(id)  ← lê do MongoDB
        → return Result.Success({Aggregate}DetailResponse)
  → HTTP 200 OK { ... }
```

---

## EMAIL (padrão de uso)

```csharp
// ❌ Errado — nunca injetar IEmailService diretamente em handlers de negócio
// ✅ Correto — agendar via Hangfire

// Em {Module}.Application/Emails/Send{Action}EmailCommand.cs
public sealed record Send{Action}EmailCommand(
    string ToEmail,   // apenas primitivos
    string ToName,
    Guid RelatedId
) : ICommand;

// Em {Module}.Application/Emails/Send{Action}EmailCommandHandler.cs
public class Send{Action}EmailCommandHandler(IEmailService emailService) : ICommandHandler<Send{Action}EmailCommand>
{
    public async Task<Result> Handle(Send{Action}EmailCommand request, CancellationToken ct)
    {
        var subject = "Assunto do email";
        var body = $"<h1>Body HTML</h1><p>Olá {request.ToName}</p>";
        await emailService.SendAsync(request.ToEmail, request.ToName, subject, body, ct);
        return Result.Success();
    }
}

// Em qualquer handler de negócio — enfileirar o envio
public class SomeBusinessHandler(IJobSchedulerService jobScheduler) : ICommandHandler<SomeCommand>
{
    public Task<Result> Handle(SomeCommand request, CancellationToken ct)
    {
        jobScheduler.Enqueue(new Send{Action}EmailCommand(request.Email, request.Name, request.Id));
        return Task.FromResult(Result.Success());
    }
}
```

---

## VARIÁVEIS DE AMBIENTE (.env.local)

```env
ASPNETCORE_ENVIRONMENT=Development
ConnectionStrings__Default=Server=societiza-postgres-dev;Port=5432;Database=societiza;Username=societiza;Password=societiza
ConnectionStrings__Hangfire=Server=societiza-postgres-dev;Port=5432;Database=societiza;Username=societiza;Password=societiza
Projections__{Module}=mongodb://societiza:societiza@societiza-mongodb-dev:27017/societiza
EventBusOptions__ConnectionString=amqp://guest:guest@societiza-rabbitmq-dev:5672
EmailOptions__Username=seu_login_brevo
EmailOptions__Password=sua_smtp_key_brevo   # nunca commitar
EmailOptions__FromEmail=seuemail@gmail.com
```

---

## DOCKER COMPOSE (Infraestrutura local)

```yaml
# Docker/docker-compose.Infrastructure.Development.yaml
version: "3.8"

networks:
  societiza-dev:
    driver: bridge

services:
  societiza-postgres-dev:
    image: postgres:latest
    environment:
      POSTGRES_USER: societiza
      POSTGRES_PASSWORD: societiza
      POSTGRES_DB: societiza
    ports: ["5432:5432"]
    networks: [societiza-dev]
    deploy:
      resources:
        limits:
          memory: 512M

  societiza-mongodb-dev:
    image: mongo:latest
    environment:
      MONGO_INITDB_ROOT_USERNAME: societiza
      MONGO_INITDB_ROOT_PASSWORD: societiza
    ports: ["27017:27017"]
    networks: [societiza-dev]
    deploy:
      resources:
        limits:
          memory: 512M

  societiza-rabbitmq-dev:
    image: rabbitmq:management
    environment:
      RABBITMQ_DEFAULT_USER: guest
      RABBITMQ_DEFAULT_PASS: guest
    ports:
      - "5672:5672"
      - "15672:15672"
    networks: [societiza-dev]
    deploy:
      resources:
        limits:
          memory: 512M
```

---

## CHECKLIST — ADICIONAR NOVO MÓDULO

```
[ ] 1. Criar {Module}.Domain — Entity, AggregateRoot, DomainEvents, ProjectionModel
[ ] 2. Criar {Module}.Shared — Commands, Queries, Responses
[ ] 3. Criar {Module}.Application — Handlers, Validators, EventHandlers, IApplicationService
[ ] 4. Criar {Module}.Infrastructure — ModuleInstaller, ApplicationService, ServiceInstallers
[ ] 5. Criar {Module}.Persistence — DbContext, ProjectionDbContext, Projection, Configurations, Migrations
[ ] 6. Web/Endpoints — Routes, Endpoint classes, Request DTOs
[ ] 7. Web/ServiceInstallers/EventBus/Consumers — Consumer class para cada aggregate
[ ] 8. Program.cs — adicionar {Module}.Infrastructure.AssemblyReference.Assembly em InstallModulesFromAssemblies
[ ] 9. .env.local — adicionar Projections__{Module}=mongodb://...
[ ] 10. Rodar: dotnet ef migrations add Init --project src/Modules/{Module}/{Module}.Persistence --startup-project src/Web
```

---

## PACOTES NUGET PRINCIPAIS

| Pacote | Uso |
|---|---|
| `MediatR` | CQRS dispatcher |
| `FluentValidation.AspNetCore` | Validação de commands |
| `Ardalis.ApiEndpoints` | Pattern endpoint-per-class |
| `MassTransit.RabbitMQ` | Event Bus |
| `Hangfire.PostgreSql` | Background jobs |
| `Microsoft.EntityFrameworkCore` + `Npgsql.EFCore.PostgreSQL` | Event store (PostgreSQL) |
| `MongoDB.Driver` | Projections (read models) |
| `Scrutor` | Auto-registro por Scrutor scan |
| `Asp.Versioning.Mvc` | API versioning |
| `Serilog.AspNetCore` | Logging estruturado |
| `CorrelationId` | Correlation ID middleware |
| `DotNetEnv` | Leitura de `.env` files |
| `Swashbuckle.AspNetCore` | Swagger/OpenAPI |
| `Newtonsoft.Json` | Serialização TypeNameHandling (eventos) |
| `JsonNet.ContractResolvers` | PrivateSetterContractResolver p/ deserialização |
