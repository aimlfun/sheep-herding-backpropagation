using System.Security.Cryptography;
using System.Diagnostics;

namespace SheepHerderAI.AI;

/// <summary>
/// Supported "activation" functions for the neuron layer.
/// </summary>
public enum ActivationFunctions { Sigmoid, TanH, ReLU, LeakyReLU, BinaryStep, Identity }

/// <summary>
///    _   _                      _   _   _      _                      _    
///   | \ | | ___ _   _ _ __ __ _| | | \ | | ___| |___      _____  _ __| | __
///   |  \| |/ _ \ | | | '__/ _` | | |  \| |/ _ \ __\ \ /\ / / _ \| '__| |/ /
///   | |\  |  __/ |_| | | | (_| | | | |\  |  __/ |_ \ V  V / (_) | |  |   < 
///   |_| \_|\___|\__,_|_|  \__,_|_| |_| \_|\___|\__| \_/\_/ \___/|_|  |_|\_\
///                                                                          
/// Implementation of a feedforward neural network.
///         
///   A neuron is simply:
///      output = SUM( weight * input ) + bias
///                "weight" amplifies or reduces the input it receives from a neuron that feeds into it. It is from the conceptual dendrite.
///                "bias" is how much is added to the neuron output. (think fires when it reaches a threshold, this lowers the need for the
///                neuron to fire for the output to be "on" full.
/// </summary>
public class NeuralNetwork
{
    #region ACTIVATION FUNCTIONS
    /// <summary>
    /// 
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    private delegate double ActivationFunction(double input);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    private delegate double ActivationDerivativeFunction(double input);

    /// <summary>
    /// 
    /// </summary>
    private readonly ActivationFunction[] activationMethod;

    /// <summary>
    /// 
    /// </summary>
    private readonly ActivationDerivativeFunction[] activationDerivativeMethod;
    #endregion

    /// </summary>
    /// Tracks the neural networks.
    /// <summary>
    internal static Dictionary<int, NeuralNetwork> s_networks = new();

    /// <summary>
    /// The "id" (index) of the brain, should also align to the "id" of the item it is attached.
    /// </summary>
    internal int Id;

    /// <summary>
    /// How many layers of neurons (3+). Do not do 1.
    /// 2 => input connected to output.
    /// 1 => input is output, and feed forward will crash.
    /// </summary>
    internal readonly int[] Layers;

    /// <summary>
    /// The neurons.
    /// [layer][neuron]
    /// </summary>
    internal double[][] Neurons;

    /// <summary>
    /// NN Biases. Either improves or lowers the chance of this neuron fully firing.
    /// [layer][neuron]
    /// </summary>
    private double[][] Biases;

    /// <summary>
    /// NN weights. Reduces or amplifies the output for the relationship between neurons in each layer
    /// [layer][neuron][neuron]
    /// </summary>
    private double[][][] Weights;

    /// <summary>
    /// 
    /// </summary>
    internal readonly List<int> Performance = new();

    /// <summary>
    /// 
    /// </summary>
    internal float Fitness;

    internal bool CannotBeMutated
    {
        get
        {
            return Fitness > 300;
        }
    }

    /// <summary>
    /// LeakyReLU alpha.
    /// </summary>
    private const float alpha = 0.01f;

