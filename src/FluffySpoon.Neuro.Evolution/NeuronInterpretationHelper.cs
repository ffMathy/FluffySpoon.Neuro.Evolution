using System;
using System.Collections.Generic;
using System.Linq;

namespace FluffySpoon.Neuro.Evolution;

/// <summary>
/// Helpers for converting neural network outputs (which are in the range [0, 1]) into meaningful values and back.
/// </summary>
public static class NeuronInterpretationHelper
{
    public static TEnum InterpretAsEnum<TEnum>(double neuronOutput) where TEnum : Enum
    {
        var allEnums = GetEnumValues<TEnum>();
        var index = (int)(neuronOutput * allEnums.Count);
        return allEnums[Math.Clamp(index, 0, allEnums.Count - 1)];
    }

    public static bool InterpretAsBoolean(double neuronOutput)
    {
        return neuronOutput > 0.5;
    }

    public static double ConvertEnumToNeuronInput<TEnum>(TEnum enumValue) where TEnum : Enum
    {
        var allEnums = GetEnumValues<TEnum>();
        var threshold = 1d / allEnums.Count;

        return (allEnums.IndexOf(enumValue) * threshold) + (threshold / 2);
    }

    private static IList<TEnum> GetEnumValues<TEnum>() where TEnum : Enum
    {
        return typeof(TEnum)
            .GetEnumValues()
            .Cast<TEnum>()
            .ToList();
    }
}
