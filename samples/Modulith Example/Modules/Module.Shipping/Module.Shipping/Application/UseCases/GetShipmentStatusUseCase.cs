using FluentValidation; // Used elsewhere to validate the UseCase before it reaches this Handler.
using Faster.Modulith.Contracts;
using Module.Shipping.Api; // Access to the Public Contract (GetShipmentStatusUseCase)

namespace Module.Shipping.Application.UseCases
{
    /// <summary>
    /// Handles the business logic for retrieving a shipment's status.
    /// <para>
    /// <b>Why exists?</b> This class creates a clean separation of concerns. 
    /// The 'UseCase' record in the API project defines the <i>Contract</i> (the input), 
    /// while this Handler defines the <i>Implementation</i> (the logic).
    /// </para>
    /// </summary>
    /// <remarks>
    /// <b>Why Internal?</b> This class is declared <c>internal</c> because it is an implementation detail. 
    /// No other module needs to instantiate this directly. They should communicate only via the 
    /// <see cref="GetShipmentStatusUseCase"/> public contract.
    /// </remarks>
    internal class GetShipmentStatusHandler : IUseCaseHandler<GetShipmentStatusUseCase, Result<string>>
    {
        /// <summary>
        /// Executes the lookup logic when a <see cref="GetShipmentStatusUseCase"/> is dispatched.
        /// </summary>
        /// <param name="useCase">The immutable request object containing the necessary inputs (e.g., ShipmentId).</param>
        /// <param name="ct">A token that allows the operation to be cancelled (e.g., if the user closes the browser).</param>
        /// <returns>A standardized <see cref="Result{T}"/> containing the status string.</returns>
        public async ValueTask<Result<string>> Handle(GetShipmentStatusUseCase useCase, CancellationToken ct)
        {
            // HOW: Typically, you would inject a Repository (Infrastructure Layer) into the constructor 
            // and call something like: await _repository.GetStatusAsync(useCase.Id, ct);

            // DEMO LOGIC: Simulating a Database Lookup
            // Conceptually: SELECT Status FROM Shipments WHERE OrderId = @id
            var status = "Shipped - In Transit";

            // WHY ValueTask?
            // Since this specific demo path is synchronous (no real I/O wait), ValueTask is more memory-efficient 
            // than Task because it avoids allocating an object on the heap for immediate results.

            // HOW: We wrap the raw data in Result.Success().
            // WHY: This wrapper allows us to return metadata (like Success/Failure flags) alongside the data,
            // avoiding the need to throw expensive Exceptions for flow control (like "Item Not Found").
            return Result<string>.Success(status);
        }
    }
}