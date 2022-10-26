//#define drawingSteps
//#define writeTrainingData
using NUnit.Framework;
using SheepHerderAI.Configuration;
using SheepHerderAI.Sheepies;
using SheepHerderAI.Utilities;
using SheepHerderAI;
using SheepHerderAI.Predator;
using SheepHerderAI.AI;
using System.Diagnostics;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ToolTip;
using System.Drawing.Imaging;

namespace SheepHerderAlternateIdea.Unit_Tests;

public class TestCreateDataWithXYtoXYplusAngle
{
    PointF centre = new(150, 150);

    [SetUp]
    public void Setup()
    {
        Config.NumberOfAIdogs = 1;
        
        NeuralNetwork.s_networks.Clear();

        for (int i = 0; i < Config.NumberOfAIdogs; i++)
        {
            _ = new NeuralNetwork(i, Config.AIHiddenLayers, Config.AIactivationFunctions);
        }

        NeuralNetwork.s_networks[0].Load(@"c:\temp\sheep0-UHA.ai");
    }

    [Test]
    public void ShowSheep()
    {
        LearnToHerd.s_sizeOfPlayingField = new Size(300, 300);
        for (int x = 0; x < 11; x++)
        {
            InitialiseSheep(out float radius);

            LearnToHerd.s_flock[0].dog.Position = new PointF(0, 0);
            LearnToHerd.s_flock[0].dog.DesiredPosition = LearnToHerd.s_flock[0].dog.Position;

            DrawSheep(x);
        }
    }


    /// <summary>
    /// Draws the dog + flock of sheep to each image.
    /// </summary>
    /// <param name="prefix"></param>
    /// <param name="distanceDogMustBeFromSheep"></param>
    /// <param name="comArray"></param>
    /// <param name="round"></param>
    private static void DrawSheep(int p)
    {
        List<Bitmap> images = LearnToHerd.DrawAll();

        foreach (Bitmap b in images)
        {
            b.Save($@"c:\temp\sheep-random-{p}.png", ImageFormat.Png);
        }
    }

    [Test]
    public void PlotDogSheepInteractionDogAngGenerateTestData()
    {
        /*
         * Compute point closest to CoM driving angle point -
         *      Draw logical line "L" thru CoM at angle required.
         *      Fetch closest point between dog and logical line "L" (dog aims for shortest route to correct path)
         *      Compute delta dog needs to move to get to that point
         *      
         * Training data: max of +/-90 from sheep driving line
         *                max Euclidean distance of training point for dog is within effectual distance (how far predator influences).
         *                
         * Note simplistic approach is likely to fail when the dog is force through the herd. Making it go around is out of scope.
         * Not factoring this in will lead to herd splitting and unhappy behaviour.
         */

        LearnToHerd.s_sizeOfPlayingField = new Size(300, 300);

        InitialiseSheep(out float radius);

        radius = Dog.ClosestDogMayIntentionallyGetToSheepMass();
        PointF centreOfMass = LearnToHerd.s_flock[0].TrueCentreOfMass();

#if writeTrainingData
        StreamWriter sw = new(@"c:\TEMP\training.dat");
#endif
        // for each position of dog, do all the angles the sheep may want to go
        for (int desiredAngle = 0; desiredAngle < 360; desiredAngle++)
        {

            // position the dog around the sheep
            for (int x = (int)(centre.X - Config.DogSensorOfSheepVisionDepthOfVisionInPixels + 1); x < (int)(centre.X + Config.DogSensorOfSheepVisionDepthOfVisionInPixels); x += 10)
            {
                for (int y = (int)(centre.Y - Config.DogSensorOfSheepVisionDepthOfVisionInPixels + 1); y < (int)(centre.Y + Config.DogSensorOfSheepVisionDepthOfVisionInPixels); y += 10)
                {
                    LearnToHerd.s_flock[0].dog.Position = new PointF(x, y);
                    LearnToHerd.s_flock[0].dog.DesiredPosition = LearnToHerd.s_flock[0].dog.Position;

                    // point is not within distance circle from CoM (approximation, not for all sheep)
                    if (MathUtils.DistanceBetweenTwoPoints(LearnToHerd.s_flock[0].dog.Position, centreOfMass) > Config.DogSensorOfSheepVisionDepthOfVisionInPixels) continue;

                    List<double> inputToAI = new()
                    {
                        // sheep dogs know where they are in the field, so we give that to the AI
                        (centreOfMass.X - LearnToHerd.s_flock[0].dog.Position.X) / LearnToHerd.s_sizeOfPlayingField.Width,
                        (centreOfMass.Y - LearnToHerd.s_flock[0].dog.Position.Y) / LearnToHerd.s_sizeOfPlayingField.Height
                    };

                    float arc = (int)Config.DogSensorOfSheepVisionDepthOfVisionInPixels;

                    double angleInRads = MathUtils.DegreesInRadians(desiredAngle);
                    float xDesiredPosition = (float)(centreOfMass.X + Math.Cos(angleInRads) * arc);
                    float yDesiredPosition = (float)(centreOfMass.Y + Math.Sin(angleInRads) * arc);

                    LearnToHerd.s_flock[0].DesiredLocation = new PointF(xDesiredPosition, yDesiredPosition);

                    double desiredAngleInRadians = Math.Atan2((yDesiredPosition - centreOfMass.Y),
                                                              (xDesiredPosition - centreOfMass.X));

                    inputToAI.Add(desiredAngleInRadians / Math.PI);

                    ComputePointOnTheSheepCircleThatTheDogNeedsToGoTo(radius, arc, centreOfMass, desiredAngleInRadians, out PointF bpl, out PointF closest);

                    double[] output = new double[] {
                            (closest.X-centreOfMass.X) / LearnToHerd.s_sizeOfPlayingField.Width,
                            (closest.Y-centreOfMass.Y) / LearnToHerd.s_sizeOfPlayingField.Height
                    };

                    Debug.WriteLine($"[TRAINING] angle={desiredAngle / Math.PI} | TARGET: {closest} | AI TRAINING DATA: inputs = {string.Join(",",inputToAI)} outputs = {string.Join(",",output)}");

                    UseAItoMoveDogAndOutputDataPoints(inputToAI);

#if writeTrainingData
                    sw.WriteLine($"{string.Join(",", inputToAI)},{string.Join(",", output)}");
#endif
                    // draw every other X degress
#if drawingSteps
                    if (desiredAngle % 20 == 0) DrawDogAndSheepWithAnnotationToFile("static", desiredAngleInRadians, arc, new PointF(xDesiredPosition, yDesiredPosition), centreOfMass, LearnToHerd.s_flock[0].dog.Position, bpl, closest, output[0]*Math.PI);
#endif
                }
            }
        }

#if writeTrainingData
        sw.Close();
#endif
        Assert.Pass();
    }
    
