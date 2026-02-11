using FluentValidation;
using OrderSystem.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrderSystem.Validators
{
    public class OrderInputValidator : AbstractValidator<OrderInput>
    {
        public OrderInputValidator() 
        {
            RuleFor(x => x.OrderService)
                .NotNull()
                .WithMessage("OrderService cannot be null.");

            RuleFor(x => x.PriceThreshold)
                .GreaterThan(0)
                .WithMessage("PriceThreshold must be greater than zero.");
        }
    }
}
