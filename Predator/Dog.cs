using Microsoft.VisualStudio.TestPlatform.Utilities;
using SheepHerderAI.AI;
using SheepHerderAI.Configuration;
using SheepHerderAI.Sheepies;
using SheepHerderAI.Utilities;
using System.Diagnostics;
using System.Drawing.Drawing2D;

namespace SheepHerderAI.Predator;

/// <summary>
/// 
/// </summary>
internal class Dog
{

    //   ____              
    //  |  _ \  ___   __ _ 
    //  | | | |/ _ \ / _` |
    //  | |_| | (_) | (_| |
    //  |____/ \___/ \__, |
    //               |___/ 

    /// <summary>
    /// Used to behave differently for each dog (Id 0 = user, 1 = heuristics, 2 = AI). 
    /// </summary>
    readonly private int Id;

    /// <summary>
    /// Which flock of sheep the dog is herding (given multiple dogs, and hence flocks of sheep).
    /// </summary>
    readonly Flock flockBeingHerded;

    /// <summary>
    /// Where the dog is located.
    /// </summary>
    internal PointF Position = new(10, 10);

    /// <summary>
    /// Where we want the dog to go (human mouse position, computed via heuristic or AI).
    /// </summary>
    internal PointF DesiredPosition = new(10, 10);

    /// <summary>
    /// The direction the dog is facing.
    /// </summary>
    internal float AngleDogIsFacingInDegrees = 0;

    /// <summary>
    /// How fast the dog is moving.
    /// </summary>
    internal float Speed = 0;

    /// <summary>
    /// 
    /// </summary>
    static StreamWriter? swUser = null;