    #region BACK PROPAGATION
    /// <summary>
    /// 
    /// </summary>
    private readonly float learningRate = 0.01f;
    #endregion

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="_id">Unique ID of the neuron.</param>
    /// <param name="layerDefinition">Defines size of the layers.</param>
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Init*() set the fields.
    internal NeuralNetwork(int _id, int[] layerDefinition, ActivationFunctions[] func, bool addToList = true)
#pragma warning restore CS8618
    {
        // (1) INPUT (2) HIDDEN (3) OUTPUT.
        if (layerDefinition.Length < 2) throw new ArgumentException(nameof(layerDefinition) + " insufficient layers.");

        Id = _id; // used to reference this network

        // copy layerDefinition to Layers.     
        Layers = new int[layerDefinition.Length];

        activationMethod = new ActivationFunction[layerDefinition.Length];
        activationDerivativeMethod = new ActivationDerivativeFunction[layerDefinition.Length];

        for (int layer = 0; layer < layerDefinition.Length; layer++)
        {
            Layers[layer] = layerDefinition[layer];

            GetActivationFunctions(
                func[layer],
                out ActivationFunction activationFunc,
                out ActivationDerivativeFunction derivFunc);

            activationMethod[layer] = activationFunc;
            activationDerivativeMethod[layer] = derivFunc;
        }

        // if layerDefinition is [2,3,2] then...
        // 
        // Neurons :      (o) (o)    <-2  INPUT
        //              (o) (o) (o)  <-3
        //                (o) (o)    <-2  OUTPUT
        //

        InitialiseNeurons();
        InitialiseBiases();
        InitialiseWeights();

        // track all the neurons we created
        if (addToList)
        {
            if (!s_networks.ContainsKey(Id)) s_networks.Add(Id, this); else s_networks[Id] = this;
        }
    }

