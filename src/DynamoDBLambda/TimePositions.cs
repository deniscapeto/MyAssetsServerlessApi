using System;
using System.Collections.Generic;

namespace DynamoDBLambda
{
    public class TimePosition
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public DateTime CreatedTimestamp { get; set; }
        public List<AssetPosition> AssetPositions { get; set; }
    }
}
