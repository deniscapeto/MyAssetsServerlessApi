using System;
using System.Collections.Generic;
using System.Text;

namespace DynamoDBLambda
{
    public class AssetPosition
    {
        public int Id { get; set; }
        public DateTime DateTime { get; set; }
        public string Asset { get; set; }
        public decimal Amount { get; set; }
        public DateTime CreatedTimestamp { get; set; }

        /// <summary>
        /// Amount that was added since last position. This Amount is not take into account when calculating profit
        /// </summary>
        public decimal AmountAdded { get; set; }

    }
}
