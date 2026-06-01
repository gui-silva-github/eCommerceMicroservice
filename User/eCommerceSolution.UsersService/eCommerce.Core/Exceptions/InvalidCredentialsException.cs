namespace eCommerce.Core.Exceptions
{
    public class InvalidCredentialsException : BusinessException
    {
        public InvalidCredentialsException()
            : base("Email e/ou senha inválidos.")
        {
        }
    }
}
