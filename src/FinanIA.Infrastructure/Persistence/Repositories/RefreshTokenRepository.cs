using FinanIA.Domain.Entities;
using FinanIA.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FinanIA.Infrastructure.Persistence.Repositories;

public class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly AppDbContext _context;

    public RefreshTokenRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<RefreshToken?> GetActiveByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("userId cannot be empty.", nameof(userId));

        return await _context.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null && t.ExpiresAt > DateTime.UtcNow)
            .FirstOrDefaultAsync(ct);
    }

    public async Task AddAsync(RefreshToken token, CancellationToken ct = default)
    {
        await _context.RefreshTokens.AddAsync(token, ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(RefreshToken token, CancellationToken ct = default)
    {
        _context.RefreshTokens.Update(token);
        await _context.SaveChangesAsync(ct);
    }

    public async Task RevokeAllByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("userId cannot be empty.", nameof(userId));

        var tokens = await _context.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null)
            .ToListAsync(ct);

        foreach (var token in tokens)
            token.Revoke();

        await _context.SaveChangesAsync(ct);
    }
}
