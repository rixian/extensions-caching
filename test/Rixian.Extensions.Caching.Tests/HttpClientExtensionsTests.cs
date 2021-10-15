// Copyright (c) Rixian. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE file in the project root for full license information.

namespace Rixian.Extensions.Caching.Tests
{
    using Xunit;
    using Xunit.Abstractions;

    public class HttpClientExtensionsTests
    {
        private readonly ITestOutputHelper logger;

        public HttpClientExtensionsTests(ITestOutputHelper logger)
        {
            this.logger = logger;
        }

        [Fact]
        public void Placeholder_Default_Success()
        {
            this.logger.WriteLine("Ran placeholder test.");
        }
    }
}
