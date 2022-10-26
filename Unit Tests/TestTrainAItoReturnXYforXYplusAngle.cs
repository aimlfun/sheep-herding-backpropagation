using NUnit.Framework;
using SheepHerderAI.AI;
using SheepHerderAI.Configuration;
using System.Diagnostics;

namespace SheepHerderAlternateIdea.Unit_Tests;

/// <summary>
/// Class to train the AI to return the "new" X Y for the dog based on the 
/// X Y of the sheep given an angle.
/// 
/// Data provided as a training.dat file/ 
/// </summary>
class TestTrainAItoReturnXYforXYplusAngle
{
    const int thresholdRowsToProcess = 1000000;

    /// <summary>
    /// Ensure a consistent setup
    /// </summary>
    [SetUp]
    public void Setup()
    {
        Config.NumberOfAIdogs = 1;
      
        NeuralNetwork.s_networks.Clear();
    }

    [Test]
    public void TestTrainXYAngleToXYDesired()
    {          
        _ = new NeuralNetwork(0, Config.AIHiddenLayers, Config.AIactivationFunctions);

        // attempt to load any prior learning for the sheep.
        NeuralNetwork.s_networks[0].Load(@"c:\temp\sheep0-UHA.ai");

        // loads the training data into a list of values
        LoadTrainingDATaFile(out List<double[]> traingDataParsed);

        // improve the training by shuffling the sequential data up
        RandomlyShuffleTheTrainingData(traingDataParsed);

        bool trained = false;
        int epoch = 0;

        for (int i = 0; i < 2000000; i++) // training takes a lot
        {
            epoch = i;

            TraingNeuralNetworkReturningCost(traingDataParsed, out float totalcost, out int cnt);

            if (epoch % 100 == 0) Debug.WriteLine($"Epoch: {epoch}. total-cost: {totalcost} avg cost: {totalcost / cnt}");

            trained = false;

            if (totalcost < 0.02f) // total isn't a good indicator
            {
                trained = true;

                int itemsTrained = 0;

                foreach (double[] tokens in traingDataParsed)
                {
                    ExtractInputsAndOutputsFromTrainingDat(tokens, out double[] inputs, out double[] outputs);

                    double[] result = NeuralNetwork.s_networks[0].FeedForward(inputs);

                    if (Math.Abs(result[0] - outputs[0]) > 0.001f || Math.Abs(result[1] - outputs[1]) > 0.001f)
                    {
                        if (itemsTrained > 0) Debug.WriteLine($"matched {itemsTrained} out of {traingDataParsed.Count} backpopulations: {cnt} total cost:{totalcost} avg cost: {totalcost / cnt}");

                        trained = false;
                        break;
                    }

                    ++itemsTrained;
                }
            }

            if (trained)
            {
                Debug.WriteLine($"** TRAINED {epoch} **");
                break;
            }
        }

        if (trained)
        {
            NeuralNetwork.Save();
        }
        else
        {
            Debug.WriteLine($"!! FAILED {epoch}!!");
        }

        DumpOutTheResultsOfTraining(traingDataParsed);
        
        if (!trained) Assert.Fail();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="traingDataParsed"></param>
    private static void DumpOutTheResultsOfTraining(List<double[]> traingDataParsed)
    {
        foreach (double[] tokens in traingDataParsed)
        {
            ExtractInputsAndOutputsFromTrainingDat(tokens, out double[] inputs, out double[] outputs);

            double[] result = NeuralNetwork.s_networks[0].FeedForward(inputs);

            Console.WriteLine($"{string.Join(",", inputs)}={string.Join(",", outputs)} NN=>{string.Join(",", result)}");
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="traingDataParsed"></param>
    /// <param name="totalcost"></param>
    /// <param name="cnt"></param>
    private static void TraingNeuralNetworkReturningCost(List<double[]> traingDataParsed, out float totalcost, out int cnt)
    {
        totalcost = 0;
        cnt = 0;

        for (int n = 0; n < traingDataParsed.Count; n++)
        {
            ExtractInputsAndOutputsFromTrainingDat(traingDataParsed[n], out double[] inputs, out double[] outputs);

            totalcost += NeuralNetwork.s_networks[0].BackPropagate(inputs, outputs);
            ++cnt;
        }
    }

    /// <summary>
    /// Splits a double[] into 2 separate arrays (inputs and outputs). 
    /// </summary>
    /// <param name="traingDataParsed"></param>
    /// <param name="inputs"></param>
    /// <param name="outputs"></param>
    private static void ExtractInputsAndOutputsFromTrainingDat(double[] traingDataParsed, out double[] inputs, out double[] outputs)
    {
        inputs = new double[] { traingDataParsed[0], traingDataParsed[1], traingDataParsed[2] };
        outputs = new double[] { traingDataParsed[3], traingDataParsed[4] };
    }

    /// <summary>
    /// Shuffle the training data, so it's unordered. This appears to help.
    /// </summary>
    /// <param name="traingDataParsed"></param>
    private static void RandomlyShuffleTheTrainingData(List<double[]> traingDataParsed)
    {
        Random random = new();

        // swap them randomly so they are not sequential
        for (int i = 0; i < traingDataParsed.Count / 2; i++)
        {
            int p1 = random.Next(0, traingDataParsed.Count);
            int p2 = random.Next(0, traingDataParsed.Count);

            if (p1 == p2) continue; // no swap to perform

            // swap the points
            (traingDataParsed[p2], traingDataParsed[p1]) = (traingDataParsed[p1], traingDataParsed[p2]);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="traingDataParsed"></param>
    private static void LoadTrainingDATaFile(out List<double[]> traingDataParsed)
    {
        List<string[]> listOfTrainingDataAsStrings = new();
        using StreamReader sr = new(@"c:\temp\training.dat");
        try
        {
            int n = 0;

            while (!sr.EndOfStream)
            {
                string? line = sr.ReadLine();
                if (line == null) break;

                string[] tokens = line.Split(',');
                listOfTrainingDataAsStrings.Add(tokens);

                if (++n > thresholdRowsToProcess) break;
            }
        }
        finally
        {
            sr.Close();
        }
    
        traingDataParsed = new();

        foreach (string[] data in listOfTrainingDataAsStrings)
        {
            DecodeTokensIntoXYanglePlusXYoutput(data, out double[] inputs, out double[] outputs);

            List<double> dataParsed = new(inputs);
            dataParsed.AddRange(outputs);

            traingDataParsed.Add(dataParsed.ToArray());
        }

        listOfTrainingDataAsStrings.Clear();
    }

    /// <summary>
    /// Converts the line of AI training data into the input / outputs used for training.
    /// </summary>
    /// <param name="tokens"></param>
    /// <param name="inputs"></param>
    /// <param name="outputs"></param>
    static void DecodeTokensIntoXYanglePlusXYoutput(string[] tokens, out double[] inputs, out double[] outputs)
    {
        double xPosition = double.Parse(tokens[0]);
        double yPosition = double.Parse(tokens[1]);
        double angle0to1 = double.Parse(tokens[2]);

        inputs = new double[] { xPosition, yPosition, angle0to1 };

        double xdesired = double.Parse(tokens[3]);
        double ydesired = double.Parse(tokens[4]);

        outputs = new double[] { xdesired, ydesired };
    }

}
