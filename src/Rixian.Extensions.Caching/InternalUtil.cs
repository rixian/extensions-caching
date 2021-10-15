// Copyright (c) Rixian. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE file in the project root for full license information.

namespace Rixian.Extensions.Caching
{
    using System;
    using System.Diagnostics;
    using System.Reflection;

    /// <summary>
    /// Internal library utilities.
    /// </summary>
    internal class InternalUtil
    {
        /// <summary>
        /// Defines a static instance of ActivitySource for use in the library.
        /// </summary>
        internal static readonly ActivitySource ActivitySource = new("Rixian.Extensions.Caching", (Attribute.GetCustomAttribute(Assembly.GetAssembly(typeof(InternalUtil)), typeof(AssemblyInformationalVersionAttribute)) as AssemblyInformationalVersionAttribute)?.InformationalVersion);

        /// <summary>
        /// Initializes a new instance of the <see cref="InternalUtil"/> class.
        /// </summary>
        protected InternalUtil()
        {
        }
    }
}
