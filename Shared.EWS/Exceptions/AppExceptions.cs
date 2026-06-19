namespace Shared.EWS.Exceptions
{
    public class InvalidCredentialsException : Exception
    {
        public InvalidCredentialsException(string message)
            : base(message) { }
    }

    public class AccountInactiveException : Exception
    {
        public AccountInactiveException(string message)
            : base(message) { }
    }

    public class TokenException : Exception
    {
        public TokenException(string message)
            : base(message) { }
    }

    public class ResetTokenException : Exception
    {
        public ResetTokenException(string message)
            : base(message) { }
    }

    public class DuplicateRecordException : Exception
    {
        public DuplicateRecordException(string message)
            : base(message) { }
    }

    public class NotFoundException : Exception
    {
        public NotFoundException(string message)
            : base(message) { }
    }

    public class ForbiddenException : Exception
    {
        public ForbiddenException(string message)
            : base(message) { }
    }
}