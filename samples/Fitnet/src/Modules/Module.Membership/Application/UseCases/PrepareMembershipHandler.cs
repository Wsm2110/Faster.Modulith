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

namespace Module.Membership.Application.UseCases
{
    /// <summary>
    /// Handles the preparation and initialization of a new membership contract.
    /// </summary>
    internal class PrepareMembershipHandler(IMembershipRepository Repository, IMembershipDispatcher dispatcher) : IUseCaseHandler<PrepareMembershipUseCase, Result<Guid>>
    {
        /// <summary>
        /// Asynchronously processes the membership preparation, applies any eligible discounts, and validates the provided credit card.
        /// </summary>
        /// <param name="useCase">The data transfer object containing the customer and plan details.</param>
        /// <param name="ct">The cancellation token to observe while waiting for the task to complete.</param>
        /// <returns>A value task representing the asynchronous operation, yielding a result with the generated contract identifier if successful, or an error otherwise.</returns>
        public async ValueTask<Result<Guid>> Handle(PrepareMembershipUseCase useCase, CancellationToken ct)
        {
            Console.WriteLine($"[{DateTime.UtcNow:O}] Handling PrepareContractCommand for Customer: {useCase.CustomerId}");

            var contract = new Contract(useCase.CustomerId, useCase.PlanType);

            var discountResult = await dispatcher.MembershipDiscount(new MembershipDiscountCommand(contract));
            if (!discountResult.IsSuccess)
            {
                return Result<Guid>.Failure(discountResult.Error);
            }

            var creditcardResult = await dispatcher.ValidateCreditCard(contract.CustomerId, contract.CreditCardNumber, contract.ValidUntil);
            if (!creditcardResult.IsSuccess)
            {
                return Result<Guid>.Failure(creditcardResult.Error);
            }

            await Repository.SaveAsync(contract);

            return Result<Guid>.Success(Guid.NewGuid());
        }
    }

    /// <summary>
    /// Provides validation rules for the <see cref="PrepareMembershipUseCase"/>.
    /// </summary>
    internal class PrepareMembershipValidator : AbstractValidator<PrepareMembershipUseCase>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PrepareMembershipValidator"/> class and defines validation constraints.
        /// </summary>
        public PrepareMembershipValidator()
        {
            RuleFor(c => c.CustomerId).NotEmpty().WithMessage("CustomerId cannot be empty");
        }
    }
}