using NUnit.Framework;
using SheepHerderAI;
using SheepHerderAI.Configuration;
using SheepHerderAI.Sheepies;
using SheepHerderAI.Utilities;

namespace SheepHerderAlternateIdea.Unit_Tests;

class TestCreateDataWithAngleOutput
{
    PointF centre = new(150, 150);


    [SetUp]
    public void Setup()
    {
        Config.NumberOfAIdogs = 1;        
    }

    [Test]
    public void PlotDogSheepInteractionDogStatic2()
    {
        /*
         * Today:
         *   out: speed / angle that will move correctly 
         *    in: lots of things (angle OF , speed, sensors etc), desired angle
         * 
         * Required
         *    in: desired angle
         *    in: relative x,y (sheep CoM to dog)
         *   out: speed / angle to move dog
         * 
         * Compute point closest to CoM driving angle point -
         *      Draw logical line "L" thru CoM at angle required.
         *      Fetch closest point between dog and logical line "L" (dog aims for shortest route to correct path)
         *      Compute angle dog needs to move to get to that point
         *      Compute speed dog requires to move to that point (basically, it's MAX velocity allowed capped at Euclidean distance.
         *      
         * Training data: max of +/-90 from sheep driving line
         *                max Euclidean distance of training point for dog is within effectual distance (how far predator influences).
         *                
         * Note simplistic approach is likely to fail when the dog is force through the herd. Making it go around is out of scope.
         * Not factoring this in will lead to herd splitting and unhappy behaviour.
         */

        LearnToHerd.s_sizeOfPlayingField = new Size(300, 300);

        InitialiseSheep(out float _);

        PointF centreOfMass = LearnToHerd.s_flock[0].TrueCentreOfMass();

        StreamWriter sw = new(@"c:\TEMP\training.dat");

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
         
                    double[] output = new[] { desiredAngleInRadians / Math.PI };

                    sw.WriteLine($"{string.Join(",", inputToAI)},{string.Join(",", output)}");
                }
            }
        }

        sw.Close();

        Assert.Pass();
    }


    /// <summary>
    /// 
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
