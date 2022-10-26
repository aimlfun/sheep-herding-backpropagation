#define randomSheep
using SheepHerderAI.Configuration;
using SheepHerderAI.Predator;
using SheepHerderAI.Utilities;
using SheepHerderUserHeuristicAI.Courses.Waypoints;
using System.Drawing.Drawing2D;

namespace SheepHerderAI.Sheepies;

/// <summary>
/// Class represents a flock of "sheep". 
/// 
/// Flocking: https://en.wikipedia.org/wiki/Flocking_(behavior)
/// 
/// "Flocking" is the collective motion by a group of self-propelled entities and is a collective animal behavior 
/// exhibited by many living beings such as birds, fish, bacteria, and insects.
/// It is considered an emergent behavior arising from simple rules that are followed by individuals and does not 
/// involve any central coordination.
///  
/// https://vergenet.net/~conrad/boids/pseudocode.html article helped a lot to become proficient at flocking.
/// </summary>
internal class Flock
{
    //   _____ _            _    
    //  |  ___| | ___   ___| | __
    //  | |_  | |/ _ \ / __| |/ /
    //  |  _| | | (_) | (__|   < 
    //  |_|   |_|\___/ \___|_|\_\
   
    internal PointF DesiredLocation = new();

    /// <summary>
    /// How many sheep are in the scoring zone (the box top right
    /// </summary>
    internal int numberOfSheepInPenZone = 0;

    /// <summary>
    /// Indirect link to dog and its brain. This is the "Id" of the neural network.
    /// </summary>
    internal readonly int Id;

    /// <summary>
    /// Coloured sheep? A bit too much dye.
    /// </summary>
    internal Color sheepColour = Color.Pink;

    /// <summary>
    /// Horizontal confines of the sheep pen in pixel.
    /// </summary>
    internal int SheepPenWidth;

    /// <summary>
    /// Vertical confines of the sheep pen in pixel.
    /// </summary>
    internal int SheepPenHeight;

    /// <summary>
    /// Represents the flock of sheep.
    /// </summary>
    internal readonly List<Sheep> flock = new();

    /// <summary>
    /// The dog chasing the flock.
    /// </summary>
    internal readonly Dog dog;

    /// <summary>
    /// Next location we want the flock to go towards.
    /// </summary>
    private int nextWayPointToHeadTo = 0;

    /// <summary>
    /// A class to choose the next way point. We do this way so we can replace 
    /// </summary>
    private readonly IWayPointCalculator WayPointCalculator = new CalculatorOfMostDirectWayPoint();

    /// <summary>
    /// Next location we want the flock to go towards.
    /// But tracks "time" it was set, so we can ensure generations progress or halt.
    /// </summary>
    internal int NextWayPointToHeadTo
    {
        get
        {
            return nextWayPointToHeadTo;
        }

        set
        {
            if (nextWayPointToHeadTo == value) return;

            nextWayPointToHeadTo = value;
        }
    }

    /// <summary>
    /// Failed to go towards way point - make the flock in "failed" state (doesn't move).
    /// </summary>
    internal bool flockIsFailure = false;

    /// <summary>
    /// Used to show WHY this flock was terminated early.
    /// </summary>
    internal string failureReason = "RUNNING";

