using UnityEngine;

namespace BalloonToHeaven.Obstacles
{
    /// <summary>
    /// Fairness planner. The playfield is split into lanes. A two-lane "safe corridor" random-walks
    /// at most one lane per spawn, and no lane-blocking hazard may ever be placed in it, so a
    /// continuous escape path always exists. Every spawn row must also keep a minimum number of
    /// free lanes. Pure C# so it can be unit tested.
    /// </summary>
    public sealed class LanePlanner
    {
        readonly float[] blockedUntil;
        readonly System.Random rng;

        public int LaneCount { get; }
        public float HalfWidth { get; }
        public float LaneWidth { get; }
        /// <summary>Left lane of the two-lane safe corridor.</summary>
        public int Corridor { get; private set; }
        public float RowBlockedUntil { get; private set; }

        public LanePlanner(int laneCount, float halfWidth, System.Random random)
        {
            LaneCount = Mathf.Max(3, laneCount);
            HalfWidth = halfWidth;
            LaneWidth = halfWidth * 2f / LaneCount;
            blockedUntil = new float[LaneCount];
            rng = random;
            Corridor = LaneCount / 2 - 1;
        }

        public float LaneCenter(int lane) => -HalfWidth + LaneWidth * (lane + 0.5f);
        public int LaneOf(float x) => Mathf.Clamp(Mathf.FloorToInt((x + HalfWidth) / LaneWidth), 0, LaneCount - 1);
        public float CorridorCenter => -HalfWidth + LaneWidth * (Corridor + 1f);
        public bool IsCorridorLane(int lane) => lane == Corridor || lane == Corridor + 1;
        public bool IsFree(int lane, float time) => blockedUntil[lane] <= time;

        /// <summary>
        /// Random-walks the safe corridor by at most one lane. It only moves onto lanes that are
        /// free right now, so the two corridor lanes are never blocked at the spawn row.
        /// </summary>
        public void AdvanceCorridor(float time)
        {
            int next = Mathf.Clamp(Corridor + rng.Next(-1, 2), 0, LaneCount - 2);
            if (next == Corridor) return;
            if (!IsFree(next, time) || !IsFree(next + 1, time)) return;
            Corridor = next;
        }

        public int FreeCount(float time)
        {
            int n = 0;
            for (int i = 0; i < LaneCount; i++) if (blockedUntil[i] <= time) n++;
            return n;
        }

        public bool AllFree(float time) => FreeCount(time) == LaneCount;

        /// <summary>Lanes needed for an object of the given width.</summary>
        public int LanesFor(float width) => Mathf.Clamp(Mathf.CeilToInt(width / LaneWidth - 0.05f), 1, LaneCount - 2);

        public bool CanBlock(int start, int count, float time, int minFree)
        {
            if (start < 0 || start + count > LaneCount) return false;
            int newlyBlocked = 0;
            for (int i = start; i < start + count; i++)
            {
                if (IsCorridorLane(i)) return false;
                if (blockedUntil[i] <= time) newlyBlocked++;
            }
            return FreeCount(time) - newlyBlocked >= minFree;
        }

        public void Block(int start, int count, float until)
        {
            for (int i = Mathf.Max(0, start); i < Mathf.Min(LaneCount, start + count); i++)
                blockedUntil[i] = Mathf.Max(blockedUntil[i], until);
        }

        /// <summary>Finds a random valid start lane for a block of 'count' lanes.</summary>
        public bool TryFindPlacement(int count, float time, int minFree, out int start, int avoidLane = -1)
        {
            int options = LaneCount - count + 1;
            int offset = rng.Next(Mathf.Max(1, options));
            for (int k = 0; k < options; k++)
            {
                int s = (offset + k) % options;
                if (avoidLane >= s && avoidLane < s + count) continue;
                if (CanBlock(s, count, time, minFree)) { start = s; return true; }
            }
            start = -1;
            return false;
        }

        /// <summary>Reserves the whole row except the corridor (walls with a gap). Requires an empty row.</summary>
        public bool TryReserveFullRow(float time, float until)
        {
            if (!AllFree(time) || RowBlockedUntil > time) return false;
            for (int i = 0; i < LaneCount; i++) if (!IsCorridorLane(i)) blockedUntil[i] = until;
            RowBlockedUntil = until;
            return true;
        }

        public void MarkRowEvent(float until) => RowBlockedUntil = Mathf.Max(RowBlockedUntil, until);
    }
}
