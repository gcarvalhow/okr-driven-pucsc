using MediatR;

namespace Core.Application.Services.Interfaces;

public interface IJobSchedulerService
{
    void Enqueue(string jobName, IRequest request);
    void Enqueue(IRequest request);
    void Enqueue<T>(IRequest<T> request);
    void Schedule(string jobName, TimeSpan scheduleAt, IRequest request);
    void Schedule<T>(string jobName, TimeSpan scheduleAt, IRequest<T> request);
    void Schedule(string jobName, DateTimeOffset scheduleAt, IRequest request);
    void Schedule<T>(string jobName, DateTimeOffset scheduleAt, IRequest<T> request);
    void ScheduleRecurring<T>(string jobName, string cronExpression, IRequest<T> request);
}