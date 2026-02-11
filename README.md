# OrderSystem Problem Statement

//  PROBLEM: Implement the IOrder interface
//
//  1.    The class should take IOrderService & a decimal threshold value as parameters in the constructor
//  2.    In RespondToTick if the incoming price is less than the threshold use the IOrderService to buy, and also raise the "Placed" event
//  3.    If anything goes wrong you should raise the "Errored" event
//  4.    Prevent any further buys once one has been placed, or if there is an error
//  5.    The code should be thread safe, and you should assume it can be called from multiple threads

Key points
- Language: C# 12 / .NET 8
- Uses `FluentValidation` for input validation
- Thread-safe `RespondToTick` implementation
- Safe event invocation: subscriber exceptions are logged and do not propagate back to the caller
- Tests use NUnit + Moq + FluentAssertions

Getting started

Prerequisites
- .NET 8 SDK (install from https://dotnet.microsoft.com)

Build

Run from repository root:

```bash
dotnet build
```

Run tests

```bash
dotnet test
```

Project layout
- `OrderSystem/` - main project
  - `Order.cs` - core `Order` implementation
  - `Interfaces/` - `IOrder`, `IOrderService`, event interfaces
  - `Models/` - `PlacedEventArgs`, `ErroredEventArgs`, `OrderInput`, etc.
  - `Validators/` - `OrderInputValidator`, `TickDataValidator` (FluentValidation)
- `OrderSystem.Tests/` - unit tests (`OrderTests.cs`)

Usage example

```csharp
// create Order with an IOrderService implementation and optional logger
var order = new Order(orderService, priceThreshold);

order.Placed += args => Console.WriteLine($"Placed {args.Code} @ {args.Price}");
order.Errored += args => Console.WriteLine($"Errored {args.Code} @ {args.Price}: {args.GetException()}");

order.RespondToTick("BOND", 50m);
```

Event behavior
- `Placed` event is raised after a successfull buy.
- `Errored` event is raised in case of an exception.
- Events are invoked per-subscriber inside try/catch: when a subscriber throws, the exception is logged (`Trace`) and invocation continues for other subscribers.
- Once an order is successfully placed or a buy attempt errors, further calls to `RespondToTick` do not attempt to buy and return control.

Tests & Conventions
- Test method naming uses the `MethodName_StateUnderTest_ExpectedBehavior` convention (e.g. `Constructor_PriceThresholdIsZero_ThrowsValidationException`).
- Concurrency tests exercise `RespondToTick` using parallel calls to ensure only one buy occurs.

## Design Decisions

1. **Thread Safety**: Uses a simple lock-based approach for thread safety, which is appropriate for the expected usage pattern
2. **Single Order Policy**: Once an order is placed or an error occurs, no further orders are accepted
3. **Quantity**: Fixed at 1 unit per buy order
4. **Event Patterns**: Uses standard .NET event pattern with custom EventArgs classes
5. **Error Handling**: Any exception during buy operation is caught and reported via the `Errored` event
6. **Input Validation**: FluentValidation provides declarative, testable validation rules
   - Constructor parameters are validated immediately
   - Tick data is validated before entering the lock to prevent invalid data from blocking
   - Clear, descriptive error messages for validation failures
7. **Testing Framework**: NUnit chosen for its robust assertion model and wide industry adoption
8. **Mocking**: Moq framework provides clean, fluent syntax for creating test doubles and verifying interactions
9. **Assertions**: FluentAssertions provides natural language assertions that make tests more readable and maintainable