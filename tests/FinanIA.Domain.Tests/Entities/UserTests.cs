using FinanIA.Domain.Entities;
using FluentAssertions;

namespace FinanIA.Domain.Tests.Entities;

public class UserTests
{
    [Fact]
    public void Create_ShouldLowercaseEmail()
    {
        var user = User.Create("User@Example.COM", "hash");

        user.Email.Should().Be("user@example.com");
    }

    [Fact]
    public void Create_ShouldGenerateNonEmptyGuid()
    {
        var user = User.Create("user@test.com", "hash");

        user.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void Create_ShouldGenerateUniqueGuids()
    {
        var user1 = User.Create("a@test.com", "hash");
        var user2 = User.Create("b@test.com", "hash");

        user1.Id.Should().NotBe(user2.Id);
    }

    [Fact]
    public void Create_ShouldStorePasswordHash()
    {
        var user = User.Create("user@test.com", "myhash");

        user.PasswordHash.Should().Be("myhash");
    }

    [Fact]
    public void Create_ShouldSetCreatedAtUtc()
    {
        var before = DateTime.UtcNow;
        var user = User.Create("user@test.com", "hash");
        var after = DateTime.UtcNow;

        user.CreatedAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }

    [Fact]
    public void Create_ShouldSetEmail_WithMixedCaseAlreadyLower()
    {
        var user = User.Create("already@lowercase.com", "hash");

        user.Email.Should().Be("already@lowercase.com");
    }
}
