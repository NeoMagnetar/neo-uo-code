using System;
using System.Collections.Generic;
using Server;

namespace Server.Custom.AIGM
{
    public sealed class AIGMTravelBreadcrumb
    {
        public Point3D Location { get; set; }
        public DateTime Utc { get; set; }
        public int DistanceToGoal { get; set; }
    }

    public sealed class AIGMFailedPathPoint
    {
        public Point3D Location { get; set; }
        public DateTime Utc { get; set; }
        public string Reason { get; set; }
        public int Count { get; set; }
    }

    public sealed class AIGMTravelMemory
    {
        public Queue<AIGMTravelBreadcrumb> Breadcrumbs { get; private set; }
        public List<AIGMFailedPathPoint> FailedPoints { get; private set; }

        public AIGMTravelMemory()
        {
            Breadcrumbs = new Queue<AIGMTravelBreadcrumb>();
            FailedPoints = new List<AIGMFailedPathPoint>();
        }

        public void RecordBreadcrumb(Point3D location, int distanceToGoal)
        {
            Breadcrumbs.Enqueue(new AIGMTravelBreadcrumb
            {
                Location = location,
                Utc = DateTime.UtcNow,
                DistanceToGoal = distanceToGoal
            });

            while (Breadcrumbs.Count > 32)
                Breadcrumbs.Dequeue();
        }

        public void RecordFailure(Point3D location, string reason)
        {
            for (int i = 0; i < FailedPoints.Count; i++)
            {
                if (Utility.InRange(FailedPoints[i].Location, location, 2))
                {
                    FailedPoints[i].Utc = DateTime.UtcNow;
                    FailedPoints[i].Reason = reason;
                    FailedPoints[i].Count++;
                    return;
                }
            }

            FailedPoints.Add(new AIGMFailedPathPoint
            {
                Location = location,
                Utc = DateTime.UtcNow,
                Reason = reason,
                Count = 1
            });

            while (FailedPoints.Count > 16)
                FailedPoints.RemoveAt(0);
        }

        public bool IsNearFailedPoint(Point3D location, int range)
        {
            for (int i = 0; i < FailedPoints.Count; i++)
            {
                if (Utility.InRange(FailedPoints[i].Location, location, range))
                    return true;
            }

            return false;
        }

        public Point3D GetBacktrackPoint(Point3D fallback)
        {
            if (Breadcrumbs.Count == 0)
                return fallback;

            AIGMTravelBreadcrumb[] points = Breadcrumbs.ToArray();
            int index = Math.Max(0, points.Length - 12);
            return points[index].Location;
        }

        public bool IsOscillating(Point3D location)
        {
            if (Breadcrumbs.Count < 8)
                return false;

            int revisits = 0;
            foreach (AIGMTravelBreadcrumb crumb in Breadcrumbs)
            {
                if (Utility.InRange(crumb.Location, location, 1))
                    revisits++;
            }

            return revisits >= 4;
        }
    }
}
