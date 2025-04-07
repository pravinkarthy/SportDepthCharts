using System;

namespace CodingTest.DepthCharts.Models;

public class SportConfiguration<TPosition> where TPosition : struct, Enum
{
    public Type PositionType { get; } = typeof(TPosition);
    public string QueueName { get; }

    public SportConfiguration(string queueName)
    {
        QueueName = queueName;
    }
}
