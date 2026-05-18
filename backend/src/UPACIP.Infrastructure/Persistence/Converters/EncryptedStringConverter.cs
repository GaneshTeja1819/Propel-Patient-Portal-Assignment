using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using UPACIP.Application.Interfaces;

namespace UPACIP.Infrastructure.Persistence.Converters;

/// <summary>
/// EF Core value converter that transparently encrypts PHI strings before writing
/// to the database and decrypts them on read (AC-001).
///
/// Create one instance per <see cref="AppDbContext"/> and share it across all
/// PHI property mappings within <c>OnModelCreating</c>.
/// </summary>
public sealed class EncryptedStringConverter : ValueConverter<string, string>
{
    public EncryptedStringConverter(IEncryptionService encryptionService)
        : base(
            plaintext => encryptionService.Encrypt(plaintext),
            ciphertext => encryptionService.Decrypt(ciphertext))
    {
    }
}
