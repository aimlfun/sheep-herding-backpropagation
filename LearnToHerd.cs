using SheepHerderAI.AI;
using SheepHerderAI.Configuration;
using SheepHerderAI.Predator;
using SheepHerderAI.Sheepies;
using System.Drawing.Drawing2D;
using System.Security.Cryptography;

namespace SheepHerderAI;

///    _                        _____     _   _              _ 
///   | |    ___  __ _ _ __ _ _|_   _|__ | | | | ___ _ __ __| |
///   | |   / _ \/ _` | '__| '_ \| |/ _ \| |_| |/ _ \ '__/ _` |
///   | |__|  __/ (_| | |  | | | | | (_) |  _  |  __/ | | (_| |
///   |_____\___|\__,_|_|  |_| |_|_|\___/|_| |_|\___|_|  \__,_|
///
static class LearnToHerd
{
    /// <summary>
    /// Delegate for event handler (we call upon mutation).
    /// </summary>
    public delegate void MutationDelegate();

    /// <summary>
    /// This is the green field with brown hedges/fences, and a score zone. 
    /// We paint it once, and use it as the basis of our background.
    /// </summary>
    private static Bitmap? s_backgroundImage;

    /// <summary>
    /// Size of the "learning" area (sheep pen / fences).
    /// </summary>
    internal static Size s_sizeOfPlayingField = new();

    /// <summary>
    /// Brush for drawing scoring zone. Static as it applies to ALL flocks.
    /// </summary>
    private readonly static HatchBrush s_hatchBrushForScoringZone = new(HatchStyle.DiagonalCross, Color.FromArgb(30, 255, 255, 255), Color.Transparent);

    /// <summary>
    /// Region that is the "home" scoring zone. Static as it applies to ALL flocks.
    /// </summary>
    internal static RectangleF s_sheepPenScoringZone = new(100, 100, 100, 100);

    /// <summary>
    /// Lines the sheep must avoid (makes for more of a challenge). Static as it applies to ALL flocks.
    /// </summary>
    internal static List<PointF[]> s_lines = new();

    /// <summary>
    /// The generation (how many times the network has been mutated).
    /// </summary>
    internal static float s_generation = 0;

    /// <summary>
    /// The list of flocks indexed by their "id".
    /// </summary>
    internal readonly static Dictionary<int, Flock> s_flock = new();

    /// <summary>
    /// If set to true, then this ignores requests to mutate.
    /// </summary>
    internal static bool s_stopMutation = false;

    /// <summary>
    /// How many moves the predator has performed.
    /// </summary>
    internal static int s_numberOfMovesMadeByPredator = 0;

    /// <summary>
    /// Defines the number of moves it will initialise the mutate counter to.
    /// This number increases with each generation.
    /// </summary>
    internal static int s_movesToCountBetweenEachMutation = 0;

    /// <summary>
    /// Defines the number of moves before a mutation occurs. 
    /// This is decremented each time the cars move, and upon reaching zero triggers
    /// a mutation.
    /// </summary>
    internal static int s_movesLeftBeforeNextMutation = 0;

    /// <summary>
    /// The points the sheep need to go thru.
    /// </summary>
    internal static Point[] s_wayPointsSheepNeedsToGoThru = Array.Empty<Point>();

    /// <summary>
    /// Used for drawing the scoreboard. 
    /// </summary>
    private readonly static Font scoreBoardFont = new("Arial", 8);

    /// <summary>
    /// 
    /// </summary>
    internal static List<TrainData> s_trainData = new();

    /// <summary>
    /// Start the AI learning process: initialises the neural networks, and starts first generation.
    /// </summary>
    internal static void StartLearning()
    {
        s_generation = 0;

        InitialiseTheNeuralNetworksForEachFlock();

        NextGeneration();
    }

