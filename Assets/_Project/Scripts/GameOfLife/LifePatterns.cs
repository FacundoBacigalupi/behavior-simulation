using UnityEngine;

namespace BehaviorSimulation.GameOfLife
{
    // Predefined cell patterns. Each returns offsets relative to a center point.
    // x increases right, y increases up.
    public static class LifePatterns
    {
        // Glider: travels diagonally, period 4.
        public static readonly Vector2Int[] Glider =
        {
            new(1, 2),
            new(2, 1),
            new(0, 0), new(1, 0), new(2, 0),
        };

        // Blinker: simplest oscillator, period 2.
        public static readonly Vector2Int[] Blinker =
        {
            new(-1, 0), new(0, 0), new(1, 0),
        };

        // Toad: period-2 oscillator, two offset rows of three.
        public static readonly Vector2Int[] Toad =
        {
            new(0, 0), new(1, 0), new(2, 0),
            new(-1, 1), new(0, 1), new(1, 1),
        };

        // Pulsar: period-3, large symmetric oscillator.
        public static readonly Vector2Int[] Pulsar =
        {
            // Top cluster
            new(-4, 6), new(-3, 6), new(-2, 6),
            new(2, 6),  new(3, 6),  new(4, 6),

            new(-6, 4), new(-1, 4), new(1, 4), new(6, 4),
            new(-6, 3), new(-1, 3), new(1, 3), new(6, 3),
            new(-6, 2), new(-1, 2), new(1, 2), new(6, 2),

            new(-4, 1), new(-3, 1), new(-2, 1),
            new(2, 1),  new(3, 1),  new(4, 1),

            // Bottom cluster (mirror of top)
            new(-4, -1), new(-3, -1), new(-2, -1),
            new(2, -1),  new(3, -1),  new(4, -1),

            new(-6, -2), new(-1, -2), new(1, -2), new(6, -2),
            new(-6, -3), new(-1, -3), new(1, -3), new(6, -3),
            new(-6, -4), new(-1, -4), new(1, -4), new(6, -4),

            new(-4, -6), new(-3, -6), new(-2, -6),
            new(2, -6),  new(3, -6),  new(4, -6),
        };
    }
}
