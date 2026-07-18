namespace Eventify.Identity.Web.Endpoints.User;

public record RegisterUserRequest(string Email, string FirstName, string LastName, string Password);
