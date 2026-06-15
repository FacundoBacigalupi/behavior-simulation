namespace BehaviorSimulation.GameOfLife
{
    // Conway's Game of Life rules applied to flat bool arrays.
    // Rules:
    //   Live cell, <2 live neighbors  -> dies (underpopulation)
    //   Live cell, 2-3 live neighbors -> survives
    //   Live cell, >3 live neighbors  -> dies (overpopulation)
    //   Dead cell, ==3 live neighbors -> born (reproduction)
    public static class LifeRules
    {
        public static void NextGeneration(bool[] current, bool[] next, int width, int height)
        {
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int neighbors = CountNeighbors(current, x, y, width, height);
                    bool alive = current[y * width + x];
                    next[y * width + x] = alive
                        ? neighbors == 2 || neighbors == 3
                        : neighbors == 3;
                }
            }
        }

        // Counts the 8 Moore neighbors, wrapping at grid edges (toroidal grid).
        private static int CountNeighbors(bool[] grid, int x, int y, int width, int height)
        {
            int count = 0;
            for (int dy = -1; dy <= 1; dy++)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    if (dx == 0 && dy == 0) continue;
                    int nx = (x + dx + width) % width;
                    int ny = (y + dy + height) % height;
                    if (grid[ny * width + nx]) count++;
                }
            }
            return count;
        }
    }
}
