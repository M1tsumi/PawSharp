using Xunit;
using PawSharp.Core.Entities;
using System;

namespace PawSharp.Core.Tests
{
    public class UnitTests
    {
        [Fact]
        public void DiscordEntity_CreatedAt_CalculatedCorrectly()
        {
            // Example Snowflake ID for a known date
            ulong snowflakeId = 175928847299117063; // Corresponds to 2016-04-30 11:18:25.796 UTC
            var entity = new TestDiscordEntity { Id = snowflakeId };
            
            // Expected: 2016-04-30 11:18:25.796 UTC
            var expected = new DateTimeOffset(2016, 4, 30, 11, 18, 25, 796, TimeSpan.Zero);
            Assert.Equal(expected, entity.CreatedAt);
        }
    }

    internal class TestDiscordEntity : DiscordEntity
    {
    }
}