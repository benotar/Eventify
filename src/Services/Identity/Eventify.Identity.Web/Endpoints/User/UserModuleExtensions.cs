using Eventify.Identity.Application.User.RegisterUser;

namespace Eventify.Identity.Web.Endpoints.User;

public static class UserModuleExtensions
{
    extension(RegisterUserRequest request)
    {
        public RegisterUserCommand ToCommand()
        {
            return new RegisterUserCommand(request.Email, request.FirstName, request.LastName, request.Password);
        }
    }
}
