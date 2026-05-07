namespace Balda.Infrastructure.Server.Auth
{
    public class AuthResult
    {
        public bool Success;
        public string Message;

        public static AuthResult Ok(string message)
        {
            return new AuthResult
            {
                Success = true,
                Message = message
            };
        }

        public static AuthResult Fail(string message)
        {
            return new AuthResult
            {
                Success = false,
                Message = message
            };
        }
    }
}