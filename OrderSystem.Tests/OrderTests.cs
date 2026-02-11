using Moq;
using OrderSystem.Interfaces;
using FluentAssertions;
using FluentValidation;
using OrderSystem.Models;
using System.Threading.Tasks;

namespace OrderSystem.Tests
{
    [TestFixture]
    public class OrderTests
    {
        private Mock<IOrderService> _mockOrderService;
        private Order _order;
        private decimal _priceThreshold = 100m;

        [SetUp]
        public void Setup()
        {
            _mockOrderService = new Mock<IOrderService>();
        }

        [Test]
        public void Constructor_OrderServiceIsNull_ThrowsValidationException()
        {
            //Act
            Action action = () => new Order(null, _priceThreshold);

            //Assert
            action.Should().Throw<ValidationException>()
                .WithMessage("OrderService cannot be null.");
        }

        [Test]
        public void Constructor_PriceThresholdIsNegative_ThrowsValidationException()
        {
            //Act
            Action action = () => new Order(_mockOrderService.Object, -1m);

            //Assert
            action.Should().Throw<ValidationException>()
                .WithMessage("PriceThreshold must be greater than zero.");
        }

        [Test]
        public void Constructor_PriceThresholdIsZero_ThrowsValidationException()
        {
            //Act
            Action action = () => new Order(_mockOrderService.Object, 0m);

            //Assert
            action.Should().Throw<ValidationException>()
                .WithMessage("PriceThreshold must be greater than zero.");
        }

        [Test]
        public void Constructor_ValidParameters_DoesNotThrow()
        {
            //Act
            Action action = () => new Order(_mockOrderService.Object, _priceThreshold);

            //Assert
            action.Should().NotThrow();
        }

        [Test]
        public void RespondToTick_PriceBelowThreshold_BuysStock()
        {
            //Arrange
            _order = new Order(_mockOrderService.Object, _priceThreshold);

            //Act
            _order.RespondToTick("BOND", 50m);

            //Assert
            _mockOrderService.Verify(s => s.Buy("BOND", 1, 50m), Times.Once);
        }

        [Test]
        public void RespondToTick_PriceAboveThreshold_DoesNotBuy()
        {
            //Arrange
            _order = new Order(_mockOrderService.Object, _priceThreshold);

            //Act
            _order.RespondToTick("BOND", 150m);

            //Assert
            _mockOrderService.Verify(s => s.Buy(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<decimal>()), Times.Never);
        }

        [Test]
        public void RespondToTick_PriceEqualsThreshold_DoesNotBuy()
        {
            //Arrange
            _order = new Order(_mockOrderService.Object, _priceThreshold);

            //Act
            _order.RespondToTick("BOND", _priceThreshold);

            //Assert
            _mockOrderService.Verify(s => s.Buy(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<decimal>()), Times.Never);
        }

        [Test]
        public void RespondToTick_BuySuccessful_RaisesPlacedEvent()
        {
            //Arrange
            _order = new Order(_mockOrderService.Object, _priceThreshold);
            PlacedEventArgs placedEventArgs = null;
            bool eventRaised = false;
            _order.Placed += (args) =>
            {
                eventRaised = true;
                placedEventArgs = args;
            };

            //Act
            _order.RespondToTick("BOND", 50m);

            //Assert
            eventRaised.Should().BeTrue();
            placedEventArgs.Should().NotBeNull();
            placedEventArgs.Code.Should().Be("BOND");
            placedEventArgs.Price.Should().Be(50m);
        }

        [Test]
        public void RespondToTick_BuyThrowsException_RaisesErroredEvent()
        {
            //Arrange
            Exception ex = new Exception("Buy failed");
            _mockOrderService.Setup(s => s.Buy(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<decimal>())).Throws(ex);
            _order = new Order(_mockOrderService.Object, _priceThreshold);
            ErroredEventArgs erroredEventArgs = null;
            bool eventRaised = false;
            _order.Errored += (args) =>
            {
                eventRaised = true;
                erroredEventArgs = args;
            };

            //Act
            _order.RespondToTick("BOND", 50m);

            //Assert
            eventRaised.Should().BeTrue();
            erroredEventArgs.Should().NotBeNull();
            erroredEventArgs.Code.Should().Be("BOND");
            erroredEventArgs.Price.Should().Be(50m);
            erroredEventArgs.GetException().Should().Be(ex);
        }

