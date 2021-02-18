﻿using DFC.Api.Lmi.Import.Attributes;
using System.Diagnostics.CodeAnalysis;

namespace DFC.Api.Lmi.Import.Models.GraphData
{
    [ExcludeFromCodeCoverage]
    [GraphNode("py", "LmiSocPredictedYear")]
    public class GraphPredictedYearModel : GraphBaseModel
    {
        [GraphProperty(nameof(Year), isKey: true, isPreferredLabel: true)]
        public int Year { get; set; }

        [GraphProperty(nameof(Employment))]
        public decimal Employment { get; set; }
    }
}
