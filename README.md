# Betty Wallet

A small C#/.NET 8 console application for wallet deposits, withdrawals, and slot
bets. Business rules live in Core; the console parses commands and displays
results through WalletService.

The balance is stored only in memory for the current process and is not saved
between sessions. Each new application run starts with a balance of `$0.00`.

## Requirements

- .NET 8 SDK, or a newer SDK capable of targeting `net8.0`.
- .NET 8 runtime to run the console application and tests.
- Access to NuGet for the first restore of targeting packs and test dependencies.

## Structure

```text
Betty.Wallet.sln
src/
  Betty.Wallet.Console/  Command parser, command loop, and random source; references Core.
  Betty.Wallet.Core/     WalletService, Wallet, SlotGame, RoundResult, and IRandomSource.
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
statistical assertions. Console tests use StringReader/StringWriter, a real
WalletService and SlotGame, and the same deterministic fake random source.

## Console usage

Start the application from the repository root:

```sh
dotnet run --project src/Betty.Wallet.Console/Betty.Wallet.Console.csproj
```

Commands are case insensitive and allow extra spaces or tabs:

- `deposit 20` adds funds.
- `withdraw 5` removes funds if the balance is sufficient.
- `bet 1.50` places a bet from 1 to 10 inclusive and shows the gross payout.
- `exit` ends the session. End of input also ends the session normally.

Input assumption: amounts use `InvariantCulture`, with a decimal point and at
most two digits after it. Thousands separators, currency symbols, and exponents
are rejected. More than two fractional digits, including trailing zeros such as
`1.000`, are rejected without rounding. Values that cannot be represented exactly
as `decimal` are also rejected. An optional leading sign is parsed, but business
rules still require positive deposits, withdrawals, and bets.

Money is displayed with `$`, a decimal point, and exactly two decimal places,
independently of the system culture. Invalid input, insufficient funds, and
overflow show an error and the current balance; the session continues.

One possible session (slot payouts vary):

```text
Betty Wallet
Commands: deposit <amount>, withdraw <amount>, bet <amount>, exit.
Use a decimal point and at most 2 decimal places. Bets: $1.00 to $10.00.
Balance: $0.00
> deposit 20
Deposited $20.00. Balance: $20.00
> bet 1.50
Bet: $1.50. Gross payout: $1.52. Balance: $20.02
> withdraw 5
Withdrew $5.00. Balance: $15.02
> exit
Goodbye.
```

## Assumptions and unspecified rules

Wallet starts at zero. Deposits and withdrawals must be positive, and withdrawals
cannot exceed the balance. All monetary values use `decimal`.

Slot game rules and explicit assumptions:

- Bets from 1 to 10 inclusive are accepted. Invalid bets are rejected before any
  random draw. Core accepts bets with more than two decimal places and rounds only
  payout; console input is restricted to two fractional digits as documented above.
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
  on Wallet. The console supplies `SystemRandomSource`, which uses `Random.Shared.Next`.

`WalletService` connects the supplied Wallet and SlotGame synchronously. It exposes
the current balance and delegates deposits and withdrawals to Wallet. `PlaceBet`
validates the 1-10 bet range first, then available funds, before any random draw.
It obtains a complete game result before applying it with `Wallet.ApplyRound`.

`ApplyRound` requires a positive, funded bet and a nonnegative payout, including
zero. It calculates `Balance - betAmount + payout` in a local variable before
assigning the balance; it does not perform separate withdrawal and deposit calls.
Invalid arguments, insufficient funds, random source errors, and decimal overflow
leave the wallet balance unchanged. Decimal overflow propagates as
`OverflowException`. No concurrency coordination is provided.

The console displays amounts as dollars. Record any further assumptions here
before adding rules. Keep the architecture simple and dependencies minimal.