        [Test]
        public void RespondToTick_AfterFirstBuyIsPlaced_PreventsSubsequentBuys()
        {
            //Arrange
            _order = new Order(_mockOrderService.Object, _priceThreshold);
            _order.RespondToTick("BOND", 50m); // First buy

            //Act
            _order.RespondToTick("BOND2", 60m); // Attempt second buy

            //Assert
            _mockOrderService.Verify(s => s.Buy("BOND", 1, 50m), Times.Once); // First buy should occur
            _mockOrderService.Verify(s => s.Buy("BOND2", 1, 60m), Times.Never); // Second buy should not occur
            _mockOrderService.Verify(s => s.Buy(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<decimal>()), Times.Once); // Only first buy should occur
        }

        [Test]
        public void RespondToTick_AfterErroredEventIsRaised_PreventsSubsequentBuys()
        {
            //Arrange
            Exception ex = new Exception("Buy failed");
            _mockOrderService.Setup(s => s.Buy(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<decimal>())).Throws(ex);
            _order = new Order(_mockOrderService.Object, _priceThreshold);
            _order.RespondToTick("BOND", 50m); // First buy attempt that fails

            //Act
            _order.RespondToTick("BOND2", 60m); // Attempt second buy

            //Assert
            _mockOrderService.Verify(s => s.Buy("BOND", 1, 50m), Times.Once); // First buy attempt should occur
            _mockOrderService.Verify(s => s.Buy("BOND2", 1, 60m), Times.Never); // Second buy should not occur
            _mockOrderService.Verify(s => s.Buy(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<decimal>()), Times.Once); // Only first buy attempt should occur
        }

        [Test]
        public void RespondToTick_IsThreadSafe_WhenCalledFromMultipleThreadsConcurrently_BehavesCorrectly()
        {
            //Arrange
            _order = new Order(_mockOrderService.Object, _priceThreshold);
            int threadCount = 10;

            ErroredEventArgs erroredEventArgs = null;
            bool errorEventRaised = false;
            _order.Errored += (args) =>
            {
                errorEventRaised = true;
                erroredEventArgs = args;
            };

            var placeEventCount = 0;
            PlacedEventArgs placedEventArgs = null;
            bool placedEventRaised = false;
            _order.Placed += (args) =>
            {
                placedEventRaised = true;
                placedEventArgs = args;
                Interlocked.Increment(ref placeEventCount);
            };

            //Act - run the calls in parallel
            Parallel.For(0, threadCount, i =>
            {
                _order.RespondToTick($"BOND{i}", 50m);
            });

            //Assert
            _mockOrderService.Verify(s => s.Buy(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<decimal>()), Times.Once); // Only one buy should occur
            placedEventRaised.Should().BeTrue(); // Placed event should be raised
            placedEventArgs.Should().NotBeNull();
            placedEventArgs.Code.Should().StartWith("BOND");
            placedEventArgs.Price.Should().Be(50m);
            placeEventCount.Should().Be(1); // Only one Placed event should be raised
            errorEventRaised.Should().BeFalse(); // No error event should be raised
            erroredEventArgs.Should().BeNull();
        }

        [Test]
        public void RespondToTick_IsThreadSafe_WhenBuyThrowsExceptionFromMultipleThreadsConcurrently_BehavesCorrectly()
        {
            //Arrange
            Exception ex = new Exception("Buy failed");
            _mockOrderService.Setup(s => s.Buy(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<decimal>())).Throws(ex);
            _order = new Order(_mockOrderService.Object, _priceThreshold);
            int threadCount = 10;

            var errorEventCount = 0;
            ErroredEventArgs erroredEventArgs = null;
            bool errorEventRaised = false;
            _order.Errored += (args) =>
            {
                errorEventRaised = true;
                erroredEventArgs = args;
                Interlocked.Increment(ref errorEventCount);
            };

            PlacedEventArgs placedEventArgs = null;
            bool placedEventRaised = false;
            _order.Placed += (args) =>
            {
                placedEventRaised = true;
                placedEventArgs = args;
            };

            //Act - run the calls in parallel
            Parallel.For(0, threadCount, i =>
            {
                _order.RespondToTick($"BOND{i}", 50m);
            });

            //Assert
            _mockOrderService.Verify(s => s.Buy(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<decimal>()), Times.Once); // Only one buy attempt should occur
            placedEventRaised.Should().BeFalse(); // Placed event should not be raised
            placedEventArgs.Should().BeNull();
            errorEventCount.Should().Be(threadCount); // All errored events should be raised
            errorEventRaised.Should().BeTrue(); // Error event should be raised
            erroredEventArgs.Should().NotBeNull();
            erroredEventArgs.Code.Should().StartWith("BOND");
            erroredEventArgs.Price.Should().Be(50m);
            erroredEventArgs.GetException().Should().Be(ex);
        }