    /// <summary>
    /// Constructor: Creates a flock of sheep.
    /// </summary>
    internal Flock(int id, List<PointF> sheepPositions, int width, int height)
    {
        Id = id;
        SheepPenWidth = width;
        SheepPenHeight = height;

        // random
        sheepColour = Color.White;

#if randomSheep
        // add the required number of sheep
        for (int i = 0; i < Config.InitialFlockSize; i++)
        {
            flock.Add(new Sheep(this, sheepPositions[i]));
        }
#else
        flock.Add(new Sheep(this, new PointF(31, 62)));        
        flock.Add(new Sheep(this, new PointF(52, 79)));
        flock.Add(new Sheep(this, new PointF(21, 60)));
        flock.Add(new Sheep(this, new PointF(29, 75)));
        flock.Add(new Sheep(this, new PointF(57, 44)));
        flock.Add(new Sheep(this, new PointF(28, 50)));
        flock.Add(new Sheep(this, new PointF(36, 50)));
        flock.Add(new Sheep(this, new PointF(30, 68)));
        flock.Add(new Sheep(this, new PointF(41, 71)));
        flock.Add(new Sheep(this, new PointF(61, 52)));
        flock.Add(new Sheep(this, new PointF(49, 84)));
        flock.Add(new Sheep(this, new PointF(34, 43)));
        flock.Add(new Sheep(this, new PointF(51, 60)));
        flock.Add(new Sheep(this, new PointF(43, 51)));
        flock.Add(new Sheep(this, new PointF(60, 46)));
        flock.Add(new Sheep(this, new PointF(16, 45)));
        flock.Add(new Sheep(this, new PointF(39, 45)));
        flock.Add(new Sheep(this, new PointF(42, 75)));
        flock.Add(new Sheep(this, new PointF(40, 79)));
        flock.Add(new Sheep(this, new PointF(60, 62)));        
#endif

        dog = new Dog(id, this);
    }

    /// <summary>
    /// Computes center of ALL sheep except this one.
    /// </summary>
    internal PointF CenterOfMassExcludingThisSheep(Sheep thisSheep)
    {
        // center of nothing = nothing
        if (flock.Count < 2) return new PointF(thisSheep.Position.X, thisSheep.Position.Y);

        // compute center
        float x = 0;
        float y = 0;

        foreach (Sheep sheep in flock)
        {
            if (sheep == thisSheep) continue; // center of mass excludes this sheep

            x += sheep.Position.X;
            y += sheep.Position.Y;
        }

        // we exclude "thisSheep", so average is N-1.
        int sheepSummed = flock.Count - 1;

        return new PointF(x / sheepSummed, y / sheepSummed);
    }

    /// <summary>
    /// Returns angle sheep need to go to, to be at next waypoint.
    /// Called everytime we move the predator to know where we
    /// require the sheep to go (feeds as input).
    /// </summary>
    /// <returns></returns>
    internal double AngleToNextWayPointInRadians()
    {

        PointF centreOfMass = TrueCentreOfMass();
        PointF wayPointToHeadTowards;

        AdjustClosestWayPointForwards(centreOfMass);

        if (LearnToHerd.s_wayPointsSheepNeedsToGoThru.Length == 0)
        {
            if (LearnToHerd.s_wayPointsSheepNeedsToGoThru.Length == 0)
            {
                return Math.Atan2((DesiredLocation.Y - centreOfMass.Y),
                                   (DesiredLocation.X - centreOfMass.X));

            }
        }
        
        wayPointToHeadTowards = LearnToHerd.s_wayPointsSheepNeedsToGoThru[NextWayPointToHeadTo];
        
        double angle = Math.Atan2((wayPointToHeadTowards.Y - centreOfMass.Y),
                                   (wayPointToHeadTowards.X - centreOfMass.X));

        return angle;
    }

    /// <summary>
    /// Sets nextWayPointToHeadTo.
    /// </summary>
    /// <param name="centreOfMass"></param>
    private void AdjustClosestWayPointForwards(PointF centreOfMass)
    {
        NextWayPointToHeadTo = WayPointCalculator.GetClosestWayPointForwards(centreOfMass, NextWayPointToHeadTo);
    }

    /// <summary>
    /// Center of mass of ALL the sheep (not excluding one)
    /// </summary>
    /// <returns></returns>
    internal PointF TrueCentreOfMass()
    {
        // compute center using average of x & y
        float x = 0;
        float y = 0;

        foreach (Sheep sheep in flock)
        {
            if (MathUtils.DistanceBetweenTwoPoints(sheep.Position, dog.Position) > 150) continue; // close
            x += sheep.Position.X;
            y += sheep.Position.Y;
        }

        // centre of ALL sheep
        return new(x / flock.Count, y / flock.Count);
    }
  
