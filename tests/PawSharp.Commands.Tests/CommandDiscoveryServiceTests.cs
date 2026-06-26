#nullable enable
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using FluentAssertions;
using PawSharp.Commands.Discovery;
using PawSharp.Commands.Preconditions;
using Xunit;

namespace PawSharp.Commands.Tests;

public class CommandDiscoveryServiceTests
{
    [Fact]
    public void DiscoverCommandModules_ReturnsModuleTypes()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var types = CommandDiscoveryService.DiscoverCommandModules(assembly);
        types.Should().NotBeNull();
    }

    [Fact]
    public void DiscoverCommandModules_EmptyAssembly_ReturnsEmpty()
    {
        var types = CommandDiscoveryService.DiscoverCommandModules(typeof(string).Assembly);
        types.Should().BeEmpty();
    }
}
