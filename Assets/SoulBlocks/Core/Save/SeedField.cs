using System.Text;

namespace SoulBlocks
{
    // Pure parsing for the seed text field shared by the start menu and the pause menu.
    public static class SeedField
    {
        // Keeps only characters a valid seed could contain: digits anywhere, a minus sign only
        // at position 0 of the raw string. Meant to be re-applied every keystroke (the field is
        // re-filtered each frame, not parsed once), so a stray leading non-digit just drops the
        // sign rather than needing smarter reconstruction.
        public static string Filter(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return "";
            var sb = new StringBuilder(raw.Length);
            for (int i = 0; i < raw.Length; i++)
            {
                char c = raw[i];
                if (char.IsDigit(c) || (c == '-' && i == 0)) sb.Append(c);
            }
            return sb.ToString();
        }

        public static bool TryParse(string text, out int seed) => int.TryParse(text, out seed);
    }
}
