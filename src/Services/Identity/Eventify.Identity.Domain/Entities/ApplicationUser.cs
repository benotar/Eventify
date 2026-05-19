using Microsoft.AspNetCore.Identity;

namespace Eventify.Identity.Domain.Entities;

public sealed class ApplicationUser : IdentityUser<Guid>
{
    public string FirstName { get; private set; } = default!;

    public string LastName { get; private set; } = default!;

    public DateTimeOffset CreatedAt { get; private set; }

    // For EF Core
    private ApplicationUser()
    {
    }

    public ApplicationUser(string email, string firstName, string lastName) : base(email)
    {
        Id = Guid.CreateVersion7();
        FirstName = firstName;
        LastName = lastName;
        Email = email;
        CreatedAt = DateTimeOffset.UtcNow;
    }
}
