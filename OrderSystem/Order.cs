using OrderSystem.Interfaces;
using OrderSystem.Models;
using OrderSystem.Validators;
using FluentValidation;

namespace OrderSystem
{
    public class Order : IOrder
    {
        private readonly IOrderService _orderService;
        private readonly decimal _priceThreshold;
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

            throw new NotImplementedException();
        }
    }
}
