using System.Security.Cryptography;
using System.Text;
using Authify.Application.Data;
using Authify.Core.Common;
using Authify.Core.Interfaces;
using Authify.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace Authify.Application.Services;

public class PersonalAccessTokenService : IPersonalAccessTokenService
{
    private readonly IAuthifyDbContext _dbContext;

    public PersonalAccessTokenService(IAuthifyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<OperationResult<CreatePersonalAccessTokenResponse>> CreateAsync(string userId, CreatePersonalAccessTokenRequest request)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return OperationResult<CreatePersonalAccessTokenResponse>.Fail("Invalid user context.");
        if (string.IsNullOrWhiteSpace(request.Name))
            return OperationResult<CreatePersonalAccessTokenResponse>.Fail("Token name is required.");
        if (string.IsNullOrWhiteSpace(request.TenantId))
            return OperationResult<CreatePersonalAccessTokenResponse>.Fail("TenantId is required.");
        if (string.IsNullOrWhiteSpace(request.EndUserId))
            return OperationResult<CreatePersonalAccessTokenResponse>.Fail("EndUserId is required.");

        var rawToken = GenerateRawToken();
        var tokenHash = ComputeSha256(rawToken);

        var entity = new PersonalAccessToken
        {
            UserId = userId,
            TenantId = request.TenantId,
            EndUserId = request.EndUserId,
            Name = request.Name,
            TokenHash = tokenHash,
            TokenPrefix = rawToken[..Math.Min(rawToken.Length, 16)],
            Scopes = string.Join(',', request.Scopes.Distinct(StringComparer.OrdinalIgnoreCase)),
            ExpiresAt = request.ExpiresAt
        };

        await _dbContext.PersonalAccessTokens.AddAsync(entity);
        await _dbContext.SaveChangesAsync();

        return OperationResult<CreatePersonalAccessTokenResponse>.Ok(new CreatePersonalAccessTokenResponse
        {
            Token = rawToken,
            Metadata = ToDto(entity)
        });
    }

    public async Task<OperationResult<List<PersonalAccessTokenDto>>> GetMineAsync(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return OperationResult<List<PersonalAccessTokenDto>>.Fail("Invalid user context.");

        var items = await _dbContext.PersonalAccessTokens
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();

        return OperationResult<List<PersonalAccessTokenDto>>.Ok(items.Select(ToDto).ToList());
    }

    public async Task<OperationResult> RevokeAsync(string userId, Guid tokenId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return OperationResult.Fail("Invalid user context.");

        var token = await _dbContext.PersonalAccessTokens
            .FirstOrDefaultAsync(x => x.Id == tokenId && x.UserId == userId);

        if (token is null)
            return OperationResult.Fail("PAT not found.");

        token.RevokedAt = DateTimeOffset.UtcNow;
        await _dbContext.SaveChangesAsync();
        return OperationResult.Ok();
    }

    public async Task<OperationResult<ResolvePersonalAccessTokenResponse>> ResolveAsync(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return OperationResult<ResolvePersonalAccessTokenResponse>.Fail("Missing PAT token.");

        var tokenHash = ComputeSha256(token);
        var match = await _dbContext.PersonalAccessTokens
            .FirstOrDefaultAsync(x =>
                x.TokenHash == tokenHash &&
                x.RevokedAt == null &&
                (!x.ExpiresAt.HasValue || x.ExpiresAt > DateTimeOffset.UtcNow));

        if (match is null)
            return OperationResult<ResolvePersonalAccessTokenResponse>.Fail("Invalid or expired PAT token.");

        return OperationResult<ResolvePersonalAccessTokenResponse>.Ok(new ResolvePersonalAccessTokenResponse
        {
            TenantId = match.TenantId,
            EndUserId = match.EndUserId
        });
    }

    private static PersonalAccessTokenDto ToDto(PersonalAccessToken source)
    {
        return new PersonalAccessTokenDto
        {
            Id = source.Id,
            Name = source.Name,
            TenantId = source.TenantId,
            EndUserId = source.EndUserId,
            TokenPrefix = source.TokenPrefix,
            Scopes = source.Scopes
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList(),
            CreatedAt = source.CreatedAt,
            ExpiresAt = source.ExpiresAt,
            RevokedAt = source.RevokedAt
        };
    }

    private static string GenerateRawToken()
    {
        Span<byte> bytes = stackalloc byte[32];
        RandomNumberGenerator.Fill(bytes);
        return $"pat_{Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_')}";
    }

    private static string ComputeSha256(string rawToken)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
        return Convert.ToHexString(hash);
    }
}
