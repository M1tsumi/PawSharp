#nullable enable
using System;
using System.Collections.Generic;
using FluentAssertions;
using PawSharp.Core.Exceptions;
using PawSharp.Interactivity.Validation;
using Xunit;

namespace PawSharp.Interactivity.Tests;

public class InteractivityValidationTests
{
    [Fact]
    public void RequireNotNullOrEmpty_Null_Throws()
    {
        Action act = () => InteractivityValidation.RequireNotNullOrEmpty(null!, "param");
        act.Should().Throw<ValidationException>().WithMessage("*param*");
    }

    [Fact]
    public void RequireNotNullOrEmpty_Empty_Throws()
    {
        Action act = () => InteractivityValidation.RequireNotNullOrEmpty("", "param");
        act.Should().Throw<ValidationException>().WithMessage("*param*");
    }

    [Fact]
    public void RequireNotNullOrEmpty_Valid_DoesNotThrow()
    {
        Action act = () => InteractivityValidation.RequireNotNullOrEmpty("valid", "param");
        act.Should().NotThrow();
    }

    [Fact]
    public void RequireNotEmpty_EmptyCollection_Throws()
    {
        Action act = () => InteractivityValidation.RequireNotEmpty(new List<int>(), "param");
        act.Should().Throw<ValidationException>().WithMessage("*param*");
    }

    [Fact]
    public void RequireNotEmpty_NonEmptyCollection_DoesNotThrow()
    {
        Action act = () => InteractivityValidation.RequireNotEmpty(new[] { 1 }, "param");
        act.Should().NotThrow();
    }

    [Fact]
    public void RequireCountBetween_OutOfRange_Throws()
    {
        Action act = () => InteractivityValidation.RequireCountBetween(new[] { 1, 2, 3 }, 1, 2, "param");
        act.Should().Throw<ValidationException>();
    }

    [Fact]
    public void RequireCountBetween_InRange_DoesNotThrow()
    {
        Action act = () => InteractivityValidation.RequireCountBetween(new[] { 1, 2 }, 1, 3, "param");
        act.Should().NotThrow();
    }

    [Fact]
    public void RequirePositive_Zero_Throws()
    {
        Action act = () => InteractivityValidation.RequirePositive(0, "param");
        act.Should().Throw<ValidationException>();
    }

    [Fact]
    public void RequirePositive_Negative_Throws()
    {
        Action act = () => InteractivityValidation.RequirePositive(-1, "param");
        act.Should().Throw<ValidationException>();
    }

    [Fact]
    public void RequirePositive_ValidInt_DoesNotThrow()
    {
        Action act = () => InteractivityValidation.RequirePositive(5, "param");
        act.Should().NotThrow();
    }

    [Fact]
    public void RequirePositive_TimeSpanZero_Throws()
    {
        Action act = () => InteractivityValidation.RequirePositive(TimeSpan.Zero, "param");
        act.Should().Throw<ValidationException>();
    }

    [Fact]
    public void RequirePositive_TimeSpanValid_DoesNotThrow()
    {
        Action act = () => InteractivityValidation.RequirePositive(TimeSpan.FromSeconds(30), "param");
        act.Should().NotThrow();
    }

    [Fact]
    public void RequireNotNull_Null_Throws()
    {
        Action act = () => InteractivityValidation.RequireNotNull<string?>(null!, "param");
        act.Should().Throw<ValidationException>();
    }

    [Fact]
    public void RequireNotNull_NotNull_DoesNotThrow()
    {
        Action act = () => InteractivityValidation.RequireNotNull("hello", "param");
        act.Should().NotThrow();
    }
}
