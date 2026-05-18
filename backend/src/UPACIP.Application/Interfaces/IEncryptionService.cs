namespace UPACIP.Application.Interfaces;

/// <summary>
/// Symmetrically encrypts and decrypts PHI strings at the application layer.
/// Implementations must use AES-256-GCM and load the key exclusively from
/// environment variables — never from committed source files (AC-002).
/// </summary>
public interface IEncryptionService
{
    /// <summary>Encrypts <paramref name="plaintext"/> and returns a Base64 ciphertext blob.</summary>
    string Encrypt(string plaintext);

    /// <summary>Decrypts a Base64 ciphertext blob produced by <see cref="Encrypt"/>.</summary>
    string Decrypt(string ciphertext);
}
