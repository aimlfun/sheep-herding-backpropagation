namespace SheepHerderUserHeuristicAI.Courses.Waypoints;

/// <summary>
/// Interface so that one can try out different methods for getting the next way point.
/// </summary>
public interface IWayPointCalculator
{
    /// <summary>
    /// Returns the "next" way point to head to.
    /// </summary>
    /// <param name="centreOfMass"></param>
    /// <param name="currentWayPoint"></param>
    /// <returns></returns>
    public int GetClosestWayPointForwards(PointF centreOfMass, int currentWayPoint);
}
