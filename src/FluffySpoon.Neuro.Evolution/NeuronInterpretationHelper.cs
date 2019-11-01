using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FluffySpoon.Neuro.Evolution
{
    public static class NeuronInterpretationHelper
    {
        public static TEnum InterpretAsEnum<TEnum>(double neuronInput) where TEnum : Enum
        {
            var allEnums = GetEnumValues<TEnum>();
            var threshold = 1d / allEnums.Count;

            for (var i = threshold; i < 1; i += threshold)
            {
                if (neuronInput < i)
                    return allEnums[(int)(i * allEnums.Count + threshold / 2d)];
            }

            return allEnums[allEnums.Count - 1];
        }

        public static bool InterpretAsBoolean(double neuronInput)
        {
            return neuronInput > 0.5;
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
}
