using OrderSystem.Interfaces;

namespace OrderSystem
{
    public class Order : IOrder
    {
        private readonly IOrderService _orderService;
        private readonly decimal _priceThreshold;
        public event PlacedEventHandler Placed;
        public event ErroredEventHandler Errored;

        public Order(IOrderService orderService, decimal priceThreshold)
        {
            _orderService = orderService;
            _priceThreshold = priceThreshold;
        }

        public void RespondToTick(string code, decimal price)
        {
            throw new NotImplementedException();
        }
    }
}
