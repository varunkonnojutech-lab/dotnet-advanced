namespace BankAccountDemo;

// Append-only, in-memory event store. A real system would persist these
// streams to EventStoreDB, Marten, or a SQL outbox table — the API shape
// (append with expected version, read stream, snapshots) stays the same.
public sealed class InMemoryEventStore
{
    private readonly Dictionary<Guid, List<IEvent>> _streams = new();
    private readonly Dictionary<Guid, BankAccountSnapshot> _snapshots = new();

    public void Append(Guid streamId, IEvent e)
    {
        if (!_streams.TryGetValue(streamId, out var list))
        {
            list = new List<IEvent>();
            _streams[streamId] = list;
        }
        list.Add(e);
    }

    // Optimistic concurrency: the writer must know the version it read.
    // A mismatch means someone else appended first — retry instead of
    // silently overwriting their events.
    public void Append(Guid streamId, IEvent e, int expectedVersion)
    {
        int current = GetStream(streamId).Count;
        if (current != expectedVersion)
            throw new InvalidOperationException(
                $"Concurrency conflict: expected version {expectedVersion}, but stream is at {current}.");
        Append(streamId, e);
    }

    public IReadOnlyList<IEvent> GetStream(Guid streamId) =>
        _streams.TryGetValue(streamId, out var list)
            ? list
            : (IReadOnlyList<IEvent>)Array.Empty<IEvent>();

    public void SaveSnapshot(BankAccountSnapshot s) => _snapshots[s.AccountId] = s;

    public bool TryGetSnapshot(Guid id, out BankAccountSnapshot? s) =>
        _snapshots.TryGetValue(id, out s);

    // Rehydrate an aggregate. If a snapshot exists, only the events *after*
    // the snapshot's version are replayed. Once enough new events accumulate
    // (snapshotEvery), a fresh snapshot is taken opportunistically.
    public BankAccount Load(Guid accountId, int snapshotEvery = 5)
    {
        TryGetSnapshot(accountId, out var snapshot);
        int fromVersion = snapshot?.Version ?? 0;

        BankAccount account = snapshot is null
            ? BankAccount.Replay(Array.Empty<IEvent>())
            : BankAccount.FromSnapshot(snapshot);

        var events = GetStream(accountId);
        for (int i = fromVersion; i < events.Count; i++)
            account.Apply(events[i]);

        int replayed = account.Version - fromVersion;
        if (snapshotEvery > 0 && replayed >= snapshotEvery)
            SaveSnapshot(account.TakeSnapshot());

        return account;
    }
}
