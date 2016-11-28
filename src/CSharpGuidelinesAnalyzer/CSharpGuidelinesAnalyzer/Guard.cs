﻿using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace CSharpGuidelinesAnalyzer
{
    /// <summary>
    /// Member precondition checks.
    /// </summary>
#pragma warning disable AV1008 // Class should not be static
    public static class Guard
#pragma warning restore AV1008 // Class should not be static
    {
        [AssertionMethod]
        [ContractAnnotation("value: null => halt")]
        public static void NotNull<T>([CanBeNull] [NoEnumeration] T value, [NotNull] [InvokerParameterName] string name)
        {
            if (ReferenceEquals(value, null))
            {
                throw new ArgumentNullException(name);
            }
        }

        [AssertionMethod]
        [ContractAnnotation("value: null => halt")]
        public static void NotNullNorEmpty<T>([CanBeNull] [ItemCanBeNull] IEnumerable<T> value,
            [NotNull] [InvokerParameterName] string name)
        {
            NotNull(value, name);

            if (!value.Any())
            {
                throw new ArgumentException($"'{name}' cannot be empty.", name);
            }
        }

        [AssertionMethod]
        [ContractAnnotation("value: null => halt")]
        public static void NotNullNorWhiteSpace([CanBeNull] string value, [NotNull] [InvokerParameterName] string name)
        {
            NotNull(value, name);

            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException($"'{name}' cannot be empty or contain only whitespace.", name);
            }
        }
    }
}
