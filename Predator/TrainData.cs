//#define spinInCirclesForTesting

namespace SheepHerderAI.Predator;

internal class TrainData
{
    internal double[] inputs;
    internal double[] outputs;

    internal TrainData(double[] inputs, double[] outputs)
    {
        this.inputs = inputs;
        this.outputs = outputs;
    }
}
