using Server.Contracts.Referrals;
using Server.Models;

namespace Server.Services.Referrals;

/// <summary>Validates bounded uploads and removes staged files unless the referral is committed.</summary>
public class ReferralUploadService(ReferralService referrals, IConfiguration configuration)
{
    public const long PerFileLimit = 10 * 1024 * 1024;
    public const long TotalLimit = 25 * 1024 * 1024;

    public async Task<CreateReferralResult> CreateAsync(CreateReferralRequest request,
        IReadOnlyList<IFormFile> files, CancellationToken cancellationToken)
    {
        CreateReferralResult Invalid() => new() { Errors = new() {
            ["attachments"] = ["Kies maximaal 5 PDF-, JPG- of PNG-bestanden van maximaal 10 MB per bestand en 25 MB samen."] } };
        if (files.Count > 5 || files.Any(f => f.Length <= 0 || f.Length > PerFileLimit) ||
            files.Sum(f => f.Length) > TotalLimit) return Invalid();

        // LocalApplicationData is outside the web root and repository. No static-file endpoint exposes it.
        var root = Path.GetFullPath(configuration["Attachments:StoragePath"] ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MediSharp", "attachments"));
        var directory = Path.Combine(root, Guid.NewGuid().ToString("N"));
        var metadata = new List<ReferralAttachment>();
        var committed = false;
        try
        {
            foreach (var file in files)
            {
                var name = file.FileName;
                if (name.Length > 255 || name != Path.GetFileName(name) ||
                    name.IndexOfAny(['/', '\\', ':']) >= 0 || name.Any(char.IsControl)) return Invalid();
                await using var source = file.OpenReadStream();
                using var buffer = new MemoryStream();
                var chunk = new byte[81920];
                int count;
                while ((count = await source.ReadAsync(chunk, cancellationToken)) > 0)
                {
                    if (buffer.Length + count > PerFileLimit) return Invalid();
                    await buffer.WriteAsync(chunk.AsMemory(0, count), cancellationToken);
                }
                if (buffer.Length != file.Length) return Invalid();
                var data = buffer.ToArray();
                var extension = Path.GetExtension(name).ToLowerInvariant();
                var type = DetectType(data, extension);
                if (type is null) return Invalid();
                Directory.CreateDirectory(directory);
                var key = Guid.NewGuid().ToString("N");
                await File.WriteAllBytesAsync(Path.Combine(directory, key), data, cancellationToken);
                metadata.Add(new ReferralAttachment { FileName = name, ContentType = type,
                    StorageKey = Path.Combine(Path.GetFileName(directory), key), SizeInBytes = data.Length });
            }
            var result = await referrals.CreateWithAttachmentsAsync(request, metadata, cancellationToken);
            committed = result.Errors.Count == 0;
            return result;
        }
        finally
        {
            if (!committed && Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }

    // Signature and extension must agree. This is not malware scanning or a medical image viewer.
    private static string? DetectType(byte[] data, string extension)
    {
        if (extension == ".pdf" && data.AsSpan().StartsWith("%PDF-"u8) &&
            data.AsSpan(Math.Max(0, data.Length - 1024)).IndexOf("%%EOF"u8) >= 0) return "application/pdf";
        if (extension is ".jpg" or ".jpeg" && data.Length >= 4 && data[0] == 255 && data[1] == 216 &&
            data[^2] == 255 && data[^1] == 217) return "image/jpeg";
        if (extension == ".png" && data.AsSpan().StartsWith(new byte[] {137,80,78,71,13,10,26,10}) &&
            data.Length >= 24 && data.AsSpan(data.Length - 8, 4).SequenceEqual("IEND"u8)) return "image/png";
        return null;
    }
}
