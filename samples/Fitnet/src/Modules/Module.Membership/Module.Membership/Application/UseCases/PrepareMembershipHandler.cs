using Faster.Modulith.Contracts;
using FluentValidation;
using Module.Membership.Api;
using Module.Membership.Contracts;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Module.Membership.Domain;
using Module.Membership.Infrastructure;
using Module.Membership.Application.CommandHandlers;
using Faster.Modulith;

namespace Module.Membership.Application.UseCases
{
    internal class PrepareMembershipHandler(IMembershipRepository Repository, IMembershipDispatcher dispatcher) : IUseCaseHandler<PrepareMembershipUseCase, Result<Guid>>
    {
        public async ValueTask<Result<Guid>> Handle(PrepareMembershipUseCase useCase, CancellationToken ct)
        {
            Console.WriteLine($"[{DateTime.UtcNow:O}] Handling PrepareContractCommand for Customer: {useCase.CustomerId}");

            var contract = new Contract(useCase.CustomerId, useCase.PlanType);

            var result = await dispatcher.MembershipDiscount(new MembershipDiscountCommand(contract));

            await Repository.SaveAsync(contract);         

            return result.IsSuccess ? Result<Guid>.Success(Guid.NewGuid()) : Result<Guid>.Failure(result.Error);
        }
    }

    internal class PrepareMembershipValidator : AbstractValidator<PrepareMembershipUseCase>
    {
        public PrepareMembershipValidator()
        {
            RuleFor(c => c.CustomerId).NotEmpty().WithMessage("CustomerId cannot be empty");
        }
    }
}
