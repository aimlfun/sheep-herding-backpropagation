using NUnit.Framework;
using SheepHerderAI.AI;
using SheepHerderAI.Configuration;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SheepHerderAlternateIdea.Unit_Tests
{
    class TestTrainAItoReturnAngle
    {
        [SetUp]
        public void Setup()
        {
            Config.NumberOfAIdogs = 1;
            
            NeuralNetwork.s_networks.Clear();
        }

        [Test]
        public void TestTrain()
        {         
            _ = new NeuralNetwork(0, Config.AIHiddenLayers, Config.AIactivationFunctions);

            List<string[]> xxx = new();

            using StreamReader sr = new(@"c:\temp\training.dat");

            while (!sr.EndOfStream)
            {
                string? line = sr.ReadLine();
                if (line is null)
                {
                    Debugger.Break();
                    break;
                }

                string[] tokens = line.Split(',');
                xxx.Add(tokens);
            }

            sr.Close();

            bool trained = false;
            Random random = new(1);

            int epoch = 0;
            for (int i = 0; i < 10000; i++)
            {
                epoch = i;
                for (int n = 0; n < 600; n++)
                {
                    int value = random.Next(0, 600); // train on random
                    string[] tokens = xxx[value];
                    Encode2(tokens, out double[] inputs, out double[] outputs);

                    NeuralNetwork.s_networks[0].BackPropagate(inputs, outputs);
                }

                trained = false;

                if (i > 100)
                {
                    trained = true;

                    int z = 0;

                    foreach (string[] tokens in xxx)
                    {
                        Encode2(tokens, out double[] inputs, out double[] outputs);

                        double[] result = NeuralNetwork.s_networks[0].FeedForward(inputs);

                        if (result[0] is double.NaN) Debugger.Break();

                        if (Math.Abs(result[0] - outputs[0]) > 0.001f /*|| Math.Abs(result[1] - outputs[1]) > 0.001f*/)
                        {
                            trained = false;
                            break;
                        }

                        ++z;
                        if (z >= 100) break;
                    }

                }

                if (trained)
                {
                    Console.WriteLine($"** TRAINED @{epoch} **");
                    break;
                }
            }


            if (trained)
            {
                foreach (string[] tokens in xxx)
                {
                    Encode2(tokens, out double[] inputs, out double[] outputs);

                    double[] result = NeuralNetwork.s_networks[0].FeedForward(inputs);

                    Console.WriteLine($"{string.Join(",", inputs)}={string.Join(",", outputs)} NN=>{string.Join(",", result)}");
                }
                Assert.Pass();
            }
            else
            {
                Console.WriteLine("!! FAILED !!");
                Assert.Fail();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tokens"></param>
        /// <param name="inputs"></param>
        /// <param name="outputs"></param>
        static void Encode2(string[] tokens, out double[] inputs, out double[] outputs)
        {
            double x = double.Parse(tokens[0]) + 0.5f;
            double y = double.Parse(tokens[1]) + 0.5f;

            inputs = new double[] { x, y };

            double r = double.Parse(tokens[2]);

            outputs = new double[] { r };
        }
    }
}
