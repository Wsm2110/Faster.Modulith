using System;
using System.Collections.Generic;
using System.Text;
using Faster.Modulith.Contracts;

namespace Module.Ordering.Api.UseCases;

public record struct FinalizeTableOrderUseCase : IUseCase<Result<decimal>>
{
    public int TableNumber { get; set; }
}