    /// <summary>
    /// Prevent sheep moving outside the sheep pen / off-screen.
    /// </summary>
    /// <param name="thisSheep"></param>
    /// <returns></returns>
    internal PointF EnforceBoundary(Sheep thisSheep)
    {
        PointF p = new(0, 0);

        if (thisSheep.Position.X < 5) p.X = 4;

        if (thisSheep.Position.X > SheepPenWidth - 5) p.X = -4;

        if (thisSheep.Position.Y < 5) p.Y = 4;

        if (thisSheep.Position.Y > SheepPenHeight - 5) p.Y = -4;

        return p;
    }

#region SHEEP RULES OF MOVEMENT
    /// <summary>
    /// Rule 1: Sheep like to move towards the centre of mass of flock.
    /// </summary>
    /// <param name="thisSheep"></param>
    /// <returns></returns>
    internal PointF MoveTowardsCentreOfMass(Sheep thisSheep)
    {
        //      _  _   _    ____      _               _                __    _   _                  _   _             
        //    _| || |_/ |  / ___|___ | |__   ___  ___(_) ___  _ __    / /_ _| |_| |_ _ __ __ _  ___| |_(_) ___  _ __  
        //   |_  ..  _| | | |   / _ \| '_ \ / _ \/ __| |/ _ \| '_ \  / / _` | __| __| '__/ _` |/ __| __| |/ _ \| '_ \ 
        //   |_      _| | | |__| (_) | | | |  __/\__ \ | (_) | | | |/ / (_| | |_| |_| | | (_| | (__| |_| | (_) | | | |
        //     |_||_| |_|  \____\___/|_| |_|\___||___/_|\___/|_| |_/_/ \__,_|\__|\__|_|  \__,_|\___|\__|_|\___/|_| |_|
        //                                                                                                            
        // Cohesion: Steer towards average position of neighbours (long range attraction)

        PointF pointF = CenterOfMassExcludingThisSheep(thisSheep);

        // move it 1% of the way towards the centre
        return new PointF((pointF.X - thisSheep.Position.X) / 100,
                          (pointF.Y - thisSheep.Position.Y) / 100);
    }

    /// <summary>
    /// Rule 2: Try to keep a small distance away from other sheep (short range repulsion), or walls.
    /// </summary>
    /// <param name="thisSheep"></param>
    /// <returns></returns>
    internal PointF MaintainSeparation(Sheep thisSheep)
    {
        //      _  _  ____       _             _     _                             _ _             
        //    _| || ||___ \     / \__   _____ (_) __| |   ___ _ __ _____      ____| (_)_ __   __ _ 
        //   |_  ..  _|__) |   / _ \ \ / / _ \| |/ _` |  / __| '__/ _ \ \ /\ / / _` | | '_ \ / _` |
        //   |_      _/ __/   / ___ \ V / (_) | | (_| | | (__| | | (_) \ V  V / (_| | | | | | (_| |
        //     |_||_||_____| /_/   \_\_/ \___/|_|\__,_|  \___|_|  \___/ \_/\_/ \__,_|_|_| |_|\__, |
        //                                                                                   |___/ 
        // Separation: avoid crowding neighbours (short range repulsion)

        // The purpose of this rule is to for sheep to ensure they don't collide into each other.

        // We simply look at each sheep, and if it's within a defined small distance (say 6 pixels) of
        // another sheep move it as far away again as it already is. This is done by subtracting from a
        // vector c the displacement of each boid which is near by.
        
        const float c_size = 6;

        // We initialise to zero as we want this rule to give us a vector which when added to the
        // current position moves a sheep away from those near it.
        PointF separationVector = new(0, 0);

        // collision with another sheep?
        foreach (Sheep sheep in flock)
        {
            if (sheep == thisSheep) continue; // excludes this sheep, you can't collide with yourself!

            if (MathUtils.DistanceBetweenTwoPoints(sheep.Position, thisSheep.Position) < c_size) // too close?
            {
                separationVector.X -= sheep.Position.X - thisSheep.Position.X;
                separationVector.Y -= sheep.Position.Y - thisSheep.Position.Y;
            }
        }
        

        // collision with any walls? (contiguou points treated as pairs of points)
        foreach (PointF[] points in LearnToHerd.s_lines)
        {
            for (int i = 0; i < points.Length - 1; i++) // -1, because we're doing line "i" to "i+1"
            {
                PointF point1 = points[i];
                PointF point2 = points[i + 1];

                // touched wall? returns the closest point on the line to the sheep. We check the distance
                if (MathUtils.IsOnLine(point1, point2, new PointF(thisSheep.Position.X + separationVector.X, thisSheep.Position.Y + separationVector.Y), out PointF closest) 
                    && MathUtils.DistanceBetweenTwoPoints(closest, thisSheep.Position) < 9)
                {
                    // yes, need to back off from the wall
                    separationVector.X -= 2.5f * (closest.X - thisSheep.Position.X);
                    separationVector.Y -= 2.5f * (closest.Y - thisSheep.Position.Y);
                }
            }
        }        

        return separationVector; // how much to separate this sheep
    }

