#nullable enable
using System;
using FluentAssertions;
using PawSharp.Cache.Exceptions;
using Xunit;

namespace PawSharp.Cache.Tests;

public class CacheExceptionTests
{
    [Fact]
    public void CacheException_CanBeThrownAndCaught()
    {
        Action act = () => throw new CacheException("test error");
        act.Should().Throw<CacheException>().WithMessage("test error");
    }

    [Fact]
    public void CacheException_WithInnerException()
    {
        var inner = new InvalidOperationException("inner");
        Action act = () => throw new CacheException("outer", inner);
        act.Should().Throw<CacheException>().WithMessage("outer").WithInnerException<InvalidOperationException>();
    }
}
