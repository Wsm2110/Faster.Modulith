using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Module.Ordering.Domain;
using System;
using System.Collections.Generic;
using System.Text;

namespace Module.Ordering.Infrastructure;

public partial class OrderingOptions
{
    public bool UseInMemory { get; set; }
 
}