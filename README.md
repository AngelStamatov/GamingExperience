# Betty Wallet

A small C#/.NET 8 application with wallet deposits, withdrawals, and an independent
slot game in Core. The console currently prints only the application name;
interactive commands and application wiring will be added later.

## Requirements

- .NET 8 SDK, or a newer SDK capable of targeting `net8.0`.
- .NET 8 runtime to run the console application and tests.
- Access to NuGet for the first restore of targeting packs and test dependencies.

## Structure

```text
Betty.Wallet.sln
src/
  Betty.Wallet.Console/  Console entry point; references Core.
  Betty.Wallet.Core/     Wallet, SlotGame, RoundResult, and IRandomSource.
tests/
  Betty.Wallet.Tests/    Deterministic xUnit tests; references Core.
```

The console interface and business logic live in separate projects. Core has no
external package dependencies. Tests use xUnit, its Visual Studio adapter, and
the .NET test SDK.

## Commands

Run these commands from the repository root. Build and test restore dependencies
automatically.

```sh
dotnet build Betty.Wallet.sln
dotnet run --project src/Betty.Wallet.Console/Betty.Wallet.Console.csproj
dotnet test Betty.Wallet.sln
```

Tests cover business rules and boundary cases. Slot tests use a scripted fake
`IRandomSource` that checks requested ranges; they use no real randomness or
statistical assertions.

## Assumptions and unspecified rules

Wallet starts at zero. Deposits and withdrawals must be positive, and withdrawals
cannot exceed the balance. All monetary values use `decimal`.

Slot game rules and explicit assumptions:

- Bets from 1 to 10 inclusive are accepted. Invalid bets are rejected before any
  random draw. Bets may have more than two decimal places; only payout is rounded.
- Each round selects a category with `Next(0, 100)`. The upper bound of every
  `IRandomSource.Next` call is exclusive.

| Category draw | Outcome | Multiplier |
| --- | --- | --- |
| 0-49 | Loss | 0; no multiplier draw |
| 50-89 | Small win | `Next(1, 201) / 100m`, from 0.01 to 2.00 |
| 90-99 | Large win | `Next(201, 1001) / 100m`, from 2.01 to 10.00 |

- Payout is `betAmount * multiplier`, rounded to two decimal places using
  `MidpointRounding.AwayFromZero`. It is the total returned amount, including the
  stake, rather than net profit; the stake is not added a second time.
- `RoundResult` is immutable and contains `BetAmount`, `Multiplier`, and `Payout`.
- `SlotGame` receives `IRandomSource` through its constructor and has no dependency
  on Wallet. A concrete random source and wallet/console integration are deferred.

Currency remains unspecified. Record any further assumptions here before adding
rules. Keep the architecture simple and dependencies minimal.
