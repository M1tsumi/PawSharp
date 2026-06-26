#nullable enable
using FluentAssertions;
using PawSharp.Voice.DAVE.MLS.Crypto;
using Xunit;

namespace PawSharp.Voice.Tests;

public class CryptoProviderFactoryTests
{
    [Fact]
    public void Instance_ReturnsBouncyCastleProvider()
    {
        var provider = CryptoProviderFactory.Instance;
        provider.Should().BeOfType<BouncyCastleCryptoProvider>();
    }

    [Fact]
    public void Instance_IsNotNull()
    {
        var provider = CryptoProviderFactory.Instance;
        provider.Should().NotBeNull();
    }

    [Fact]
    public void Instance_SameType()
    {
        var p1 = CryptoProviderFactory.Instance;
        var p2 = CryptoProviderFactory.Instance;
        p1.GetType().Should().Be(p2.GetType());
    }

    [Fact]
    public void Instance_Singleton()
    {
        var p1 = CryptoProviderFactory.Instance;
        var p2 = CryptoProviderFactory.Instance;
        p1.Should().BeSameAs(p2);
    }
}
