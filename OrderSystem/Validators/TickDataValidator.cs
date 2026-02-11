using FluentValidation;
using OrderSystem.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrderSystem.Validators
{
    public class TickDataValidator : AbstractValidator<TickData>
    {
        public TickDataValidator() 
        {
            RuleFor(x => x.Code)
                .NotNull()
                .WithMessage("Code cannot be null.")
                .NotEmpty()
                .WithMessage("Code cannot be empty.");

            RuleFor(x => x.Price)
                .GreaterThan(0)
                .WithMessage("Price must be greater than zero.");
        }
    }
}
