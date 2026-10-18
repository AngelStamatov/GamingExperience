# Betty Wallet

A small C#/.NET 8 console application intended to support deposits, withdrawals,
and a slot game. This initial skeleton only prints the application name; business
rules and tests will be added in later changes.

## Requirements

- .NET 8 SDK, or a newer SDK capable of targeting `net8.0`.
- .NET 8 runtime to run the console application and tests.
- Access to NuGet for the first restore of targeting packs and test dependencies.

## Structure

```text
Betty.Wallet.sln
src/
  Betty.Wallet.Console/  Console entry point; references Core.
  Betty.Wallet.Core/     Business logic library; currently empty.
tests/
  Betty.Wallet.Tests/    xUnit test project; references Core; currently empty.
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

No tests are defined yet, so `dotnet test` is expected to report that no tests
were found. Add real business rule and boundary case tests as behavior is
implemented.

## Assumptions and unspecified rules

This stage establishes project structure only. Currency, starting balance,
deposit and withdrawal limits, monetary precision and rounding, bet limits,
slot probabilities, and payouts remain unspecified; no defaults are implemented.
Record any chosen assumptions here before implementing those rules.

Future monetary values must use `decimal`. Random number generation must be
injected through an abstraction to allow deterministic tests. Keep the architecture
simple and dependencies minimal.
