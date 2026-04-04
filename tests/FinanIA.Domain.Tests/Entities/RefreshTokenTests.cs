using FinanIA.Domain.Entities;
using FluentAssertions;

namespace FinanIA.Domain.Tests.Entities;

public class RefreshTokenTests
{
    // ── RefreshToken.Create() ────────────────────────────────────────────────

    [Fact]
    public void Create_ShouldGenerateNonEmptyId()
    {
        var token = RefreshToken.Create(Guid.NewGuid(), "hash");

        token.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void Create_ShouldGenerateUniqueIds()
    {
        var userId = Guid.NewGuid();
        var t1 = RefreshToken.Create(userId, "hash1");
        var t2 = RefreshToken.Create(userId, "hash2");

        t1.Id.Should().NotBe(t2.Id);
    }

    [Fact]
    public void Create_ShouldStoreUserId()
    {
        var userId = Guid.NewGuid();
        var token = RefreshToken.Create(userId, "hash");

        token.UserId.Should().Be(userId);
    }

    [Fact]
    public void Create_ShouldStoreTokenHash()
    {
        var token = RefreshToken.Create(Guid.NewGuid(), "myhash");

        token.TokenHash.Should().Be("myhash");
    }

    [Fact]
    public void Create_ShouldSetCreatedAtUtc()
    {
        var before = DateTime.UtcNow;
        var token = RefreshToken.Create(Guid.NewGuid(), "hash");
        var after = DateTime.UtcNow;

        token.CreatedAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }

    [Fact]
    public void Create_ShouldSetExpiresAtAfterExpiryDays()
    {
        var before = DateTime.UtcNow;
        var token = RefreshToken.Create(Guid.NewGuid(), "hash", expiryDays: 7);
        var after = DateTime.UtcNow;

        token.ExpiresAt.Should().BeOnOrAfter(before.AddDays(7)).And.BeOnOrBefore(after.AddDays(7));
    }

    [Fact]
    public void Create_ShouldDefaultTo7DaysExpiry()
    {
        var token = RefreshToken.Create(Guid.NewGuid(), "hash");

        token.ExpiresAt.Should().BeCloseTo(DateTime.UtcNow.AddDays(7), TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Create_ShouldLeaveRevokedAtNull()
    {
        var token = RefreshToken.Create(Guid.NewGuid(), "hash");

        token.RevokedAt.Should().BeNull();
    }

    // ── IsActive ─────────────────────────────────────────────────────────────

    [Fact]
    public void IsActive_ShouldBeTrue_WhenNotRevokedAndNotExpired()
    {
        var token = RefreshToken.Create(Guid.NewGuid(), "hash", expiryDays: 7);

        token.IsActive.Should().BeTrue();
    }

    [Fact]
    public void IsActive_ShouldBeFalse_WhenRevokedAtIsSet()
    {
        var token = RefreshToken.Create(Guid.NewGuid(), "hash", expiryDays: 7);
        token.Revoke();

        token.IsActive.Should().BeFalse();
    }

    [Fact]
    public void IsActive_ShouldBeFalse_WhenExpiresAtIsInPast()
    {
        // expiryDays: 0 means ExpiresAt ≈ UtcNow, which is already or immediately in the past
        var token = RefreshToken.Create(Guid.NewGuid(), "hash", expiryDays: -1);

        token.IsActive.Should().BeFalse();
    }

    // ── Revoke() ─────────────────────────────────────────────────────────────

    [Fact]
    public void Revoke_ShouldSetRevokedAt()
    {
        var token = RefreshToken.Create(Guid.NewGuid(), "hash");
        var before = DateTime.UtcNow;
        token.Revoke();
        var after = DateTime.UtcNow;

        token.RevokedAt.Should().NotBeNull();
        token.RevokedAt!.Value.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }

    [Fact]
    public void Revoke_WithReplacedByToken_ShouldStoreIt()
    {
        var token = RefreshToken.Create(Guid.NewGuid(), "hash");
        token.Revoke("new-token-hash");

        token.ReplacedByToken.Should().Be("new-token-hash");
    }

    [Fact]
    public void Revoke_WithoutReplacedByToken_ShouldLeaveReplacedByTokenNull()
    {
        var token = RefreshToken.Create(Guid.NewGuid(), "hash");
        token.Revoke();

        token.ReplacedByToken.Should().BeNull();
    }
}
