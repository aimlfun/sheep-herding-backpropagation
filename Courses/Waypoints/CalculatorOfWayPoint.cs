using SheepHerderAI.Configuration;
using SheepHerderAI.Utilities;
using SheepHerderAI;

namespace SheepHerderUserHeuristicAI.Courses.Waypoints
{
    /// <summary>
    /// Follow the dots waypoint calculation.
    /// </summary>
    internal class CalculatorOfWayPoint : IWayPointCalculator
    {
        /// <summary>
        /// Sets nextWayPointToHeadTo.
        /// </summary>
        /// <param name="centreOfMass"></param>
        public int GetClosestWayPointForwards(PointF centreOfMass, int currentWayPoint)
        {
            // determine furthest way point based on where the centre of mass is

            int closestWayPointByIndex = -1;

            float closestDistanceToWayPoint = -int.MaxValue; // further than the sheep can walk
                                                             // evaluate all check points close to the current one.

            int minWayPointIndex = Math.Max(currentWayPoint, 0);
            int maxWayPointIndex = Math.Min(currentWayPoint + 4, LearnToHerd.s_wayPointsSheepNeedsToGoThru.Length);

            for (int indexOfWayPoints = minWayPointIndex; indexOfWayPoints < maxWayPointIndex; indexOfWayPoints++)
            {
                // check one by one, trying to find the furthest in range
                Point wayPointForIndex = LearnToHerd.s_wayPointsSheepNeedsToGoThru[indexOfWayPoints];

                float distanceFromCenterOfMassToWayPoint = MathUtils.DistanceBetweenTwoPoints(wayPointForIndex, centreOfMass);

                // head for furthest reachable in range
                if (distanceFromCenterOfMassToWayPoint < Config.SheepClosenessToMoveToNextWayPoint && distanceFromCenterOfMassToWayPoint > closestDistanceToWayPoint)
                {
                    closestWayPointByIndex = indexOfWayPoints;
                    closestDistanceToWayPoint = distanceFromCenterOfMassToWayPoint;
                }
            }

            if (closestWayPointByIndex > 0) currentWayPoint = closestWayPointByIndex;

            if (currentWayPoint >= LearnToHerd.s_wayPointsSheepNeedsToGoThru.Length) --currentWayPoint;

            return currentWayPoint;
        }
    }
}
