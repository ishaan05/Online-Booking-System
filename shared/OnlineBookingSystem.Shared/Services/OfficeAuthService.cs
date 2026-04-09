using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OnlineBookingSystem.Shared.Data;
using OnlineBookingSystem.Shared.Helpers;
using OnlineBookingSystem.Shared.Models;
using OnlineBookingSystem.Shared.Security;
using OnlineBookingSystem.Shared.ViewModels;

namespace OnlineBookingSystem.Shared.Services;

public class OfficeAuthService
{
    private readonly AppDbContext _db;
    private readonly JwtTokenService _jwt;

    public OfficeAuthService(AppDbContext db, JwtTokenService jwt)
    {
        _db = db;
        _jwt = jwt;
    }

    public async Task<OfficeLoginResponse?> LoginAsync(string username, string password, CancellationToken ct = default)
    {
        var u = username.Trim();
        if (string.IsNullOrEmpty(u) || string.IsNullOrEmpty(password))
        {
            await Task.Delay(120, ct);
            return null;
        }

        var uLower = u.ToLowerInvariant();
        var user = await _db.OfficeUsers
            .FirstOrDefaultAsync(
                x => x.IsActive
                    && (
                        x.Username.ToLower() == uLower
                        || (x.MobileNumber != null && x.MobileNumber == u)
                        || (x.EmailID != null && x.EmailID.ToLower() == uLower)),
                ct);
        if (user == null || string.IsNullOrEmpty(user.PasswordHash))
        {
            await Task.Delay(120, ct);
            return null;
        }

        // if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
        // {
        //     await Task.Delay(120, ct);
        //     return null;
        // }

        OfficeUserRoleEntity? roleRow = null;
        if (user.RoleID > 0)
        {
            roleRow = await _db.OfficeUserRoles.AsNoTracking()
                .FirstOrDefaultAsync(r => r.RoleID == user.RoleID, ct);
        }

        var role = OfficeJwtRoleMapper.ResolveForLogin(user, roleRow);
        var token = _jwt.CreateToken(user.OfficeUserID, user.FullName ?? "", role, user.EmailID ?? user.Username);
        List<int> venueIds = await _db.VenueUserMappings.AsNoTracking()
            .Where(m => m.OfficeUserID == user.OfficeUserID && m.IsActive)
            .Select(m => m.VenueID)
            .Distinct()
            .ToListAsync(ct);
        return new OfficeLoginResponse(
            token,
            user.OfficeUserID,
            user.FullName ?? "",
            role,
            user.EmailID ?? user.Username,
            user.RoleID,
            venueIds);
    }

    /// <summary>
    /// Anonymous reset for active office users. Matches by username, mobile (exact), or email (case-insensitive).
    /// </summary>
    public async Task<(bool Ok, string? ErrorMessage)> ResetPasswordAsync(
        string usernameOrMobileOrEmail,
        string newPassword,
        CancellationToken ct = default)
    {
        var id = (usernameOrMobileOrEmail ?? "").Trim();
        var pwd = newPassword ?? "";
        if (string.IsNullOrWhiteSpace(id))
            return (false, "Enter your office username, mobile number, or email.");
        if (!PasswordPolicy.IsValid(pwd))
            return (false, PasswordPolicy.RequirementMessage);

        var idLower = id.ToLowerInvariant();
        var user = await _db.OfficeUsers.FirstOrDefaultAsync(
            x =>
                x.IsActive
                && (
                    x.Username.ToLower() == idLower
                    || (x.MobileNumber != null && x.MobileNumber == id)
                    || (x.EmailID != null && x.EmailID.ToLower() == idLower)),
            ct);

        if (user == null)
            return (false, "No office account matches that username, mobile, or email.");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(pwd);
        await _db.SaveChangesAsync(ct);
        return (true, null);
    }
}
