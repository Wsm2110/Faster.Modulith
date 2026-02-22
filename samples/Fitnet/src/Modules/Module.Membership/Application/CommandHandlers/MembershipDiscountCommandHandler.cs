using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using Faster.Modulith.Contracts;
using Module.Membership.Domain;

namespace Module.Membership.Application.CommandHandlers;

internal class MembershipDiscountCommandHandler : ICommandHandler<MembershipDiscountCommand, Result>
{
    public async ValueTask<Result> Handle(MembershipDiscountCommand command, CancellationToken ct)
    {
        throw new NotImplementedException();
    }
}

internal record MembershipDiscountCommand(Contract Contract) : ICommand<Result>
{
}

internal class MembershipDiscountValidator : AbstractValidator<MembershipDiscountCommand>
{
    public MembershipDiscountValidator()
    {
        // RuleFor(c => c.Id).NotEqual(0).WithMessage("Id cannot be 0");
    }
}