    /// <summary>
    /// Moves to next generation. Increases the moves by a fixed %age.
    /// Flock is "initialised".
    /// </summary>
    internal static void NextGeneration()
    {
        ++s_generation;

        if (s_movesLeftBeforeNextMutation == 0) s_movesLeftBeforeNextMutation = s_movesToCountBetweenEachMutation;

        // after a mutation, we crush cars and get new ones (reset their position/state) whilst keeping the neural networks
        InitialiseFlocks();

        s_numberOfMovesMadeByPredator = 0;
    }

    /// <summary>
    /// Initialises the neural network for the sheep.
    /// </summary>
    internal static void InitialiseTheNeuralNetworksForEachFlock()
    {
        NeuralNetwork.s_networks.Clear();

        for (int i = 0; i < Config.NumberOfAIdogs; i++)
        {
            _ = new NeuralNetwork(i, Config.AIHiddenLayers, Config.AIactivationFunctions);
        }

        NeuralNetwork.s_networks[2].Load(@"c:\temp\sheep0-UHA.ai");

        return;

        /*
        for (int z = 0; z < 1000;z++)
        {

            LoadTrainingData();          
        }

        bool loaded = false;

        foreach (NeuralNetwork n in NeuralNetwork.s_networks.Values)
        {
            loaded |= n.Load($@"c:\temp\sheep{n.Id}.ai");
        }
        */

        // loaded are trained. Trained don't need an early mutation.
        //if (loaded) Config.AINumberOfInitialMovesBeforeFirstMutation = 100000;
    }

    private static void LoadTrainingData()
    {
        using StreamReader sr = new(@"c:\temp\training.dat");

        while(!sr.EndOfStream)
        {
            string? line = sr.ReadLine();
            if (line == null) break;

            string[] tokens = line.Split(',');

            double x = double.Parse(tokens[0]);
            double y = double.Parse(tokens[1]);
            double a = double.Parse(tokens[2]);

            double r = double.Parse(tokens[3]);
            double s = double.Parse(tokens[4]);

            NeuralNetwork.s_networks[1].BackPropagate(new double[] { x, y, a }, new double[] { r, s });
        }
    }

    /// <summary>
    /// If not the first flock, kills them humanely first. 
    /// Re-creates the flock (reset position etc). Important: this does not touch the neural network we are training
    /// </summary>
    internal static void InitialiseFlocks()
    {
        // if we have flocks, then we need to mutate the brains based of fitness (a brain per dog)
        if (s_flock.Count > 0)
        {
            MutateFlock();
            s_flock.Clear();
        }

        List<PointF> sheepPositions = new();
        for (int i = 0; i < Config.InitialFlockSize; i++)
        {
            // randomish place near the start position
            sheepPositions.Add(new PointF(RandomNumberGenerator.GetInt32(0, s_sizeOfPlayingField.Width / 6 + 20),
                                          RandomNumberGenerator.GetInt32(0, s_sizeOfPlayingField.Height / 6) + 40));
        }

        // we train multiple dogs at once, this creates a dog and its flock
        for (int flockNumber = 0; flockNumber < Config.NumberOfAIdogs; flockNumber++)
        {
            Flock flock = new(flockNumber, sheepPositions, s_sizeOfPlayingField.Width, s_sizeOfPlayingField.Height);

            s_flock.Add(flockNumber, flock);
        }
    }
   
    /// <summary>
    /// Mutation of the neural network.
    /// </summary>
    private static void MutateFlock()
    {
        if (Config.NumberOfAIdogs == 1)
        {
            NeuralNetwork.s_networks[0] = new NeuralNetwork(0, Config.AIHiddenLayers, Config.AIactivationFunctions, false);
            return;
        }

        return;

        // lots of propagations
        for (int i = 0; i < 100000; i++)
        {
            // back propagate for all data
            foreach (TrainData td in s_trainData)
            {
                NeuralNetwork.s_networks[2].BackPropagate(td.inputs, td.outputs);
            }

            if (i > 10000)
            {
                bool trained = true;

                foreach (TrainData td in s_trainData)
                {
                    double[] x = NeuralNetwork.s_networks[2].FeedForward(td.inputs);

                    if (Math.Abs(td.outputs[0] - x[0]) > 0.001f || Math.Abs(td.outputs[1] - x[1]) > 0.001f)
                    {
                        trained = false;
                        break;
                    }
                }

                if (trained)
                {
                    break;
                }
            }
        }
    }

