using UnityEngine;

namespace ActionHunters.Runtime
{
    public static class DemoGameRules
    {
        public static bool CanHire(int gold, int cost, int activeHunters, int maximumHunters)
        {
            return cost >= 0 && gold >= cost && activeHunters < maximumHunters;
        }

        public static DemoTeam DetermineWinner(int blueScore, int redScore)
        {
            if (blueScore == redScore)
            {
                return DemoTeam.Neutral;
            }

            return blueScore > redScore ? DemoTeam.Blue : DemoTeam.Red;
        }

        public static string FormatTime(float seconds)
        {
            var remaining = Mathf.Max(0, Mathf.CeilToInt(seconds));
            return $"{remaining / 60:00}:{remaining % 60:00}";
        }

        public static bool AreHostile(DemoTeam first, DemoTeam second)
        {
            if (first == second)
            {
                return false;
            }

            return first == DemoTeam.Neutral || second == DemoTeam.Neutral || first != second;
        }
    }
}