    /// <summary>
    /// Rule 3: sheep try to match velocity vector with nearby sheep.
    /// </summary>
    /// <param name="thisSheep"></param>
    /// <returns></returns>
    internal PointF MatchVelocityOfNearbySheep(Sheep thisSheep)
    {
        //      _  _  _____      _    _ _               _                     _       _     _                          
        //    _| || ||___ /     / \  | (_) __ _ _ __   | |_ ___    _ __   ___(_) __ _| |__ | |__   ___  _   _ _ __ ___ 
        //   |_  ..  _||_ \    / _ \ | | |/ _` | '_ \  | __/ _ \  | '_ \ / _ \ |/ _` | '_ \| '_ \ / _ \| | | | '__/ __|
        //   |_      _|__) |  / ___ \| | | (_| | | | | | || (_) | | | | |  __/ | (_| | | | | |_) | (_) | |_| | |  \__ \
        //     |_||_||____/  /_/   \_\_|_|\__, |_| |_|  \__\___/  |_| |_|\___|_|\__, |_| |_|_.__/ \___/ \__,_|_|  |___/
        //                                |___/                                 |___/                                  
        // Alignment: steer towards average heading of neighbours

        // This is similar to Rule 1, however instead of averaging the positions of the other boids
        // we average the velocities. We calculate a 'perceived velocity', pvJ, then add a small portion
        // (about an eighth) to the boid's current velocity.

        PointF c = new(0, 0);
        int countSheep = 0;

        foreach (Sheep sheep in flock)
        {
            if (sheep == thisSheep) continue; // excludes this sheep

            /* The alignment rule is calculated for each sheep s. Each sheep si within a radius of
                50 pixels has a velocity siv that contributes equally to the final rule vector. The size
                of the rule vector is determined by the velocity of all nearby sheep N. The vector is
                directed in the average direction of the nearby sheep. The rule vector is calculated
                with the function .
            */

            if (MathUtils.DistanceBetweenTwoPoints(sheep.Position, thisSheep.Position) > Config.SheepCloseEnoughToBeAMass) continue;

            ++countSheep;

            c.X += sheep.Velocity.X;
            c.Y += sheep.Velocity.Y;
        }

        if (countSheep > 0)
        {

            c.X /= countSheep;
            c.Y /= countSheep;
        }

        return new PointF((c.X - thisSheep.Velocity.X) / 8, (c.Y - thisSheep.Velocity.Y) / 8);
    }

