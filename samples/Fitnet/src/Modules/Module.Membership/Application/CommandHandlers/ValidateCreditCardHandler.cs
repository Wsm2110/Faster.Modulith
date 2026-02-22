using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using Faster.Modulith.Contracts;

namespace Module.Membership.Application.CommandHandlers
{
    internal class ValidateCreditCardHandler : ICommandHandler<ValidateCreditCardCommand, Result>
    {
        public async ValueTask<Result> Handle(ValidateCreditCardCommand command, CancellationToken ct)
        {
            throw new NotImplementedException();
        }
    }


    internal record ValidateCreditCardCommand(Guid CustomerId, int CreditCardNumber, DateTime ValidUntil) : ICommand<Result>;
    

    internal class ValidateCreditCardValidator : AbstractValidator<ValidateCreditCardCommand>
    {
        public ValidateCreditCardValidator()
        {
            // RuleFor(c => c.Id).NotEqual(0).WithMessage("Id cannot be 0");
        }
    }
}