    #region ACTIVATION / DERIVATIVE FUNCTIONS
    /// <summary>
    /// We assign a function pointer for both, to save resolving the activation functions at runtime.
    /// </summary>
    /// <param name="activationFunctions"></param>
    /// <param name="activationFunc"></param>
    /// <param name="derivativeActivationFunc"></param>
    /// <exception cref="NotImplementedException">You referenced an activation function that is unsupported.</exception>
    private void GetActivationFunctions(ActivationFunctions activationFunctions, out ActivationFunction activationFunc, out ActivationDerivativeFunction derivativeActivationFunc)
    {
        switch (activationFunctions)
        {
            case ActivationFunctions.Sigmoid:
                activationFunc = SigmoidActivationFunction;
                derivativeActivationFunc = DerivativeOfSigmoidDerivationFunction;
                break;

            case ActivationFunctions.TanH:
                activationFunc = TanHActivationFunction;
                derivativeActivationFunc = DerivativeOfTanHActivationFunction;
                break;

            case ActivationFunctions.ReLU:
                activationFunc = ReLUActivationFunction;
                derivativeActivationFunc = DerivativeOfReLUActivationFunction;
                break;

            case ActivationFunctions.LeakyReLU:
                activationFunc = LeakyReLUActivationFunction;
                derivativeActivationFunc = DerivativeOfLeakyReLUActivationFunction;
                break;

            case ActivationFunctions.BinaryStep:
                activationFunc = StepActivationFunction;
                derivativeActivationFunc = DerivativeOfStepActivationFunction;
                break;

            case ActivationFunctions.Identity:
                activationFunc = IdentityActivationFunction;
                derivativeActivationFunc = DerivativeOfIdentityActivationFunction;
                break;

            default:
                // if there is a missing function
                throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Tanh squashes a real-valued number to the range [-1, 1]. It’s non-linear. 
    /// But unlike Sigmoid, its output is zero-centered. Therefore, in practice the tanh non-linearity is always preferred 
    /// to the sigmoid nonlinearity.
    /// 
    /// Activate is TANH         1_       ___
    /// (hyperbolic tangent)     0_      /
    ///                         -1_  ___/
    ///                                | | |
    ///                     -infinity -2 0 2..infinity
    ///                               
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    private static double TanHActivationFunction(double value)
    {
        return (double)Math.Tanh(value);
    }

    /// <summary>
    /// Derivative (for back-propagation of TanH activation function).
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static double DerivativeOfTanHActivationFunction(double value)
    {
        return 1 - value * value;
    }

    /// <summary>
    /// Sigmoid takes a real value as input and outputs another value between 0 and 1. 
    /// It’s easy to work with and has all the nice properties of activation functions: 
    /// it’s non-linear, continuously differentiable, monotonic, and has a fixed output range.
    /// 
    /// Pros
    /// - It is nonlinear in nature. Combinations of this function are also nonlinear!
    /// - It will give an analog activation unlike step function.
    /// - It has a smooth gradient too.
    /// - It’s good for a classifier.
    /// - The output of the activation function is always going to be in range (0,1) compared to(-inf, inf) of linear function.So we have our activations bound in a range.Nice, it won’t blow up the activations then.
    /// 
    /// Cons
    /// - Towards either end of the sigmoid function, the Y values tend to respond very less to changes in X.
    /// - It gives rise to a problem of “vanishing gradients”.
    /// - Its output isn’t zero centered.It makes the gradient updates go too far in different directions. 0 < output< 1, and it makes optimization harder.
    /// - Sigmoids saturate and kill gradients.
    /// - The network refuses to learn further or is drastically slow (depending on use case and until gradient /computation gets hit by floating point value limits).
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    private static double SigmoidActivationFunction(double input)
    {
        double k = (double)Math.Exp(input);
        return k / (1.0f + k);
        // ?
        // def sigmoid(z):
        //   return 1.0 / (1 + np.exp(-z))
    }

    /// <summary>
    /// Derivative (for back-propagation of Sigmoid activation function).
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    private static double DerivativeOfSigmoidDerivationFunction(double input)
    {
        return input * (1 - input);

        // ?
        // def sigmoid_prime(z):
        //   return sigmoid(z) * (1 - sigmoid(z))
    }

    /// <summary>
    /// The rectified linear activation function or ReLU for short is a piecewise linear function that will output 
    /// the input directly if it is positive, otherwise, it will output zero. It has become the default activation 
    /// function for many types of neural networks because a model that uses it is easier to train and often achieves 
    /// better performance.
    /// 
    /// See: https://machinelearningmastery.com/rectified-linear-activation-function-for-deep-learning-neural-networks/#:~:text=The%20rectified%20linear%20activation%20function,otherwise%2C%20it%20will%20output%20zero.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    private double ReLUActivationFunction(double input)
    {
        return input > 0 ? input : 0;
    }

    /// <summary>
    /// Derivative (for back-propagation of ReLU).
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    private double DerivativeOfReLUActivationFunction(double input)
    {
        return input > 0 ? 1 : 0;
    }

    /// <summary>
    /// Returns 1 if positive else 0. The beauty is in making positive outputs return 1 rather a decimal number.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    private static double StepActivationFunction(double input)
    {
        return input >= 0 ? 1 : 0; // f(x) = 1 if x >= 0, or 0 if x < 0
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    private static double DerivativeOfStepActivationFunction(double input)
    {
        return input != 0 ? 0 : 0; // f'(x) = 0 if x != 0, ? if x == 0 ("?" because we don't know which it used).
    }

    /// <summary>
    /// Returns input.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    private static double IdentityActivationFunction(double input)
    {
        return input; // f(x) = x
    }

    /// <summary>
    /// Derivative (of indentity activation function).
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    private static double DerivativeOfIdentityActivationFunction(double input)
    {
        return 1; // f'(x) = 1
    }

    /// <summary>
    /// Leaky Rectified Linear Unit, or Leaky ReLU, is a type of activation function based on a ReLU, but it has 
    /// a small slope for negative values instead of a flat slope. The slope coefficient is determined before 
    /// training, i.e. it is not learnt during training. This type of activation function is popular in tasks 
    /// where we we may suffer from sparse gradients, for example training generative adversarial networks.
    /// 
    /// See: https://paperswithcode.com/method/leaky-relu#:~:text=Leaky%20Rectified%20Linear%20Unit%2C%20or,is%20not%20learnt%20during%20training.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public static double LeakyReLUActivationFunction(double input)
    {
        return Math.Max(alpha * input, input);
    }

    /// <summary>
    /// Derivative (for back-propagation of LeakyReLU).
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static double DerivativeOfLeakyReLUActivationFunction(double value)
    {
        return value > 0 ? 1 : alpha; // return 1 if z > 0 else alpha
    }
    #endregion

    /// <summary>
    /// Create empty storage array for the neurons in the network.
    /// </summary>
    private void InitialiseNeurons()
    {
        List<double[]> neuronsList = new();

        // if layerDefinition is [2,3,2] ..   float[]
        // Neurons :      (o) (o)    <-2  ... [ 0, 0 ]
        //              (o) (o) (o)  <-3  ... [ 0, 0, 0 ]
        //                (o) (o)    <-2  ... [ 0, 0 ]
        //

        for (int layer = 0; layer < Layers.Length; layer++)
        {
            neuronsList.Add(new double[Layers[layer]]);
        }

        Neurons = neuronsList.ToArray();
    }

    /// <summary>
    /// Generate a cryptographic random number between -0.5...+0.5.
    /// </summary>
    /// <returns></returns>
    private static float RandomFloatBetweenMinusHalfToPlusHalf()
    {
        return (float)(RandomNumberGenerator.GetInt32(0, 100000) - 50000) / 100000;
    }

    /// <summary>
    /// initializes and populates biases.
    /// </summary>
    private void InitialiseBiases()
    {
        List<double[]> biasList = new();

        // for each layer of neurons, we have to set biases.
        for (int layer = 1; layer < Layers.Length; layer++)
        {
            double[] bias = new double[Layers[layer]];

            for (int biasLayer = 0; biasLayer < Layers[layer]; biasLayer++)
            {
                bias[biasLayer] = 0;
            }

            biasList.Add(bias);
        }

        Biases = biasList.ToArray();
    }

    /// <summary>
    /// initializes random array for the weights being held in the network.
    /// </summary>
    private void InitialiseWeights()
    {
        List<double[][]> weightsList = new(); // used to construct weights, as dynamic arrays aren't supported

        for (int layer = 1; layer < Layers.Length; layer++)
        {
            List<double[]> layerWeightsList = new();

            int neuronsInPreviousLayer = Layers[layer - 1];

            for (int neuronIndexInLayer = 0; neuronIndexInLayer < Neurons[layer].Length; neuronIndexInLayer++)
            {
                double[] neuronWeights = new double[neuronsInPreviousLayer];

                for (int neuronIndexInPreviousLayer = 0; neuronIndexInPreviousLayer < neuronsInPreviousLayer; neuronIndexInPreviousLayer++)
                {
                    neuronWeights[neuronIndexInPreviousLayer] = RandomFloatBetweenMinusHalfToPlusHalf();
                }

                layerWeightsList.Add(neuronWeights);
            }

            weightsList.Add(layerWeightsList.ToArray());
        }

        Weights = weightsList.ToArray();
    }

    /// <summary>
    /// 
    /// </summary>
    internal static void Save()
    {
        foreach (NeuralNetwork n in s_networks.Values)
        {
            n.Save($@"c:\temp\sheep-UHA{n.Id}.ai");
        }

        MessageBox.Show("Model Saved.");
    }

    /// <summary>
    /// Feed forward, inputs >==> outputs.
    /// 
    ///     input       input
    ///         |          |
    ///         v[0] w[0]  v[1] w[1]              w = weight
    /// l0    ( 0 )      ( 1 )                    v = value
    ///         |    \  /  |                      b = bias
    ///         |     /    |     
    ///         |   /   \  |
    /// l1    ( 0 )      ( 1 )
    ///         |          |
    ///         |     b(1) |                      l0 node 0                    l0 node 1            bias of l1 node 1
    ///    b(0) |          v[1] = Activate( w[l0][1][0] * v[l0][0] +  w[l0][1][1] * v[l0][1]   +   b[l1][1] ) 
    ///         |                  l0 node 0                l0 node 1                     bias of l1 node 0
    ///         v[0] = Activate( w[l0][0][0] * v[l0][0] +  w[l0][0][1] * v[l0][1]   +   b[l1][0] )
    ///       
    /// 
    /// </summary>
    /// <param name="inputs"></param>
    /// <returns></returns>
    internal double[] FeedForward(double[] inputs)
    {
        // put the INPUT values into layer 0 neurons
        for (int i = 0; i < inputs.Length; i++)
        {
            Neurons[0][i] = inputs[i];
        }

        // we start on layer 1 as we are computing values from prior layers (layer 0 is inputs)

        for (int layer = 1; layer < Layers.Length; layer++)
        {
            for (int neuronIndexForLayer = 0; neuronIndexForLayer < Layers[layer]; neuronIndexForLayer++)
            {
                // sum of outputs from the previous layer
                double value = 0f;

                for (int neuronIndexInPreviousLayer = 0; neuronIndexInPreviousLayer < Layers[layer - 1]; neuronIndexInPreviousLayer++)
                {
                    // remember: the "weight" amplifies or reduces, so we take the output of the prior neuron and "amplify/reduce" it's output here
                    value += Weights[layer - 1][neuronIndexForLayer][neuronIndexInPreviousLayer] * Neurons[layer - 1][neuronIndexInPreviousLayer];
                }

                // any neuron fires or not based on the input. The point of a bias is to move the activation up or down.
                // e.g. the value could be 0.3, adding a bias of 0.5 takes it to 0.8. You might think why not just use the weights to achieve this
                // but remember weights are individual per prior layer neurons, the bias affects the SUM() of them.

                Neurons[layer][neuronIndexForLayer] = activationMethod[layer](value + Biases[layer - 1][neuronIndexForLayer]);
            }
        }

        return Neurons[^1]; // final* layer contains OUTPUT
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    internal float AverageFitness()
    {
        if (Performance.Count == 0) return Fitness;

        int sum = 0;
       
        // we're doing most recent 5, because mutation makes prior data less meaningful and
        // is a better indicator of suitability.
        int minf = Math.Max(0, Performance.Count - 5);

        int[] perfArray = Performance.ToArray();

        for (int f = minf; f < Performance.Count; f++) sum += perfArray[f];
        
        return sum / (Performance.Count-minf);
    }

    
    /// <summary>
    /// Copies from one NN to another
    /// </summary>
    /// <param name="neuralNetworkToCloneFrom"></param>
    /// <param name="neuralNetworkCloneTo"></param>
    internal static void CopyFromTo(NeuralNetwork neuralNetworkToCloneFrom, NeuralNetwork neuralNetworkCloneTo)
    {
        for (int layerIndex = 0; layerIndex < neuralNetworkToCloneFrom.Biases.Length; layerIndex++)
        {
            for (int neuronIndex = 0; neuronIndex < neuralNetworkToCloneFrom.Biases[layerIndex].Length; neuronIndex++)
            {
                neuralNetworkCloneTo.Biases[layerIndex][neuronIndex] = neuralNetworkToCloneFrom.Biases[layerIndex][neuronIndex];
            }
        }

        for (int layerIndex = 0; layerIndex < neuralNetworkToCloneFrom.Weights.Length; layerIndex++)
        {
            for (int neuronIndexInLayer = 0; neuronIndexInLayer < neuralNetworkToCloneFrom.Weights[layerIndex].Length; neuronIndexInLayer++)
            {
                for (int neuronIndexInPreviousLayer = 0; neuronIndexInPreviousLayer < neuralNetworkToCloneFrom.Weights[layerIndex][neuronIndexInLayer].Length; neuronIndexInPreviousLayer++)
                {
                    neuralNetworkCloneTo.Weights[layerIndex][neuronIndexInLayer][neuronIndexInPreviousLayer] = neuralNetworkToCloneFrom.Weights[layerIndex][neuronIndexInLayer][neuronIndexInPreviousLayer];
                }
            }
        }
    }


    /// <summary>
    /// Saves the biases and weights within the network to a file.
    /// </summary>
    /// <param name="path"></param>
    internal void Save(string path)
    {
        if (File.Exists(path)) File.Delete(path);

        using StreamWriter writer = new(path, false);

        writer.WriteLine(Fitness);

        // write the biases
        for (int layerIndex = 0; layerIndex < Biases.Length; layerIndex++)
        {
            for (int neuronIndex = 0; neuronIndex < Biases[layerIndex].Length; neuronIndex++)
            {
                writer.WriteLine(Biases[layerIndex][neuronIndex]);
            }
        }

        // write the weights
        for (int layerIndex = 0; layerIndex < Weights.Length; layerIndex++)
        {
            for (int neuronIndexInLayer = 0; neuronIndexInLayer < Weights[layerIndex].Length; neuronIndexInLayer++)
            {
                for (int neuronIndexInPreviousLayer = 0; neuronIndexInPreviousLayer < Weights[layerIndex][neuronIndexInLayer].Length; neuronIndexInPreviousLayer++)
                {
                    writer.WriteLine(Weights[layerIndex][neuronIndexInLayer][neuronIndexInPreviousLayer]);
                }
            }
        }

        writer.Close();
    }

    /// <summary>
    /// This loads the biases and weights from within a file into the neural network.
    /// </summary>
    /// <param name="path"></param>
    internal bool Load(string path)
    {
        if (!File.Exists(path)) return false;

        string[] ListLines = File.ReadAllLines(path);

        int index = 0;

        Fitness = float.Parse(ListLines[index++]);

        try
        {
            for (int layerIndex = 0; layerIndex < Biases.Length; layerIndex++)
            {
                for (int neuronIndex = 0; neuronIndex < Biases[layerIndex].Length; neuronIndex++)
                {
                    Biases[layerIndex][neuronIndex] = double.Parse(ListLines[index++]);
                }
            }

            for (int layerIndex = 0; layerIndex < Weights.Length; layerIndex++)
            {
                for (int neuronIndexInLayer = 0; neuronIndexInLayer < Weights[layerIndex].Length; neuronIndexInLayer++)
                {
                    for (int neuronIndexInPreviousLayer = 0; neuronIndexInPreviousLayer < Weights[layerIndex][neuronIndexInLayer].Length; neuronIndexInPreviousLayer++)
                    {
                        Weights[layerIndex][neuronIndexInLayer][neuronIndexInPreviousLayer] = double.Parse(ListLines[index++]);
                    }
                }
            }
        }
        catch (Exception)
        {
            MessageBox.Show("Unable to load .AI files\nThe most likely reason is that the number of neurons does not match the saved AI file.");
            return false;
        }

        return true;
    }

    /// <summary>
    /// Perform back propagation.
    /// </summary>
    /// <param name="inputs"></param>
    /// <param name="expected"></param>
    public float BackPropagate(double[] inputs, double[] expected)
    {
        double[] output = FeedForward(inputs); // runs feed forward to ensure neurons are populated correctly

        float cost = 0;

        for (int i = 0; i < output.Length; i++) cost += (float)Math.Pow(output[i] - expected[i], 2);
        cost /= 2;

        List<double[]> gammaList = new();

        for (int i = 0; i < Layers.Length; i++)
        {
            gammaList.Add(new double[Layers[i]]);
        }

        double[][] gamma = gammaList.ToArray(); // gamma initialization

        int layersMinus2 = Layers.Length - 2;
        int layersMinus1 = Layers.Length - 1;

        for (int i = 0; i < output.Length; i++)
        {
            gamma[layersMinus1][i] = (output[i] - expected[i]) * activationDerivativeMethod[layersMinus2](output[i]); // Gamma calculation
        }

        for (int i = 0; i < Layers[^1]; i++) // calculates the w' and b' for the last layer in the network
        {
            Biases[layersMinus2][i] -= gamma[layersMinus1][i] * learningRate;

            for (int j = 0; j < Layers[^2]; j++)
            {
                Weights[layersMinus2][i][j] -= gamma[layersMinus1][i] * Neurons[layersMinus2][j] * learningRate; //*learning 
            }
        }

        for (int i = Layers.Length - 2; i > 0; i--) // runs on all hidden layers
        {
            int layer = i - 1;
            int layerPlus1 = i + 1;

            for (int j = 0; j < Layers[i]; j++) // outputs
            {
                gamma[i][j] = 0;

                for (int k = 0; k < gamma[layerPlus1].Length; k++)
                {
                    gamma[i][j] += gamma[layerPlus1][k] * Weights[i][k][j];
                }

                gamma[i][j] *= activationDerivativeMethod[layer](Neurons[i][j]); //calculate gamma

                Biases[layer][j] -= gamma[i][j] * learningRate; // modify biases of network

                for (int k = 0; k < Layers[layer]; k++) // iterate over inputs to layer
                {
                    Weights[layer][j][k] -= gamma[i][j] * Neurons[layer][k] * learningRate; // modify weights of network
                }
            }
        }

        return cost;
    }
}
