using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;

namespace msdyncrmWorkflowTools
{
    public static partial class Utility
    {
        // the operations Numeric Operation accepts, by symbol and by name
        private static readonly Dictionary<string, Func<decimal, decimal, decimal>> NumericOperations =
            new Dictionary<string, Func<decimal, decimal, decimal>>(StringComparer.OrdinalIgnoreCase)
            {
                ["+"] = (a, b) => a + b,
                ["add"] = (a, b) => a + b,
                ["-"] = (a, b) => a - b,
                ["subtract"] = (a, b) => a - b,
                ["*"] = (a, b) => a * b,
                ["x"] = (a, b) => a * b,
                ["multiply"] = (a, b) => a * b,
                ["/"] = (a, b) => a / b,
                ["divide"] = (a, b) => a / b,
                ["%"] = (a, b) => a % b,
                ["mod"] = (a, b) => a % b,
                ["min"] = Math.Min,
                ["max"] = Math.Max
            };

        /// <summary>
        /// Number 1 divided by Number 2, or 0 when Number 2 is 0 (the Divide output of Numeric Functions).
        /// </summary>
        public static decimal DivideOrZero(decimal number1, decimal number2)
        {
            return number2 == 0 ? 0 : number1 / number2;
        }

        /// <summary>
        /// Applies one operation to two numbers: + - * / % (remainder), min or max, or their names (add, subtract,
        /// multiply, divide, mod).
        /// </summary>
        /// <exception cref="InvalidPluginExecutionException">The operation isn't one of those, or it divides by 0.</exception>
        public static decimal NumericOperation(decimal number1, string operation, decimal number2)
        {
            operation = operation?.Trim() ?? string.Empty;

            if (!NumericOperations.TryGetValue(operation, out var apply))
            {
                throw new InvalidPluginExecutionException($"Operation '{operation}' isn't supported. Use +, -, *, /, %, min or max.");
            }

            try
            {
                return apply(number1, number2);
            }
            catch (DivideByZeroException)
            {
                throw new InvalidPluginExecutionException($"Number 2 is 0, so {number1} can't be divided by it.");
            }
            catch (OverflowException)
            {
                throw new InvalidPluginExecutionException($"The result of {number1} {operation} {number2} is too large.");
            }
        }
    }
}
