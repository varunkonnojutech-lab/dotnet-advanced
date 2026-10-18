# .NET Advanced

[![Build](https://github.com/varunkonnojutech-lab/dotnet-advanced/actions/workflows/ci.yml/badge.svg)](https://github.com/varunkonnojutech-lab/dotnet-advanced/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4)](https://dotnet.microsoft.com/download/dotnet/8.0)
[![Open in GitHub Codespaces](https://github.com/codespaces/badge.svg)](https://codespaces.new/varunkonnojutech-lab/dotnet-advanced)

Beyond-senior .NET 8 demos — one small console project per concept, each showing *why* the technique matters in real systems, not just how it compiles.

## Who is this for

Developers who are comfortable building .NET apps and want to understand the techniques used inside high-scale, high-reliability systems: event sourcing, zero-allocation parsing, compile-time code generation, and native AOT. If the earlier repos in this learning path felt easy, this one is for you.

## What you'll learn

- **Event sourcing** — storing history instead of state (audit trails, time travel)
- **Zero-allocation performance** — `Span<T>`, `stackalloc`, `ArrayPool<T>`, `System.IO.Pipelines`
- **Source generators** — compile-time logging with `[LoggerMessage]`
- **Native AOT** — single-file native binaries with JSON source generation
- **Benchmarking** — proving performance claims with BenchmarkDotNet

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (`dotnet --version` should report 8.x)

No account, no cloud, no database — everything runs locally.

## Quick start

```bash
git clone https://github.com/varunkonnojutech-lab/dotnet-advanced.git
cd dotnet-advanced
dotnet run --project 01-EventSourcing/BankAccount
```

Or skip setup entirely: click the **Codespaces** badge above to open a ready-to-code environment in your browser with the .NET 8 SDK preinstalled.

Each demo is independent — run them in any order with `dotnet run --project <path>`.

## How it works

### 01 — Event Sourcing (`01-EventSourcing/BankAccount`)

**Concept:** instead of storing current state, store the *history* of state-changing events (`AccountOpened`, `Deposited`, `Withdrawn`) in an append-only stream. Current state is rebuilt by replaying events.

**Why it matters:** you get a full audit trail for free (critical in banking/finance), time-travel debugging ("what did this account look like last Tuesday?"), and the event stream doubles as integration events for other services. Optimistic concurrency via expected-version checks prevents lost updates.

**Run:** `dotnet run --project 01-EventSourcing/BankAccount`

The demo builds a 4-event stream, takes a snapshot every 3 events, then "restarts" and rebuilds the aggregate from the store — the restored balance matches the live one.

### 02 — High Performance: Span\<T> (`02-HighPerformance/SpanDemo`)

**Concept:** `Span<T>` / `ReadOnlySpan<T>` are ref structs that point at contiguous memory (arrays, stack, or native) without owning it — slicing and parsing with zero heap allocations. `stackalloc` carves memory from the stack; `ArrayPool<T>` rents reusable buffers instead of allocating.

**Why it matters:** in hot paths (parsing, networking, serialization), allocations become GC pressure, and GC pauses become tail latency. Zero-allocation parsing is the difference between a service that hums at p99 and one that stutters.

**Run:** `dotnet run --project 02-HighPerformance/SpanDemo`

Parses 1,000,000 CSV lines two ways — `ReadOnlySpan<char>` slicing vs `string.Split` — and prints the elapsed ms for each so you can see the gap yourself. Also demos `stackalloc` and `ArrayPool<byte>`.

### 02 — High Performance: System.IO.Pipelines (`02-HighPerformance/PipelinesDemo`)

**Concept:** `System.IO.Pipelines` separates "reading bytes from the network" from "parsing messages": a `PipeWriter` is filled from the socket while a `PipeReader` consumes complete segments, with backpressure and buffer pooling handled for you.

**Why it matters:** it's the engine behind Kestrel. If you ever write a TCP server, a protocol parser, or a high-throughput ingestion path, Pipelines is how you avoid the classic buffer-management bugs (partial reads, oversized buffers, pinned memory).

**Run:** `dotnet run --project 02-HighPerformance/PipelinesDemo`

Spins up a TCP echo server on `127.0.0.1:5005` using a `Pipe`, then a client sends `hello` / `pipelines` / `echo` and prints the echoes.

### 03 — Source Generators: LoggerMessage (`03-SourceGenerators/LoggerMessageDemo`)

**Concept:** the `[LoggerMessage]` attribute triggers a *built-in* Roslyn source generator that emits the logging method at compile time — strongly typed, no boxing of value types, no `params object[]` allocations, no runtime parsing of the message template.

**Why it matters:** logging sits in every hot path. Source-generated logging keeps the ergonomics of `logger.LogInformation(...)` while removing its hidden allocations — and template/parameter mismatches become compile-time errors instead of runtime surprises.

**Run:** `dotnet run --project 03-SourceGenerators/LoggerMessageDemo`

Defines a minimal `ILogger`/`ILoggerProvider` that writes to the console, then calls two `[LoggerMessage]`-generated methods. No extra NuGet package needed — the generator ships with the .NET 8 SDK.

### 04 — Native AOT (`04-AotConsole`)

**Concept:** Native AOT compiles the app (and the runtime) ahead of time into a single native binary — no JIT, no IL to ship. Combined with System.Text.Json *source generation* (`JsonSerializerContext`), serialization works without runtime reflection, which AOT forbids.

**Why it matters:** fast cold starts, tiny containers, no JIT warmup — the reason AOT is the default conversation for AWS Lambda / Azure Functions and CLI tools. The tradeoff: no runtime reflection or dynamic codegen, so serialization shapes must be described up front.

**Run:** `dotnet run --project 04-AotConsole`

**Publish as a native binary:** `dotnet publish 04-AotConsole -c Release` (produces a single self-contained executable; AOT compilation only happens on publish, not on `dotnet run`)

Serializes/deserializes a list of orders purely through the generated `OrderJsonContext`.

### 05 — Benchmarks (`05-Benchmarks`)

**Concept:** BenchmarkDotNet runs your code through warmup, multiple iterations, and statistical analysis (mean, error, memory diagnostics) so microbenchmarks actually mean something.

**Why it matters:** "I think X is faster" is how performance regressions ship. A baseline-anchored benchmark with `[MemoryDiagnoser]` turns hallway debates into numbers — allocations included.

**Run (Release!):** `dotnet run --project 05-Benchmarks -c Release`

Compares `+=` vs `StringBuilder` vs `string.Create` for concatenating 100 strings, with `+=` as the baseline.

## Project structure

```
dotnet-advanced/
├── 01-EventSourcing/BankAccount/          # Event-sourced aggregate + in-memory store + snapshots
├── 02-HighPerformance/
│   ├── SpanDemo/                          # Zero-alloc parsing, stackalloc, ArrayPool
│   └── PipelinesDemo/                     # System.IO.Pipelines TCP echo server
├── 03-SourceGenerators/LoggerMessageDemo/ # Compile-time generated logging
├── 04-AotConsole/                         # Native AOT + JSON source generation
├── 05-Benchmarks/                         # BenchmarkDotNet shootout (run in Release)
├── DotNetAdvanced.sln
└── .devcontainer/                         # One-click Codespaces setup
```

## Notes

- `PublishAot` (04-AotConsole): AOT compilation only happens on `dotnet publish -c Release`, not `dotnet run`.
- Benchmarks must run in Release (`-c Release`); Debug numbers are meaningless.
- BenchmarkDotNet restores its NuGet package on first build — needs network access at restore time.

## Next steps

You've reached the end of the learning path — from your first console app to event sourcing and native AOT. 🎉

- Revisit any repo and extend it: add a real database to the web API starter, deploy the microservices to AKS, publish the AOT console as a CLI tool.
- Found a bug or have a better demo? [Open an issue](https://github.com/varunkonnojutech-lab/dotnet-advanced/issues) or a pull request — contributions are welcome.

## License

MIT — see [LICENSE](LICENSE).