        [Test]
        public void RespondToTick_HandlesMultipleSubsribersToPlacedEvent_AllSubscribersAreNotified()
        {
            //Arrange
            _order = new Order(_mockOrderService.Object, _priceThreshold);
            int subscriberCount = 5;
            int eventNotificationCount = 0;
            for (int i = 0; i < subscriberCount; i++)
            {
                _order.Placed += (args) =>
                {
                    Interlocked.Increment(ref eventNotificationCount);
                };
            }

            //Act
            _order.RespondToTick("BOND", 50m);

            //Assert
            eventNotificationCount.Should().Be(subscriberCount); // All subscribers should be notified
        }

        [Test]
        public void RespondToTick_HandlesMultipleSubsribersToErroredEvent_AllSubscribersAreNotified()
        {
            //Arrange
            Exception ex = new Exception("Buy failed");
            _mockOrderService.Setup(s => s.Buy(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<decimal>())).Throws(ex);
            _order = new Order(_mockOrderService.Object, _priceThreshold);
            int subscriberCount = 5;
            int eventNotificationCount = 0;
            for (int i = 0; i < subscriberCount; i++)
            {
                _order.Errored += (args) =>
                {
                    Interlocked.Increment(ref eventNotificationCount);
                };
            }

            //Act
            _order.RespondToTick("BOND", 50m);

            //Assert
            eventNotificationCount.Should().Be(subscriberCount); // All subscribers should be notified
        }

        [Test]
        public void RespondToTick_WhenBuyIsTrigered_DoesNotTriggerSell()
        {
            //Arrange
            _order = new Order(_mockOrderService.Object, _priceThreshold);

            //Act
            _order.RespondToTick("BOND", 50m);

            //Assert
            _mockOrderService.Verify(s => s.Sell(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<decimal>()), Times.Never); // Sell should never be called
        }

        [Test]
        public void RespondToTick_CodeIsNull_ThrowsValidationException()
        {
            //Arrange
            _order = new Order(_mockOrderService.Object, _priceThreshold);

            //Act
            Action action = () => _order.RespondToTick(null, 50m);

            //Assert
            action.Should().Throw<ValidationException>()
                .WithMessage("Code cannot be null.");
        }

        [Test]
        public void RespondToTick_CodeIsEmpty_ThrowsValidationException()
        {
            //Arrange
            _order = new Order(_mockOrderService.Object, _priceThreshold);

            //Act
            Action action = () => _order.RespondToTick(null, 50m);

            //Assert
            action.Should().Throw<ValidationException>()
                .WithMessage("Code cannot be empty.");
        }

        [Test]
        public void RespondToTick_PriceIsNegative_ThrowsValidationException()
        {
            //Arrange
            _order = new Order(_mockOrderService.Object, _priceThreshold);

            //Act
            Action action = () => _order.RespondToTick("BOND", -1m);

            //Assert
            action.Should().Throw<ValidationException>()
                .WithMessage("Price must be greater than zero.");
        }

        [Test]
        public void RespondToTick_PriceIsZero_ThrowsValidationException()
        {
            //Arrange
            _order = new Order(_mockOrderService.Object, _priceThreshold);

            //Act
            Action action = () => _order.RespondToTick("BOND", 0m);

            //Assert
            action.Should().Throw<ValidationException>()
                .WithMessage("Price must be greater than zero.");
        }
    }
}