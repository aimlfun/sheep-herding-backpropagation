using System.Drawing.Imaging;
using SheepHerderAI.Configuration;
using SheepHerderAI.Sheepies;
using SheepHerderAI;
using SheepHerderAI.Utilities;
using NUnit.Framework;

namespace Sheep_Dog_AI_Test_Suite
{
    public class TestDogSheepInteraction
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void PlotDogSheepInteractionDogStatic()
        {
            DrawSheepRespondingToVariousDogPositionsDogStatic();
            Assert.Pass();
        }

        [Test]
        public void PlotDogSheepInteractionDogMoves()
        {
            DrawSheepRespondingToVariousDogPositionsDogMoving();
            Assert.Pass();
        }

        /// <summary>
        /// Positions the dog around the sheep for different tests, but the dog doesn't move.
        /// </summary>
        private static void DrawSheepRespondingToVariousDogPositionsDogStatic()
        {
            InitialiseDogAndSheep(out int distanceDogMustBeFromSheep, out PointF[] comArray);

            for (int round = 0; round < 20; round++)
            {
                LearnToHerd.MoveAllFlocks();

                DrawDogAndSheepWithAnnotationToFile("static", distanceDogMustBeFromSheep, comArray, round);

                // move it more steps
                for (int z = 0; z < 10; z++) LearnToHerd.MoveAllFlocks();
            }         
        }

        /// <summary>
        /// Positions the dog around the sheep for different tests, but the dog moves towards the sheep.
        /// </summary>
        private static void DrawSheepRespondingToVariousDogPositionsDogMoving()
        {
            InitialiseDogAndSheep(out int distanceDogMustBeFromSheep, out PointF[] comArray);

            for (int round = 0; round < 20; round++)
            {
                LearnToHerd.MoveAllFlocks();

                foreach (Flock f in LearnToHerd.s_flock.Values)
                {
                    float angleInRadians = (float)MathUtils.DegreesInRadians(f.dog.AngleDogIsFacingInDegrees);

                    // predator moves towards the chosen angle, at the chosen speed.
                    f.dog.Position.X -= f.dog.Speed * (float)Math.Cos(angleInRadians);
                    f.dog.Position.Y -= f.dog.Speed * (float)Math.Sin(angleInRadians);
                    f.dog.DesiredPosition = f.dog.Position;
                }

                DrawDogAndSheepWithAnnotationToFile("moving", distanceDogMustBeFromSheep, comArray, round);

                // move it more steps
                for (int z = 0; z < 10; z++) LearnToHerd.MoveAllFlocks();
            }
        }

        /// <summary>
        /// Draws the dog + flock of sheep to each image.
        /// </summary>
        /// <param name="prefix"></param>
        /// <param name="distanceDogMustBeFromSheep"></param>
        /// <param name="comArray"></param>
        /// <param name="round"></param>
        private static void DrawDogAndSheepWithAnnotationToFile(string prefix, int distanceDogMustBeFromSheep, PointF[] comArray, int round)
        {
            List<Bitmap> images = LearnToHerd.DrawAll();
            int ang = 0;
            int n = 0;

            foreach (Bitmap b in images)
            {
                PointF centreOfFlock = LearnToHerd.s_flock[n].TrueCentreOfMass();
                using Graphics g = Graphics.FromImage(b);
            
                g.DrawLine(Pens.Magenta, comArray[n], centreOfFlock);

                g.DrawLine(Pens.Tomato, LearnToHerd.s_flock[n].dog.Position, 
                            new PointF((float)(LearnToHerd.s_flock[n].dog.Position.X + Math.Cos(MathUtils.DegreesInRadians(ang + 180)) * 2 * distanceDogMustBeFromSheep),
                                       (float)(LearnToHerd.s_flock[n].dog.Position.Y + Math.Sin(MathUtils.DegreesInRadians(ang + 180)) * 2 * distanceDogMustBeFromSheep)));
                g.Flush();

                b.Save($@"c:\temp\move-{prefix}-demo-{ang}-{round}.png", ImageFormat.Png);
                
                ang += 10;
                n++;
            }
        }

        /// <summary>
        /// Initialises the sheep and dog, and positions them.
        /// </summary>
        /// <param name="distanceDogMustBeFromSheep"></param>
        /// <param name="comArray"></param>
        private static void InitialiseDogAndSheep(out int distanceDogMustBeFromSheep, out PointF[] comArray)
        {
            Config.NumberOfAIdogs = 36;

            distanceDogMustBeFromSheep = 150;

            LearnToHerd.s_sizeOfPlayingField = new Size(300, 300);
            LearnToHerd.s_flock.Clear();
            LearnToHerd.InitialiseFlocks();
            
            PointF centre = new(150, 150);

            List<PointF> com = new();

            for (int i = 0; i < Config.NumberOfAIdogs; i++)
            {
                double angle = MathUtils.DegreesInRadians(i * 10);

                LearnToHerd.s_flock[i].dog.AngleDogIsFacingInDegrees = (float)i * 10;

                // put dog at specific angle from flock
                LearnToHerd.s_flock[i].dog.Position = new PointF((float)(centre.X + Math.Cos(angle) * distanceDogMustBeFromSheep), (float)(centre.Y + Math.Sin(angle) * distanceDogMustBeFromSheep));
                LearnToHerd.s_flock[i].dog.DesiredPosition = LearnToHerd.s_flock[i].dog.Position;
                LearnToHerd.s_flock[i].dog.Speed = 2;

                PointF centreOfFlock = LearnToHerd.s_flock[i].TrueCentreOfMass();

                foreach (Sheep f in LearnToHerd.s_flock[i].flock)
                {
                    f.Position.X -= centreOfFlock.X;
                    f.Position.Y -= centreOfFlock.Y;
                }

                foreach (Sheep f in LearnToHerd.s_flock[i].flock)
                {
                    f.Position.X += centre.X;
                    f.Position.Y += centre.Y;
                }

                centreOfFlock = LearnToHerd.s_flock[i].TrueCentreOfMass();
                com.Add(centreOfFlock);
            }

            comArray = com.ToArray();
        }
    }
}