namespace BankAccountDemo;

// Marker interface for every domain event. Events are immutable facts:
// they describe something that already happened, so they are records.
public interface IEvent { }

public sealed record AccountOpened(Guid AccountId, string Owner, DateTimeOffset OpenedAt) : IEvent;

public sealed record Deposited(Guid AccountId, decimal Amount, DateTimeOffset At) : IEvent;

public sealed record Withdrawn(Guid AccountId, decimal Amount, DateTimeOffset At) : IEvent;
