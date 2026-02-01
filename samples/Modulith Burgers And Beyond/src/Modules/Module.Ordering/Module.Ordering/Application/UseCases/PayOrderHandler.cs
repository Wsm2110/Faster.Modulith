using Faster.Modulith.Contracts;
using FluentValidation;
using Module.Ordering.Domain;
using Faster.Modulith;
using Microsoft.EntityFrameworkCore;
using Module.Ordering.Api.UseCases;

namespace Module.Ordering.Application.UseCases
{

    /// <summary>
    /// Handles payment processing for orders by marking them as paid and notifying other modules of the payment event.
    /// </summary>
    /// <remarks>This handler retrieves the specified order, marks it as paid, and saves the changes to the
    /// database. It also publishes a payment event to notify other modules. If the order is not found or an error
    /// occurs during processing, a failure result is returned.</remarks>
    /// <param name="db">The database context used to access and update order data.</param>
    /// <param name="dispatcher">The dispatcher used to publish payment events to other modules, such as Billing or Analytics.</param>
    internal class PayOrderHandler(OrderingDbContext db, IOrderingDispatcher dispatcher) : IUseCaseHandler<PayOrderUseCase, Result>
    {
        /// <summary>
        /// Gets the instance of the PayOrderValidator used for validating pay orders.
        /// </summary>
        /// <remarks>This validator is initialized as a readonly field and is intended to be used
        /// throughout the class for ensuring that pay orders meet the necessary validation criteria.</remarks>
        private readonly PayOrderValidator _validator = new();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async ValueTask<Result> Handle(PayOrderUseCase request, CancellationToken ct)
        {
            if (!_validator.ValidateAsync(request, ct).IsFaulted)
            {
                return Result.Failure("Validation failed");
            }

            var order = await db.Orders.FirstOrDefaultAsync(x => x.Id == request.OrderId, ct);
            if (order is null)
            {
                return Result.Failure("Order not found.");
            }

            try
            {
                order.MarkAsPaid();
                await db.SaveChangesAsync(ct);

                // Signal the Billing or Analytics modules
                dispatcher.PublishBurgerOrderPaid(order.Id, order.TotalPrice, ct);

                return Result.Success;
            }
            catch (InvalidOperationException ex)
            {
                return Result.Failure(ex.Message);
            }
        }
    }


    internal class PayOrderValidator : AbstractValidator<PayOrderUseCase>
    {
        public PayOrderValidator()
        {
            // RuleFor(c => c.Id).NotEqual(0).WithMessage("Id cannot be 0");
        }
    }
}
