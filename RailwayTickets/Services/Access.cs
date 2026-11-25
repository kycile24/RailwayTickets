using System.Security.Claims;
using Microsoft.AspNetCore.Identity;

namespace RailwayTickets.Services;

public static class Access
{
    public static int AccountId(this ClaimsPrincipal user) =>
        int.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private static readonly PasswordHasher<string> Hasher = new();
    public static string Hash(string password) => "v1:" + Hasher.HashPassword("", password);
    public static bool Matches(string stored, string password) =>
        stored.StartsWith("v1:") && Hasher.VerifyHashedPassword("", stored[3..], password) != PasswordVerificationResult.Failed;
}