    /// <summary>
    /// Move all the sheep and predators (occurs when the timer fires)
    /// </summary>
    internal static void Learn()
    {
        ++s_numberOfMovesMadeByPredator;

        MoveAllFlocks();
    }

    /// <summary>
    /// Moves all the flocks at once, in parallel or serial.
    /// Use serial for debugging, and parallel to get very fast performance.
    /// </summary>
    internal static void MoveAllFlocks()
    {
        if (Config.SheepUseParallelComputation)
        {
            // all sheep are independent, each one has a neural network and sensors attached, this therefore
            // is a candidate for parallelism.
            Parallel.ForEach(s_flock.Keys, id =>
            {
                s_flock[id].Move();
            });
        }
        else
        {
            foreach (int id in s_flock.Keys)
            {
                s_flock[id].Move();
            }
        }
    }

    /// <summary>
    /// Draws ALL the dogs along with their respective flock.
    /// </summary>
    /// <returns></returns>
    internal static List<Bitmap> DrawAll()
    {
        s_backgroundImage ??= DrawbackgroundImage();

        List<Bitmap> images = new();
 
        foreach (int id in s_flock.Keys)
        {
            Flock flock = s_flock[id];

            // each flock is a separate image, that starts with a predefined background
            Bitmap image = new(s_backgroundImage, s_sizeOfPlayingField);

            using Graphics graphics = Graphics.FromImage(image);
            graphics.CompositingQuality = CompositingQuality.HighQuality;
            graphics.SmoothingMode = SmoothingMode.HighQuality;

            flock.Draw(graphics);

            // add a "layer" that darkens the image to indicate failure and make it clear which ones are finished, and still running
            if (flock.flockIsFailure)
            {
                using SolidBrush p = new(Color.FromArgb(150, 0, 0, 0));
                graphics.FillRectangle(p, new Rectangle(0, 0, s_sizeOfPlayingField.Width, s_sizeOfPlayingField.Height));
            }

            if (s_wayPointsSheepNeedsToGoThru.Length > 0)
            {
                graphics.DrawString($"Id: {flock.Id}  score {flock.numberOfSheepInPenZone}   fitness {Math.Round(flock.FitnessScore() / 10)} ({NeuralNetwork.s_networks[flock.Id].Fitness / 10})   {flock.failureReason}",
                                scoreBoardFont, Brushes.White, 10, 10);
            }

            graphics.Flush();


            images.Add(image);
        }

        return images;
    }

    /// <summary>
    /// Paints the background that is common to all flocks (field, fences, score zone)
    /// </summary>
    /// <returns></returns>
    private static Bitmap DrawbackgroundImage()
    {
        Bitmap image = new(s_sizeOfPlayingField.Width, s_sizeOfPlayingField.Height);

        using Graphics g = Graphics.FromImage(image);
        g.Clear(Color.Green);
        g.CompositingQuality = CompositingQuality.HighQuality;
        g.SmoothingMode = SmoothingMode.HighQuality;

        if (s_wayPointsSheepNeedsToGoThru.Length == 0) return image;

        // draw the scoring zone as a hatched area
        g.FillRectangle(s_hatchBrushForScoringZone, s_sheepPenScoringZone);

        // draw the lines around the play area (fences)
        using Pen p = new(Color.Brown, 4);
        foreach (PointF[] points in s_lines) g.DrawLines(p, points);

        // draw blobs for way points
        using SolidBrush wayPointBrush = new(Color.FromArgb(30, 220, 230, 243));

        int i = 1;

        foreach (Point wp in s_wayPointsSheepNeedsToGoThru)
        {
            g.FillEllipse(wayPointBrush, wp.X - 3, wp.Y - 3, 6, 6);
            g.DrawString(i.ToString(), new Font("Arial", 7), wayPointBrush, wp.X + 3, wp.Y);
            ++i;
        }

        g.Flush();
        return image;
    }
}