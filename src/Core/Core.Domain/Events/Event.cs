using Core.Domain.Events.Interfaces;

namespace Core.Domain.Events;

public abstract record Event : Message, IEvent;