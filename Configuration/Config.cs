using SheepHerderAI.AI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SheepHerderAI.Configuration;

/// <summary>
///    ____             __ _                       _   _             
///   / ___|___ _ __   / _(_) __ _ _   _ _ __ __ _| |_(_) ___ _ __  
///  | |   / _ \| '_ \| |_| |/ _` | | | | '__/ _` | __| |/ _ \| '_ \ 
///  | |__| (_) | | | |  _| | (_| | |_| | | | (_| | |_| | (_) | | | |
///   \____\___/|_| |_|_| |_|\__, |\__,_|_|  \__,_|\__|_|\___/|_| |_|
///                          |___/                                   
/// </summary>
internal static class Config
{
    //  ____  _                     
    // / ___|| |__   ___  ___ _ __
    // \___ \| '_ \ / _ \/ _ \ '_ \ 
    //  ___) | | | |  __/  __/ |_) |
    // |____/|_| |_|\___|\___| .__/ 
    //                       |_|    

    /// <summary>
    /// Set this to move the sheep using parallel processing (faster).
    /// </summary>
    internal static bool SheepUseParallelComputation = false;

    /// <summary>
    /// Defines how many sheep we create in a flock for the herding.
    /// </summary>
    internal static int InitialFlockSize = 20;

    /// <summary>
    /// The smaller the number, the closer the dog can get to the sheep before they react.
    /// </summary>
    internal static int SheepHowFarAwayItSpotsTheDog = 140; //90

    /// <summary>
    /// 
    /// </summary>
    internal static double SheepClosenessToMoveToNextWayPoint = 100f;

    /// <summary>
    /// If the sheep is slower than this, we make it stop.
    /// </summary>
    internal static float SheepMinimumSpeedBeforeStop = 0.1f;

    /// <summary>
    /// Sheep can run, but like all animals they are limited by physics and physiology.
    /// This controls the amount sheep can move per frame. It assumes each sheep is 
    /// comparable in performance.
    /// An average is 25mph for a sheep.
    /// </summary>
    internal static float SheepMaximumVelocityInAnyDirection = 0.7f;

    /// <summary>
    /// How close a sheep can sense all the other sheep (makes them clump).
    /// </summary>
    internal static float SheepCloseEnoughToBeAMass = 50;

    // MULTIPLIERS

    /// <summary>
    /// How much strength we apply to cohesion.
    /// </summary>
    internal const float SheepMultiplierCohesion = 0.5f;

    /// <summary>
    /// Use -ve to reflect how cohesion breaks down when a dog is herding.
    /// </summary>
    internal const float SheepMultiplierCohesionThreatenedByDog = -0.7f;

    /// <summary>
    /// How much strength we apply to keep the sheep separated.
    /// </summary>
    internal const float SheepMultiplierSeparation = 0.3f;

    /// <summary>
    /// Use -ve to reflect how separation is increased when a dog is herding.
    /// </summary>
    internal const float SheepMultiplierSeparationThreatenedByDog = -0.9f;

    /// <summary>
    /// How much alignement of movement.
    /// </summary>
    internal const float SheepMultiplierAlignment = 0.1f;

    /// <summary>
    /// Use -ve to reflect how much alignment is messed up when a dog is herding.
    /// </summary>
    internal const float SheepMultiplierAlignmentThreatenedByDog = 0.4f; //-1.9f;

    /// <summary>
    /// How much guidance is listened to. During herding, this is 0.
    /// </summary>
    internal const float SheepMultiplierGuidance = 0f;

    /// <summary>
    /// Use -ve to reflect how guidance is disrupted when a dog is herding.
    /// </summary>
    internal const float SheepMultiplierGuidanceThreatenedByDog = 0f;

    /// <summary>
    /// How much the presence of a predator (dog) impacts the escapel.
    /// </summary>
    internal const float SheepMultiplierEscape = 3f;


    //   ____              
    //  |  _ \  ___   __ _ 
    //  | | | |/ _ \ / _` |
    //  | |_| | (_) | (_| |
    //  |____/ \___/ \__, |
    //               |___/ 

    /// <summary>
    /// Defines how many dogs we run concurrently (with their own flocks of sheep).
    /// </summary>
    internal static int NumberOfAIdogs = 3;

    /// <summary>
    /// How far away the dog sees the sheep.
    /// </summary>
    internal static double DogSensorOfSheepVisionDepthOfVisionInPixels = 140F;

    /// <summary>
    /// Dogs can run, but like all animals they are limited by physics and physiology.
    /// This controls the amount the dog can move per frame. It assumes each dog is 
    /// comparable in performance.
    /// An average is 30mph for a Collie sheep dog.
    /// </summary>
    internal static float DogMaximumVelocityInAnyDirection = 1.3f;

    //     _      ___   
    //    / \    |_ _|  
    //   / _ \    | |   
    //  / ___ \ _ | | _ 
    // /_/   \_(_)___(_)

    /// <summary>
    /// Defines the layers of the perceptron network. Each value is the number of neurons in the layer.
    /// 
    /// [0] is overridden, as it must match input data
    /// [^1] is overridden, as it is 2 (speed, direction)
    /// 
    /// Value of 0 => override with # of input neurons.
    /// </summary>
    internal static int[] AIHiddenLayers = { 3, 5, 10, 2 };

    /// <summary>
    /// This defines what activation functions to use.
    /// </summary>
    internal static ActivationFunctions[] AIactivationFunctions = { ActivationFunctions.TanH,
                                                                    ActivationFunctions.TanH,
                                                                    ActivationFunctions.TanH, 
                                                                    ActivationFunctions.Identity};
}