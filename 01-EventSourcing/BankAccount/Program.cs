using BankAccountDemo;

var store = new InMemoryEventStore();
var accountId = Guid.NewGuid();

// --- Write side: build an event stream, applying each event to the aggregate ---
var events = new IEvent[]
{
    new AccountOpened(accountId, "Varun Kumar Konnoju", DateTimeOffset.UtcNow),
    new Deposited(accountId, 1000m, DateTimeOffset.UtcNow),
    new Withdrawn(accountId, 250m, DateTimeOffset.UtcNow),
    new Deposited(accountId, 500m, DateTimeOffset.UtcNow),
};

var account = BankAccount.Replay(Array.Empty<IEvent>());
int applied = 0;
foreach (var e in events)
{
    account.Apply(e);          // mutate aggregate state
    store.Append(accountId, e); // persist the fact
    if (++applied % 3 == 0)
        store.SaveSnapshot(account.TakeSnapshot()); // snapshot every 3 events
}

Console.WriteLine($"Balance after events: {account.Balance} (expected 1250)");
Console.WriteLine($"Aggregate version:    {account.Version}");

// --- The rich behavior API wraps Apply with guards (shown on a scratch account) ---
var scratch = BankAccount.Create(Guid.NewGuid(), "Scratch");
scratch.Deposit(100m);
scratch.Withdraw(40m);
Console.WriteLine($"Scratch account: deposited 100, withdrew 40 -> balance {scratch.Balance}");
try
{
    scratch.Withdraw(10_000m);
}
catch (InvalidOperationException ex)
{
    Console.WriteLine($"Guard works (insufficient funds): {ex.Message}");
}

// --- Optimistic concurrency on append ---
try
{
    store.Append(accountId, new Deposited(accountId, 1m, DateTimeOffset.UtcNow), expectedVersion: 99);
}
catch (InvalidOperationException ex)
{
    Console.WriteLine($"Concurrency guard works: {ex.Message}");
}

// --- Simulate a restart: rebuild the aggregate from the store ---
var restored = store.Load(accountId);
Console.WriteLine();
Console.WriteLine($"Restored balance: {restored.Balance} (matches live aggregate: {restored.Balance == account.Balance})");
Console.WriteLine($"Restored version: {restored.Version}");
Console.WriteLine($"Events in stream: {store.GetStream(accountId).Count}");
Console.WriteLine($"Snapshot present: {store.TryGetSnapshot(accountId, out _)}");