    /// <summary>
    /// Inverse Square Function
    /// In two of the rules, Separation and Escape, nearby objects are prioritized higher than
    /// those further away. This prioritization is described by an inverse square function.
    /// </summary>
    /// <param name="x">is the distance between the objects</param>
    /// <param name="s">s is a softness factor that slows down the rapid decrease of the function value | s = 1 for Separation and s = 10 for Escape.</param>
    /// <returns></returns>
    internal static float InverseSquare(float x, float s)
    {
        float e = 0.0000000001f; // is a small value used to avoid division by zero, when x = 0.
        return (float)Math.Pow(x / (s + e), -2);
    }

    /// <summary>
    /// Tries to provide a vector to escape the dog.
    /// </summary>
    /// <param name="sheep"></param>
    /// <param name="dog"></param>
    /// <returns></returns>
    internal static PointF EscapeFromTheDog(Sheep sheep, PointF dog, float softness = 10)
    {
        //      _  _   _  _        _             _     _   ____               _       _                 
        //    _| || |_| || |      / \__   _____ (_) __| | |  _ \ _ __ ___  __| | __ _| |_ ___  _ __ ___ 
        //   |_  ..  _| || |_    / _ \ \ / / _ \| |/ _` | | |_) | '__/ _ \/ _` |/ _` | __/ _ \| '__/ __|
        //   |_      _|__   _|  / ___ \ V / (_) | | (_| | |  __/| | |  __/ (_| | (_| | || (_) | |  \__ \
        //     |_||_|    |_|   /_/   \_\_/ \___/|_|\__,_| |_|   |_|  \___|\__,_|\__,_|\__\___/|_|  |___/
        //                                                                                                      
        // Escape: make them move away from the dog

        float x = sheep.Position.X - dog.X;
        float y = sheep.Position.Y - dog.Y;

        float distToPredator = (float)Math.Sqrt(Math.Pow(x, 2) + Math.Pow(y, 2));

        return new(x / distToPredator * InverseSquare(distToPredator, softness),
                   y / distToPredator * InverseSquare(distToPredator, softness));
    }

    #endregion

    /// <summary>
    /// Moves and draws the flock of sheep.
    /// </summary>
    internal void Move()
    {
        if (flockIsFailure) return;

        numberOfSheepInPenZone = 0;

        // move them all, using Reynolds swarm mathematics
        foreach (Sheep s in flock)
        {
            s.Move();

            if (LearnToHerd.s_sheepPenScoringZone.Contains(s.Position)) ++numberOfSheepInPenZone;
        }

        if (LearnToHerd.s_wayPointsSheepNeedsToGoThru.Length == 0) return; // we don't want to move the dog, because this is a debug situation

        dog.Move();
    }

    /// <summary>
    /// Draws the sheep and predator.
    /// </summary>
    /// <param name="graphics"></param>
    internal void Draw(Graphics graphics)
    {
        ColourInTheWayPointsTheSheepHaveGoneCloseTo(graphics);
        
        // compute center of all sheep whilst drawing the sheep

        float x = 0;
        float y = 0;

        // draw each sheep
        foreach (Sheep sheep in flock)
        {
            sheep.Draw(graphics, (flockIsFailure ? Color.FromArgb(50, 255, 255, 255) : sheepColour));

            x += sheep.Position.X;
            y += sheep.Position.Y;
        }

        // calculate centre of ALL sheep (known as center of mass)
        PointF centerOfMass = new(x / flock.Count, y / flock.Count);
        
        DrawXatCenterOfMass(graphics, centerOfMass); 
        DrawCircleAroundCenterOfMass(graphics, centerOfMass);
        DrawPointerToNextWayPointFromCenterOfMass(graphics, centerOfMass);
        
        dog.Draw(graphics);
    }

    /// <summary>
    /// Debugging requires us to know where the center of mass is. So we draw a red "X".
    /// </summary>
    /// <param name="g"></param>
    /// <param name="centerOfMass"></param>
    private static void DrawXatCenterOfMass(Graphics g, PointF centerOfMass)
    {
        // x marks the spot for center of mass
        g.DrawLine(Pens.Red, centerOfMass.X - 4, centerOfMass.Y - 4, centerOfMass.X + 4, centerOfMass.Y + 4);
        g.DrawLine(Pens.Red, centerOfMass.X - 4, centerOfMass.Y + 4, centerOfMass.X + 4, centerOfMass.Y - 4);
    }

