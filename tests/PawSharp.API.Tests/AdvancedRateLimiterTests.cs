using System;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using PawSharp.API.RateLimit;
using Xunit;

namespace PawSharp.API.Tests
{
    public class AdvancedRateLimiterTests
    {
        [Fact]
        public void AddAdvancedRateLimiter_Registers_Interface_And_Implementation()
        {
            var services = new ServiceCollection();

            services.AddAdvancedRateLimiter();

            var provider = services.BuildServiceProvider();

            var svc = provider.GetService<IAdvancedRateLimiter>();

            svc.Should().NotBeNull();
            svc.Should().BeOfType<AdvancedRateLimiter>();
        }
    }
}