    /// <summary>
    /// 
    /// </summary>
    static StreamWriter? swAI = null;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="flockItbelongsTo"></param>
    internal Dog(int id, Flock flockItbelongsTo)
    {
        Id = id;
        flockBeingHerded = flockItbelongsTo;

        // reset cursor at start
        if (Id == 0)
        {
            swUser?.Close();
            swAI?.Close();

            swUser = InitialiseLogStreamWriter($@"c:\temp\training-{LearnToHerd.s_generation}.dat");
            swAI = InitialiseLogStreamWriter($@"c:\temp\play-{LearnToHerd.s_generation}.dat");

            Cursor.Position = new Point((int)DesiredPosition.X, (int)DesiredPosition.Y);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="filename"></param>
    /// <returns></returns>
    private StreamWriter InitialiseLogStreamWriter(string filename)
    {
        StreamWriter sw = new(filename)
        {
            AutoFlush = true
        };

        sw.WriteLine($"# Generation: {LearnToHerd.s_generation}");
        sw.WriteLine($"DOG: {Position}");

        foreach (Sheep sheep in flockBeingHerded.flock)
        {
            sw.WriteLine($"SHEEP: {sheep.Position}");
        }

        return sw;
    }

    /// <summary>
    /// Move the dog. 
    /// </summary>
    internal void Move()
    {
        List<double> inputToAI = InputsToAIorHeuristicsEngine();

        switch (Id)
        {
            // user: mouse event will have set the "DesiredPosition" xy.
            case 0:
                break;

            // heuristics decides where the dog needs to head (not AI)
            case 1:
                SetDesiredPositionByUsingHeuristicsToSteerTheDog(inputToAI);
                break;

            // AI decides where the dog needs to head
            case 2:
                SetDesiredPositionByUsingAItoSteerTheDog(inputToAI);
                break;
        }

        SetAngleAndSpeedBasedOnDesiredPosition();

        // Sin/Cos use radians, so we need to convert
        float angleInRADIANS = (float)MathUtils.DegreesInRadians(AngleDogIsFacingInDegrees);

        // predator moves towards the chosen angle, at the chosen speed.
        Position.X += Speed * (float)Math.Cos(angleInRADIANS);
        Position.Y += Speed * (float)Math.Sin(angleInRADIANS);

        PreventDogJumpingFences();

        // ensure they don't go outside the UI area.
        Position.X = Position.X.Clamp(3, LearnToHerd.s_sizeOfPlayingField.Width - 3);
        Position.Y = Position.Y.Clamp(3, LearnToHerd.s_sizeOfPlayingField.Height - 3);

        // human points records training data
        if (Id == 0)
        {
            PointF centreOfMass = flockBeingHerded.TrueCentreOfMass();

            StoreHumanCapturedTrainingDataAndSteerDog(inputToAI, new double[] { DesiredPosition.X - centreOfMass.X, DesiredPosition.Y - centreOfMass.Y });// angle is delta, speed is exact
        }
    }

    /// <summary>
    /// All 3 control mechanisms set DesiredPosition (where dog needs to be).
    /// 
    /// We now need to work out how the dog should respond - which way to rotate, what speed to travel.
    /// </summary>
    private void SetAngleAndSpeedBasedOnDesiredPosition()
    {
        float angleInDegrees = (float)MathUtils.RadiansInDegrees((float)Math.Atan2(DesiredPosition.Y - Position.Y, DesiredPosition.X - Position.X));

        float deltaAngle = Math.Abs(angleInDegrees - AngleDogIsFacingInDegrees).Clamp(0, 30);

        // quickest way to get from current angle to new angle turning the optimal direction
        float angleInOptimalDirection = ((angleInDegrees - AngleDogIsFacingInDegrees + 540f) % 360) - 180f;

        // limit max of 30 degrees
        AngleDogIsFacingInDegrees = MathUtils.Clamp360(AngleDogIsFacingInDegrees + deltaAngle * Math.Sign(angleInOptimalDirection));

        // close the distance as quickly as possible but without the dog going faster than it should
        Speed = MathUtils.DistanceBetweenTwoPoints(Position, DesiredPosition).Clamp(-Config.DogMaximumVelocityInAnyDirection, Config.DogMaximumVelocityInAnyDirection);
    }

    /// <summary>
    /// Uses a neural network to move the dog.
    /// </summary>
    /// <param name="inputToAI"></param>
    private void SetDesiredPositionByUsingAItoSteerTheDog(List<double> inputToAI)
    {
        // ask the AI what to do next? inputs[] => feedforward => outputs[],
        // [0] = x offset from centreOfMass to move dog to [1] = y offset from centreOfMass to move dog to

        // the underlying calculation requires sin/cos, tanh, closest point on a line, so instead we train the AI
        // where the dog should go to steeer the flock in the intended direction. It has to use offsets otherwise
        // the training would be specific to a course and exact location of the sheep and dog.
        double[] output = NeuralNetwork.s_networks[2].FeedForward(inputToAI.ToArray());

        PointF centreOfMass = flockBeingHerded.TrueCentreOfMass();

        DesiredPosition.X = (float)(centreOfMass.X + (output[0] * LearnToHerd.s_sizeOfPlayingField.Width));
        DesiredPosition.Y = (float)(centreOfMass.Y + (output[1] * LearnToHerd.s_sizeOfPlayingField.Height));
    }

    /// <summary>
    /// Apply heuristics to work out what angle to move the dog, and how quickly.
    /// </summary>
    /// <param name="inputToAI"></param>
    private void SetDesiredPositionByUsingHeuristicsToSteerTheDog(List<double> inputToAI)
    {
        /*
         *     + backwardsPointForLine
         *      \
         *       \
         *        \  
         *         x closestPointOnLine
         *    o     \
         *   dog     \
         *            \ 
         *             + desiredPointAtTheEdgeOfSheepCircle  
         *              \o
         *   sheep ->  o x o________  
         *               o.     |
         *                 .   /  2*PI-AngleToNextWayPointInRadians()
         *                  .--
         *                   .
         *                    .
         *                     O 
         *                   waypoint
         */

        PointF centreOfMass = flockBeingHerded.TrueCentreOfMass();

        float arc = (int)Config.DogSensorOfSheepVisionDepthOfVisionInPixels;

        double desiredAngleInRadians = inputToAI[2] * Math.PI - Math.PI; // [2] is scaled with "/ Math.PI" to make -1..1, so we reverse that. We then need to rotate 90 degrees

        float cosDesiredAngle = (float)Math.Cos(desiredAngleInRadians);
        float sinDesiredAngle = (float)Math.Sin(desiredAngleInRadians);

        float radius = ClosestDogMayIntentionallyGetToSheepMass();
        PointF desiredPointAtTheEdgeOfSheepCircle = new((float)(centreOfMass.X + cosDesiredAngle * radius),
                                                        (float)(centreOfMass.Y + sinDesiredAngle * radius));

        // away from destination thru CoM
        PointF backwardsPointForLine = new((int)(centreOfMass.X + cosDesiredAngle * arc),
                                           (int)(centreOfMass.Y + sinDesiredAngle * arc));

        // closest is the point on the line (from opposite side of CoM) to the dog
        // i.e. closest of desired point on the line between CoM and backwards point
        MathUtils.IsOnLine(centreOfMass, backwardsPointForLine, desiredPointAtTheEdgeOfSheepCircle, out PointF closestPointOnLine);

        DesiredPosition = closestPointOnLine;
    }

    /// <summary>
    /// Number of pixels dog must attempt to keep away from sheep centre of mass.
    /// </summary>
    /// <param name="pherd"></param>
    /// <returns></returns>
    internal static float ClosestDogMayIntentionallyGetToSheepMass()
    {
        // hard-code, because if we compute based on all the sheep, stragglers kill the algorithm
        return 55;
    }

    /// <summary>
    /// Human has provided a desired position for the dog.
    /// We compute the angle to move, and speed. That data is captured as "desired" output from a trained neural network,
    /// giving us "Inputs" + "Outputs" (complete training data).
    /// </summary>
    /// <param name="inputToAI"></param>
    private void StoreHumanCapturedTrainingDataAndSteerDog(List<double> inputToAI, double[] output)
    {
        if (swUser is null) throw new Exception("stream for outputting AI is not initialised");

        // training data => inputs | outputs
        LearnToHerd.s_trainData.Add(new TrainData(inputToAI.ToArray(), output));

        swUser.WriteLine($"input: {string.Join(",", inputToAI)} | output: {string.Join(",", output)} | angle: {AngleDogIsFacingInDegrees} speed: {Speed}");
    }

    /// <summary>
    /// The AI and heuristics engine require 3 inputs:
    /// - the delta X (dog vs. centre of mass) 
    /// - the delta Y (dog vs. centre of mass) 
    /// - the angle the sheep "mass" needs to head.
    /// </summary>
    /// <returns>
    /// [0] = x of dog relative to flock. Range: 0..1, 1=width
    /// [1] = y of dog relative to flock. Range: 0..1, 1=height
    /// [2] = angle flock need to head based on way point (in RADIANS) / Math.PI. Range: -1..1
    ///</returns>
    internal List<double> InputsToAIorHeuristicsEngine()
    {
        PointF centreOfMass = flockBeingHerded.TrueCentreOfMass();

        double angleSheepNeedToTravelInRadians = flockBeingHerded.AngleToNextWayPointInRadians();

        // provide the desired point for the flock to head, that is drawn later.
        flockBeingHerded.DesiredLocation = new PointF((float)(centreOfMass.X + Math.Cos(angleSheepNeedToTravelInRadians) * Config.DogSensorOfSheepVisionDepthOfVisionInPixels),
                                                      (float)(centreOfMass.Y + Math.Sin(angleSheepNeedToTravelInRadians) * Config.DogSensorOfSheepVisionDepthOfVisionInPixels));

        List<double> inputToAI = new()
                        {
                            // sheep dogs know where they are in the field wrt to the itself and sheep (relative), so we give that to the AI
                            (centreOfMass.X - flockBeingHerded.dog.Position.X) / LearnToHerd.s_sizeOfPlayingField.Width, // 0..1 wrt to Width
                            (centreOfMass.Y - flockBeingHerded.dog.Position.Y) / LearnToHerd.s_sizeOfPlayingField.Height, // 0..1 wrt to Height
                            angleSheepNeedToTravelInRadians / Math.PI // AI likes range -1..1, not -PI..+PI
                        };

        return inputToAI;
    }

    /// <summary>
    /// Collision detect: We check to see if the dog's position is now within the lines 
    /// representing fences.
    /// </summary>
    private void PreventDogJumpingFences()
    {
        PointF c = new();

        // collision with any walls?
        foreach (PointF[] points in LearnToHerd.s_lines)
        {
            for (int i = 0; i < points.Length - 1; i++) // < ..-1, because we're doing line "i" to "i+1"
            {
                PointF point1 = points[i];
                PointF point2 = points[i + 1];

                // touched wall? returns the closest point on the line to the sheep. We check the distance
                if (MathUtils.IsOnLine(point1, point2, new PointF(Position.X + c.X, Position.Y + c.Y), out PointF closest) &&
                    MathUtils.DistanceBetweenTwoPoints(closest, Position) < 6)
                {
                    // yes, need to back off from the wall
                    c.X -= (closest.X - Position.X) / 4;
                    c.Y -= (closest.Y - Position.Y) / 4;
                }
            }
        }

        Position.X += c.X;
        Position.Y += c.Y;
    }

    /// <summary>
    /// Draw the dog, with annotations.
    /// </summary>
    /// <param name="graphics"></param>
    internal void Draw(Graphics graphics)
    {
        // show the dog as a filled black circle 
        graphics.FillEllipse(Brushes.Black, Position.X - 4, Position.Y - 4, 8, 8);

        // overlay a marker showing where we want the dog to go
        graphics.FillEllipse(Brushes.Yellow, DesiredPosition.X - 4, DesiredPosition.Y - 4, 8, 8);

        DrawFaintCircleIndicatingDogsVisionLimit(graphics);

        DrawArrowShowingAngleFromDogToHerdIsCalculatedCorrectly(graphics);

        // for the human player #0, we draw draw a yellow blob indicating desitred location and a line between
        DrawLineBetweenDogAndDesiredPosition(graphics);

        // rather than store it, we do it independently. That doesn't guarantee inputs to AI are correct, but gives us a means to check calculations are correct.
        RecalculateIndependentlySoWeKnowWhatShouldBeHappening(out PointF centreOfMassOfFlock, out float xDesired, out float yDesired, out PointF backwardsPointForLine, out PointF closest, out double angleDogNeedsToMoveInRADIANS);

        DrawLineThruCentreOfMassAndWayPoint(graphics, backwardsPointForLine, centreOfMassOfFlock, new PointF(xDesired, yDesired));

        DrawXatCenterOfMass(graphics, closest, Pens.Cyan);

        DrawLineFromDogToTargetX(graphics, closest, angleDogNeedsToMoveInRADIANS);

        DrawLineWithArrowIndicatingDirectionDogIsFacing(graphics);

        // when we're training (or at least drawing the training to confirm visually there are no gate "blobs", so we end the arrow with an "x")
        if (LearnToHerd.s_wayPointsSheepNeedsToGoThru.Length == 0) DrawXatCenterOfMass(graphics, flockBeingHerded.DesiredLocation, Pens.DarkOrchid);

        DrawHumanHeuristicAIOverlayText(graphics);
    }

    /// <summary>
    /// Decorate the image with a label clarifying what the dog control mechanism is.
    /// </summary>
    /// <param name="graphics"></param>
    private void DrawHumanHeuristicAIOverlayText(Graphics graphics)
    {
        string label;

        switch (Id)
        {
            case 0:
                label = "HUMAN"; break;

            case 1:
                label = "HEURISTICS"; break;

            case 2:
                label = "A.I."; break;

            default: return;
        }

        using Font font = new("Arial", 17);
        using SolidBrush brush = new(Color.FromArgb(80, 255, 255, 255));

        SizeF size = graphics.MeasureString(label, font);
        graphics.DrawString(label, font, brush, LearnToHerd.s_sizeOfPlayingField.Width/2 - size.Width / 2, LearnToHerd.s_sizeOfPlayingField.Height/2 - size.Height / 2); // centred
    }

    /// <summary>
    /// A lot of this is happening as a result of InputsToAI, and the heuristics + separate AI training.
    /// I wanted to disconnect them. This independent check enables us to visually show the theory is correct.
    /// </summary>
    /// <param name="centreOfMassOfFlock"></param>
    /// <param name="xDesired"></param>
    /// <param name="yDesired"></param>
    /// <param name="backwardsPointForLine"></param>
    /// <param name="closest"></param>
    /// <param name="angleDogNeedsToMoveInRADIANS"></param>
    private void RecalculateIndependentlySoWeKnowWhatShouldBeHappening(out PointF centreOfMassOfFlock, out float xDesired, out float yDesired, out PointF backwardsPointForLine, out PointF closest, out double angleDogNeedsToMoveInRADIANS)
    {
        centreOfMassOfFlock = flockBeingHerded.TrueCentreOfMass();

        float radius = ClosestDogMayIntentionallyGetToSheepMass();

        double angleFlockNeedsToHeadInRADIANS = flockBeingHerded.AngleToNextWayPointInRadians();

        xDesired = (float)(centreOfMassOfFlock.X + Math.Cos(angleFlockNeedsToHeadInRADIANS) * Config.DogSensorOfSheepVisionDepthOfVisionInPixels);
        yDesired = (float)(centreOfMassOfFlock.Y + Math.Sin(angleFlockNeedsToHeadInRADIANS) * Config.DogSensorOfSheepVisionDepthOfVisionInPixels);

        double desiredAngleInRADIANS = Math.Atan2(yDesired - centreOfMassOfFlock.Y,
                                                  xDesired - centreOfMassOfFlock.X);

        PointF desiredPointAtTheEdgeOfSheepCircle = new((float)(centreOfMassOfFlock.X + Math.Cos(desiredAngleInRADIANS - Math.PI) * radius),
                                                        (float)(centreOfMassOfFlock.Y + Math.Sin(desiredAngleInRADIANS - Math.PI) * radius));

        // away from destination thru CoM
        backwardsPointForLine = new((float)(centreOfMassOfFlock.X + Math.Cos(desiredAngleInRADIANS - Math.PI) * Config.DogSensorOfSheepVisionDepthOfVisionInPixels),
                                    (float)(centreOfMassOfFlock.Y + Math.Sin(desiredAngleInRADIANS - Math.PI) * Config.DogSensorOfSheepVisionDepthOfVisionInPixels));

        // closest is the point on the line (from opposite side of CoM) to the dog
        MathUtils.IsOnLine(centreOfMassOfFlock, backwardsPointForLine, desiredPointAtTheEdgeOfSheepCircle, out closest);

        angleDogNeedsToMoveInRADIANS = Math.Atan2(closest.Y - Position.Y,
                                                  closest.X - Position.X);
    }

    /// <summary>
    /// Draws a line between the dogs current position and the desired position.
    /// </summary>
    /// <param name="graphics"></param>
    private void DrawLineBetweenDogAndDesiredPosition(Graphics graphics)
    {
        float angleInRADIANS = (float)Math.Atan2(DesiredPosition.Y - Position.Y, 
                                                 DesiredPosition.X - Position.X);
        float speed = MathUtils.DistanceBetweenTwoPoints(Position, DesiredPosition);

        using Pen p2 = new(Color.Crimson);
        p2.DashStyle = DashStyle.Dot;
        p2.EndCap = LineCap.ArrowAnchor;

        // size the line to indicate it knows the distance
        graphics.DrawLine(p2,
                   (int)Position.X, (int)Position.Y,
                   (int)(speed * Math.Cos(angleInRADIANS) + Position.X),
                   (int)(speed * Math.Sin(angleInRADIANS) + Position.Y));
    }

    /// <summary>
    /// Draws a tiny arrow pointer showing the direction the dog is facing.
    /// </summary>
    /// <param name="graphics"></param>
    private void DrawLineWithArrowIndicatingDirectionDogIsFacing(Graphics graphics)
    {
        double angleDogIsFacingInRadians = MathUtils.DegreesInRadians(AngleDogIsFacingInDegrees);

        using Pen penSmallArrow = new(Color.DarkSalmon);
        penSmallArrow.DashStyle = DashStyle.Dot;
        penSmallArrow.EndCap = LineCap.ArrowAnchor;

        graphics.DrawLine(penSmallArrow,
                   (int)Position.X, (int)Position.Y,
                   (int)(20 * Speed * Math.Cos(angleDogIsFacingInRadians) + Position.X),
                   (int)(20 * Speed * Math.Sin(angleDogIsFacingInRadians) + Position.Y));
    }

    /// <summary>
    /// Draws a line from the dog in the angle it needs to head to reach the "x" (closest point).
    /// </summary>
    /// <param name="graphics"></param>
    /// <param name="closest"></param>
    /// <param name="angleDogNeedsToMove"></param>
    private void DrawLineFromDogToTargetX(Graphics graphics, PointF closest, double angleDogNeedsToMove)
    {
        float sizeToX = (float)Math.Sqrt(Math.Pow(closest.Y - Position.Y, 2) + Math.Pow(closest.X - Position.X, 2));

        float xDogMove = (float)(Position.X + Math.Cos(angleDogNeedsToMove) * sizeToX);
        float yDogMove = (float)(Position.Y + Math.Sin(angleDogNeedsToMove) * sizeToX);

        graphics.DrawLine(Pens.Red, Position, new PointF(xDogMove, yDogMove));
    }

    /// <summary>
    /// Draws a line from the waypoint thru the centre of mass extending line backwards (on which we put the target cross).
    /// </summary>
    /// <param name="graphics"></param>
    /// <param name="backwardsPointForLine"></param>
    /// <param name="centreOfMass"></param>
    /// <param name="desiredLocation"></param>
    private static void DrawLineThruCentreOfMassAndWayPoint(Graphics graphics, PointF backwardsPointForLine, PointF centreOfMass, PointF desiredLocation)
    {
        /*
         *     + backwardsPointForLine
         *      \
         *       \
         *        \  
         *         \ 
         *          \
         *           \
         *            \ 
         *             \ 
         *              \o
         *   sheep ->  o x o
         *               o.
         *                 .   
         *                  .
         *                   .
         *                    .
         *                     O desiredLocation 
         *                   
         */

        // draw line backwards from centre of mass 
        using Pen penLineThru = new(Color.FromArgb(150, 30, 30, 30));
        penLineThru.DashStyle = DashStyle.Dash;

        graphics.DrawLine(penLineThru, backwardsPointForLine, new PointF(centreOfMass.X, centreOfMass.Y));

        // draw line forwards from centre of mass
        using Pen penForwardPointing = new(Color.FromArgb(30, 230, 230, 230));
        penForwardPointing.EndCap = LineCap.ArrowAnchor;
        penForwardPointing.DashStyle = DashStyle.Dash;

        graphics.DrawLine(penForwardPointing, centreOfMass, desiredLocation);
    }

    /// <summary>
    /// Debugging requires us to know where the center of mass is. So we draw a red "X".
    /// </summary>
    /// <param name="graphics"></param>
    /// <param name="flockCenterOfMass"></param>
    private static void DrawXatCenterOfMass(Graphics graphics, PointF flockCenterOfMass, Pen pen)
    {
        // x marks the spot for center of mass
        graphics.DrawLine(pen, flockCenterOfMass.X - 4, flockCenterOfMass.Y - 4, flockCenterOfMass.X + 4, flockCenterOfMass.Y + 4);
        graphics.DrawLine(pen, flockCenterOfMass.X - 4, flockCenterOfMass.Y + 4, flockCenterOfMass.X + 4, flockCenterOfMass.Y - 4);
    }

    /// <summary>
    /// We provide the AI dog with an angle indicating the direction to the flock.
    /// This draws the arrow. It is fixed size unless the AI knows the distance to the flock.
    /// </summary>
    /// <param name="graphics"></param>
    private void DrawArrowShowingAngleFromDogToHerdIsCalculatedCorrectly(Graphics graphics)
    {
        PointF pherd = flockBeingHerded.TrueCentreOfMass();
        double herdAngle = Math.Atan2(pherd.Y - Position.Y,
                                      pherd.X - Position.X);

        // size the line to indicate it knows the distance
        float size = (float)MathUtils.DistanceBetweenTwoPoints(Position, pherd);

        using Pen p2 = new(Color.Aqua);
        p2.DashStyle = DashStyle.Dot;
        p2.EndCap = LineCap.ArrowAnchor;

        graphics.DrawLine(p2,
                   (int)Position.X, (int)Position.Y,
                   (int)(size * Math.Cos(herdAngle) + Position.X),
                   (int)(size * Math.Sin(herdAngle) + Position.Y));
    }

    /// <summary>
    /// Having a circle helps you realise when the AI moves to far away from the 
    /// center of mass. We draw it as a faint dotted line.
    /// </summary>
    /// <param name="graphics"></param>
    private void DrawFaintCircleIndicatingDogsVisionLimit(Graphics graphics)
    {
        using Pen p = new(Color.FromArgb(30, 255, 255, 255));
        p.DashStyle = DashStyle.Dot;

        graphics.DrawEllipse(
            p,
            (int)(Position.X - Config.DogSensorOfSheepVisionDepthOfVisionInPixels),
            (int)(Position.Y - Config.DogSensorOfSheepVisionDepthOfVisionInPixels),
            (int)Config.DogSensorOfSheepVisionDepthOfVisionInPixels * 2,
            (int)Config.DogSensorOfSheepVisionDepthOfVisionInPixels * 2);
    }
}