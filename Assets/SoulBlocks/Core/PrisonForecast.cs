namespace SoulBlocks
{
    public enum ForecastKind
    {
        Breaks,
        Hits,
        RequiresLevel,
        Broken
    }

    // What IMineable.Forecast(pickLevel) returns: the pick preview shown next to the crystal.
    public struct PrisonForecast
    {
        public ForecastKind Kind;
        public int hits;
        public int requiredLevel;

        public string Describe()
        {
            switch (Kind)
            {
                case ForecastKind.Breaks: return "último golpe";
                case ForecastKind.Hits: return hits + " golpes";
                case ForecastKind.RequiresLevel: return "requiere pico nv " + requiredLevel;
                case ForecastKind.Broken: return "rota";
                default: return "";
            }
        }
    }
}
