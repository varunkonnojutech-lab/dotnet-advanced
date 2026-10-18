namespace BankAccountDemo;

// A point-in-time copy of the aggregate used to speed up rehydration:
// instead of replaying from event #1, Load() can start from the snapshot.
public sealed record BankAccountSnapshot(Guid AccountId, string Owner, decimal Balance, int Version);

// The aggregate root. All state changes go through Apply(IEvent),
// so the current state is always derivable from the event stream.
public sealed class BankAccount
{
    public Guid AccountId { get; private set; }
    public string Owner { get; private set; } = string.Empty;
    public decimal Balance { get; private set; }
    public int Version { get; private set; }
    public bool IsOpen { get; private set; }

    private BankAccount() { }

    internal static BankAccount FromSnapshot(BankAccountSnapshot s)
    {
        return new BankAccount
        {
            AccountId = s.AccountId,
            Owner = s.Owner,
            Balance = s.Balance,
            Version = s.Version,
            IsOpen = true
        };
    }

    public static BankAccount Create(Guid id, string owner)
    {
        var account = new BankAccount();
        account.Apply(new AccountOpened(id, owner, DateTimeOffset.UtcNow));
        return account;
    }

    public void Deposit(decimal amount)
    {
        if (amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount), "Deposit amount must be positive.");
        if (!IsOpen)
            throw new InvalidOperationException("Cannot deposit into a closed account.");
        Apply(new Deposited(AccountId, amount, DateTimeOffset.UtcNow));
    }

    public void Withdraw(decimal amount)
    {
        if (amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount), "Withdrawal amount must be positive.");
        if (!IsOpen)
            throw new InvalidOperationException("Cannot withdraw from a closed account.");
        if (amount > Balance)
            throw new InvalidOperationException("Insufficient funds.");
        Apply(new Withdrawn(AccountId, amount, DateTimeOffset.UtcNow));
    }

    // The single mutation point: every state change is an event application.
    public void Apply(IEvent e)
    {
        switch (e)
        {
            case AccountOpened opened:
                AccountId = opened.AccountId;
                Owner = opened.Owner;
                IsOpen = true;
                break;
            case Deposited deposited:
                Balance += deposited.Amount;
                break;
            case Withdrawn withdrawn:
                Balance -= withdrawn.Amount;
                break;
            default:
                throw new ArgumentException($"Unknown event type: {e.GetType().Name}", nameof(e));
        }
        Version++;
    }

    // Rebuild an aggregate purely from its event history.
    public static BankAccount Replay(IEnumerable<IEvent> events)
    {
        var account = new BankAccount();
        foreach (var e in events)
            account.Apply(e);
        return account;
    }

    public BankAccountSnapshot TakeSnapshot() => new(AccountId, Owner, Balance, Version);
}
