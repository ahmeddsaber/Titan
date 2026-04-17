using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Titan.Application.DTOs.Dashboard
{
    public record RevenueChartDto(string Label, decimal Revenue, int Orders);
}