    /// <summary>
    /// Way points are faint blobs. As the sheep go near the blobs we register it by
    /// drawing them in a lighter colour.
    /// </summary>
    /// <param name="g"></param>
    private void ColourInTheWayPointsTheSheepHaveGoneCloseTo(Graphics g)
    {
        using SolidBrush wayPointBrush = new(Color.FromArgb(255, 120, 255, 120));

        for (int i = 0; i < NextWayPointToHeadTo; i++)
        {
            Point wp = LearnToHerd.s_wayPointsSheepNeedsToGoThru[i];

            g.FillEllipse(wayPointBrush, wp.X - 3, wp.Y - 3, 6, 6);
        }
    }

    /// <summary>
    /// Having a circle helps us know where the center of mass ends.
    /// </summary>
    /// <param name="g"></param>
    /// <param name="centerOfMass"></param>
    private static void DrawCircleAroundCenterOfMass(Graphics g, PointF centerOfMass)
    {
        // draw circle around the CoM
        using Pen p = new(Color.FromArgb(50, 255, 50, 50));

        p.DashStyle = DashStyle.Dash;
        g.DrawEllipse(p, new RectangleF(centerOfMass.X - (float)Config.SheepClosenessToMoveToNextWayPoint,
                                        centerOfMass.Y - (float)Config.SheepClosenessToMoveToNextWayPoint,
                                        (float)Config.SheepClosenessToMoveToNextWayPoint * 2,
                                        (float)Config.SheepClosenessToMoveToNextWayPoint * 2));
    }

    /// <summary>
    /// Pointer to next way point (line with arrow head) from the center of mass.
    /// </summary>
    /// <param name="g"></param>
    /// <param name="centerOfMass"></param>
    /// <param name="wayPointToHeadTowards"></param>
    private void DrawPointerToNextWayPointFromCenterOfMass(Graphics g, PointF centerOfMass)
    {
        if (LearnToHerd.s_wayPointsSheepNeedsToGoThru.Length == 0) return;

        PointF wayPointToHeadTowards = LearnToHerd.s_wayPointsSheepNeedsToGoThru[NextWayPointToHeadTo];
    
        // we have 2 points. ArcTan gives us the angle.
        double angle = Math.Atan2((wayPointToHeadTowards.Y - centerOfMass.Y),
                                   (wayPointToHeadTowards.X - centerOfMass.X));

        using Pen p2 = new(Color.Black)
        {
            DashStyle = DashStyle.Dot,
            EndCap = LineCap.ArrowAnchor
        };

        // rotate at the angle, origin of the com.
        g.DrawLine(p2, (int)centerOfMass.X, (int)centerOfMass.Y, (int)(30 * Math.Cos(angle) + centerOfMass.X), (int)(30 * Math.Sin(angle) + centerOfMass.Y));        
    }

    /// <summary>
    /// Determine how well the sheep have been herded. 
    /// Larger number = better. 
    /// Max score = # sheep * Width of Pen * Height of Pen. 
    /// 
    /// There are lots of ways you could score. We've gone with a "desired" path made of way points and score based on how many
    /// way points they passed. Reaching the pen rewards them handsomely (9000 points per sheep).
    /// </summary>
    /// <returns></returns>
    internal float FitnessScore()
    {
        float score = 0;

        foreach (Sheep s in flock)
        {
            score += LearnToHerd.s_sheepPenScoringZone.Contains(s.Position)
                        ? SheepPenWidth * SheepPenHeight // a lot higher than us gained from mere way points
                        : NextWayPointToHeadTo * 10;
        }

        score /= flock.Count; // average score for the flock

        return score;
    }
}