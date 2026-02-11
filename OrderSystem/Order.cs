using OrderSystem.Interfaces;
using OrderSystem.Models;
using OrderSystem.Validators;
using FluentValidation;
using System.Diagnostics;

namespace OrderSystem
{
    public class Order : IOrder
    {
        private readonly IOrderService _orderService;
        private readonly decimal _priceThreshold;
        private readonly Object _lock = new Object();
        private bool _isPlaced = false;
        private bool _hasErrored = false;
        public event PlacedEventHandler Placed;
        public event ErroredEventHandler Errored;

        private static readonly OrderInputValidator _inputValidator = new OrderInputValidator();
        private static readonly TickDataValidator _tickDataValidator = new TickDataValidator();

        public Order(IOrderService orderService, decimal priceThreshold)
        {
            // Validate constructor parameters
            var parameters = new OrderInput
            {
                OrderService = orderService,
                PriceThreshold = priceThreshold
            };

            var validationResults = _inputValidator.Validate(parameters);

            if(!validationResults.IsValid)
            {
                var error = validationResults.Errors.FirstOrDefault().ErrorMessage;
                throw new ValidationException(error);
            }

            _orderService = orderService;
            _priceThreshold = priceThreshold;
        }

        public void RespondToTick(string code, decimal price)
        {
            // validate tick data
            var tickData = new TickData
            {
                Code = code,
                Price = price
            };

            var validationResults = _tickDataValidator.Validate(tickData);

            if(!validationResults.IsValid)
            {
                var error = validationResults.Errors.FirstOrDefault().ErrorMessage;
                throw new ValidationException(error);
            }

            lock(_lock)
            {
                if(_isPlaced || _hasErrored)
                {
                    return; // Order already placed or errored, ignore further ticks
                }

                try
                {
                    if(price >= _priceThreshold)
                    {
                        return;
                    }

                    _orderService.Buy(code, 1, price); // Assumption: Always Buy just 1 unit
                    _isPlaced = true;

                    SafeInvoke(Placed, (subscriber, ev) => ((PlacedEventHandler)subscriber)((PlacedEventArgs)ev), new PlacedEventArgs(code, price));
                }
                catch (Exception ex)
                {
                    // marked to prevent further processing of ticks after an error has occurred
                    _hasErrored = true;

                    SafeInvoke(Errored, (subscriber, ev) => ((ErroredEventHandler)subscriber)((ErroredEventArgs)ev), new ErroredEventArgs(code, price, ex));
                }
            }
        }

        // Safely invoke event subscribers: call each subscriber via provided invoker,
        // log subscriber exceptions and continue so they don't propagate to the caller.
        private void SafeInvoke<TEventArgs>(Delegate handler, Action<Delegate, TEventArgs> invoker, TEventArgs args)
        {
            if (handler == null) return;

            foreach (var subscriber in handler.GetInvocationList())
            {
                try
                {
                    invoker(subscriber, args);
                }
                catch (Exception ex)
                {
                    Trace.TraceError("Subscriber for {0} threw exception: {1}", typeof(TEventArgs).Name, ex);
                }
            }
        }
    }
}
