using System.Security.Cryptography;
using System.Text;
using UPACIP.Application.Interfaces;

namespace UPACIP.Infrastructure.Security;

/// <summary>
/// AES-256-GCM symmetric encryption service for PHI columns (AC-001, AC-002).
///
/// Key source: <c>PHI_ENCRYPTION_KEY</c> environment variable, expected as a
/// Base64-encoded 32-byte (256-bit) value. An <see cref="InvalidOperationException"/>
/// is thrown at construction time — surfacing at application startup — if the
/// key is absent or does not decode to exactly 32 bytes.
///
/// Wire format: Base64( nonce[12] || tag[16] || ciphertext[n] )
/// </summary>
public sealed class AesEncryptionService : IEncryptionService
{
    private const int NonceSize = 12;  // 96-bit nonce — NIST SP 800-38D recommended size for AES-GCM
    private const int TagSize = 16;    // 128-bit authentication tag — maximum GCM tag length

    private readonly byte[] _key;

    public AesEncryptionService()
    {
        var raw = Environment.GetEnvironmentVariable("PHI_ENCRYPTION_KEY")
            ?? throw new InvalidOperationException(
                "PHI_ENCRYPTION_KEY environment variable is not set. " +
                "Set it to a Base64-encoded 32-byte AES-256 key before starting the application.");

        try
        {
            _key = Convert.FromBase64String(raw);
        }
        catch (FormatException)
        {
            throw new InvalidOperationException(
                "PHI_ENCRYPTION_KEY is not valid Base64. " +
                "Provide a Base64-encoded 32-byte value.");
        }

        if (_key.Length != 32)
            throw new InvalidOperationException(
                $"PHI_ENCRYPTION_KEY must decode to exactly 32 bytes (AES-256). " +
                $"Decoded length was {_key.Length} bytes.");
    }

    /// <inheritdoc />
    public string Encrypt(string plaintext)
    {
        var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);

        var nonce = new byte[NonceSize];
        RandomNumberGenerator.Fill(nonce);

        var tag = new byte[TagSize];
        var ciphertext = new byte[plaintextBytes.Length];

        using var aes = new AesGcm(_key, TagSize);
        aes.Encrypt(nonce, plaintextBytes, ciphertext, tag);

        // Concatenate: nonce (12) || tag (16) || ciphertext
        var combined = new byte[NonceSize + TagSize + ciphertext.Length];
        nonce.CopyTo(combined, 0);
        tag.CopyTo(combined, NonceSize);
        ciphertext.CopyTo(combined, NonceSize + TagSize);

        return Convert.ToBase64String(combined);
    }

    /// <inheritdoc />
    public string Decrypt(string ciphertext)
    {
        byte[] combined;
        try
        {
            combined = Convert.FromBase64String(ciphertext);
        }
        catch (FormatException ex)
        {
            throw new CryptographicException("Ciphertext is not valid Base64.", ex);
        }

        if (combined.Length < NonceSize + TagSize)
            throw new CryptographicException(
                $"Ciphertext is too short. Expected at least {NonceSize + TagSize} bytes, got {combined.Length}.");

        var nonce = combined[..NonceSize];
        var tag = combined[NonceSize..(NonceSize + TagSize)];
        var encryptedData = combined[(NonceSize + TagSize)..];

        var plaintext = new byte[encryptedData.Length];

        using var aes = new AesGcm(_key, TagSize);
        aes.Decrypt(nonce, encryptedData, tag, plaintext);

        return Encoding.UTF8.GetString(plaintext);
    }
}
