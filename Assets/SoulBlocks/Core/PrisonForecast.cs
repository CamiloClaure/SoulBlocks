namespace SoulBlocks
{
    public enum ForecastKind
    {
        Breaks,
        Hits,
        RequiresLevel,
        Broken
    }

    // What IRayTarget.Forecast(rayLevel) returns: the Rayhit aiming preview shown to the player.
    public struct PrisonForecast
    {
        public ForecastKind Kind;
        public int hits;
        public int requiredLevel;

        public string Describe()
        {
            switch (Kind)
            {
                case ForecastKind.Breaks: return "ruptura inmediata";
                case ForecastKind.Hits: return hits + " impactos";
                case ForecastKind.RequiresLevel: return "requiere Rayhit nv " + requiredLevel;
                case ForecastKind.Broken: return "rota";
                default: return "";
            }
        }
    }
}
