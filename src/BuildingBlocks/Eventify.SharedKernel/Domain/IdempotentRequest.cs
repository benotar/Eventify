namespace Eventify.SharedKernel.Domain;

public class IdempotentRequest
{
    public Guid Id { get; set; }
    public string Key { get; set; } = default!;
    public string Method { get; set; } = default!;
    public string Path { get; set; } = default!;
    public string? RequestHash { get; set; }
    public int StatusCode { get; set; }
    public string? ContentType { get; set; }
    public byte[] Body { get; set; } = [];
    public DateTimeOffset CreatedAtUtc { get; set; }
}