    /// <summary>
    /// Uses a neural network to move the dog.
    /// </summary>
    /// <param name="inputToAI"></param>
    private static void UseAItoMoveDogAndOutputDataPoints(List<double> inputToAI)
    {
        // ask the AI what to do next? inputs[] => feedforward => outputs[], [0] = angle deviation [1] = speed
        double[] output = NeuralNetwork.s_networks[0].FeedForward(inputToAI.ToArray());
        Debug.WriteLine($"AI OUTPUT: x={output[0]} y={output[1]}");

        PointF centreOfMass = LearnToHerd.s_flock[0].TrueCentreOfMass();

        LearnToHerd.s_flock[0].dog.DesiredPosition.X = (float)(centreOfMass.X + (output[0] * LearnToHerd.s_sizeOfPlayingField.Width));
        LearnToHerd.s_flock[0].dog.DesiredPosition.Y = (float)(centreOfMass.Y + (output[1] * LearnToHerd.s_sizeOfPlayingField.Height));

        Debug.WriteLine($"AI OUTPUT: move from pos: {LearnToHerd.s_flock[0].dog.Position} TARGET: {LearnToHerd.s_flock[0].dog.DesiredPosition}");
    }

    /// <summary>
    /// Based on the position of the sheep and dog, compute the point on the circle where the dog needs to go.
    /// </summary>
    /// <param name="sheepRadius"></param>
    /// <param name="arc"></param>
    /// <param name="centreOfMass"></param>
    /// <param name="desiredAngleInRadians"></param>
    /// <param name="backwardsPointForLine"></param>
    /// <param name="closest"></param>
    private static void ComputePointOnTheSheepCircleThatTheDogNeedsToGoTo(float sheepRadius, float arc, PointF centreOfMass, double desiredAngleInRadians, out PointF backwardsPointForLine, out PointF closest)
    {
        float cosDesiredAngle = (float)Math.Cos(desiredAngleInRadians - Math.PI);
        float sinDesiredAngle = (float)Math.Sin(desiredAngleInRadians - Math.PI);

        PointF desiredPointAtTheEdgeOfSheepCircle = new((float)(centreOfMass.X + cosDesiredAngle * sheepRadius),
                                                        (float)(centreOfMass.Y + sinDesiredAngle * sheepRadius));

        // away from destination thru CoM
        backwardsPointForLine = new PointF((int)(centreOfMass.X + Math.Cos(desiredAngleInRadians - Math.PI) * arc),
                                           (int)(centreOfMass.Y + Math.Sin(desiredAngleInRadians - Math.PI) * arc));

        // closest is the point on the line (from opposite side of CoM) to the dog
        MathUtils.IsOnLine(centreOfMass, backwardsPointForLine, desiredPointAtTheEdgeOfSheepCircle, out closest);
    }

    /// <summary>
    /// Initialises the sheep.
    /// </summary>
    /// <param name="radius"></param>
    private void InitialiseSheep(out float radius)
    {
        // reset the sheep

        LearnToHerd.s_flock.Clear();
        LearnToHerd.InitialiseFlocks(); // 1 flock

        PointF centreOfFlock = LearnToHerd.s_flock[0].TrueCentreOfMass();

        foreach (Sheep f in LearnToHerd.s_flock[0].flock)
        {
            f.Position.X -= centreOfFlock.X;
            f.Position.Y -= centreOfFlock.Y;
        }

        radius = 0;

        foreach (Sheep f in LearnToHerd.s_flock[0].flock)
        {
            // work out diameter of flock
            float r = MathUtils.DistanceBetweenTwoPoints(f.Position, new PointF(0, 0));
            if (r > radius) radius = r;

            f.Position.X += centre.X;
            f.Position.Y += centre.Y;
        }
    } 
}