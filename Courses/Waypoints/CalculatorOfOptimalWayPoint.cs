using SheepHerderAI.Configuration;
using SheepHerderAI.Utilities;
using SheepHerderAI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Linq.Expressions;

namespace SheepHerderUserHeuristicAI.Courses.Waypoints
{
    /// <summary>
    /// Attempt a more optimal way point derivation method, moving in a direct line.
    /// </summary>
    internal class CalculatorOfMostDirectWayPoint : IWayPointCalculator
    {
        /// <summary>
        /// Sets nextWayPointToHeadTo.
        /// </summary>
        /// <param name="centreOfMass"></param>
        public int GetClosestWayPointForwards(PointF centreOfMass, int currentWayPoint)
        {
            // Determine the *furthest* waypoint based on where the centre of mass is (without going thru a wall). 
            // This means it takes a short-cut where possible, rather than following dots.

            int closestWayPointByIndex = -1;
            float closestDistanceToWayPoint = -1;
            // evaluate all check points close to the current one.
            int minWayPointIndex = Math.Max(currentWayPoint, 0);
            int maxWayPointIndex = LearnToHerd.s_wayPointsSheepNeedsToGoThru.Length;

            for (int indexOfWayPoints = minWayPointIndex; indexOfWayPoints < maxWayPointIndex; indexOfWayPoints++)
            {
                // check one by one, trying to find the furthest in range
                Point wayPointForIndex = LearnToHerd.s_wayPointsSheepNeedsToGoThru[indexOfWayPoints];

                float distanceFromCenterOfMassToWayPoint = MathUtils.DistanceBetweenTwoPoints(wayPointForIndex, centreOfMass);

                if (!RouteHasFencesInBetweenFlockAndWayPoint(centreOfMass, wayPointForIndex) && distanceFromCenterOfMassToWayPoint > closestDistanceToWayPoint)
                {
                    closestWayPointByIndex = indexOfWayPoints;
                    closestDistanceToWayPoint = distanceFromCenterOfMassToWayPoint;
                }
            }

            if (closestWayPointByIndex > 0) currentWayPoint = closestWayPointByIndex;

            if (currentWayPoint >= LearnToHerd.s_wayPointsSheepNeedsToGoThru.Length) --currentWayPoint;

            return currentWayPoint;
        }

        /// <summary>
        /// Check the fences, to see if the line that the fence is declared using intersects with a line between flock COM and waypoint.
        /// </summary>
        /// <param name="flockCentreOfMass"></param>
        /// <param name="wayPoint"></param>
        /// <returns></returns>
        private static bool RouteHasFencesInBetweenFlockAndWayPoint(PointF flockCentreOfMass, PointF wayPoint)
        {
            /*                         x wayPoint
             *                        .
             *                       .
             *                    =============== fence
             *                     .
             *                    .
             *                   .
             *                  o flock CoM
             */

            foreach (PointF[] points in LearnToHerd.s_lines)
            {
                // s_lines is an array of *joined* points (that we draw lines between). We thus have
                // to take one line at a time and check..
                for (int i = 0; i < points.Length - 1; i++) // -1, because we're doing line "i" to "i+1"
                {
                    PointF point1 = points[i];
                    PointF point2 = points[i + 1];

                    // will the sheep need to go thru a wall to get to the wayPoint?
                    if (MathUtils.GetLineIntersection(point1, point2, flockCentreOfMass, wayPoint, out _ /* we don't care *where* it intersects */))
                    {
                        return true; // lines intersect
                    }
                }
            }

            return false;
        }

    }
}
