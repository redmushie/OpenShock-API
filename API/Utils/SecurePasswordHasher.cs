﻿using System.Security.Cryptography;

namespace OpenShock.API.Utils;

public static class SecurePasswordHasher
{
    /// <summary>
    ///     Size of salt.
    /// </summary>
    private const int SaltSize = 16;

    /// <summary>
    ///     Size of hash.
    /// </summary>
    private const int HashSize = 32;

    private const string DefaultName = "USER";

    /// <summary>
    ///     Creates a hash from a password.
    /// </summary>
    /// <param name="password">The password.</param>
    /// <param name="iterations">Number of iterations.</param>
    /// <param name="version">Version of the hash</param>
    /// <param name="customName">Custom hash prefix</param>
    /// <returns>The hash.</returns>
    public static string Hash(string password, int iterations = 10000, uint version = 1,
        string customName = DefaultName)
    {
        // Create salt
        var salt = RandomNumberGenerator.GetBytes(SaltSize);

        // Create hash
        using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA512);
        var hash = pbkdf2.GetBytes(HashSize);

        // Combine salt and hash
        var hashBytes = new byte[SaltSize + HashSize];
        Array.Copy(salt, 0, hashBytes, 0, SaltSize);
        Array.Copy(hash, 0, hashBytes, SaltSize, HashSize);

        // Convert to base64
        var base64Hash = Convert.ToBase64String(hashBytes);

        // Format hash with extra information
        return $"{customName}${version}${iterations}${base64Hash}";
    }

    /// <summary>
    ///     Checks if hash is supported.
    /// </summary>
    /// <param name="hashString">The hash.</param>
    /// <param name="customName">Custom hash prefix</param>
    ///     /// <param name="version">Version of the hash</param>
    /// <returns>Is supported?</returns>
    private static bool IsHashSupported(string hashString, uint version = 1, string customName = DefaultName) =>
        hashString.Contains($"{customName}${version}$");


    /// <summary>
    ///     Verifies a password against a hash.
    /// </summary>
    /// <param name="password">The password.</param>
    /// <param name="hashedPassword">The hash.</param>
    /// <param name="customName">Custom hash prefix</param>
    /// <param name="version">Version of the hash</param>
    /// <returns>Could be verified?</returns>
    public static bool Verify(string password, string hashedPassword, uint version = 1, string customName = DefaultName)
    {
        // Check hash
        if (!IsHashSupported(hashedPassword, version, customName))
            throw new NotSupportedException("The hash type is not supported");

        // Extract iteration and Base64 string
        var splittedHashString = hashedPassword.Replace($"{customName}${version}$", "").Split('$');
        var iterations = int.Parse(splittedHashString[0]);
        var base64Hash = splittedHashString[1];

        // Get hash bytes
        var hashBytes = Convert.FromBase64String(base64Hash);

        // Get salt
        var salt = new byte[SaltSize];
        Array.Copy(hashBytes, 0, salt, 0, SaltSize);

        // Create hash with given salt
        using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA512);
        var hash = pbkdf2.GetBytes(HashSize);

        // Get result
        for (var i = 0; i < HashSize; i++)
            if (hashBytes[i + SaltSize] != hash[i])
                return false;
        return true;
    }
}