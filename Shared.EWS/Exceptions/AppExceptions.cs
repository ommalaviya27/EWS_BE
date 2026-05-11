namespace Shared.EWS.Exceptions
{
    /// <summary>Wrong email or password during login.</summary>
    public class InvalidCredentialsException : Exception
    {
        public InvalidCredentialsException()
            : base("Invalid email or password.") { }

        public InvalidCredentialsException(string message)
            : base(message) { }
    }

    /// <summary>User account exists but is disabled / inactive.</summary>
    public class AccountInactiveException : Exception
    {
        public AccountInactiveException()
            : base("Your account is inactive. Please contact support.") { }

        public AccountInactiveException(string message)
            : base(message) { }
    }

    /// <summary>Access token is missing, malformed, or expired.</summary>
    public class TokenException : Exception
    {
        public TokenException()
            : base("Invalid or expired access token.") { }

        public TokenException(string message)
            : base(message) { }
    }

    /// <summary>Password reset token is invalid or expired.</summary>
    public class ResetTokenException : Exception
    {
        public ResetTokenException()
            : base("Invalid or expired password reset token.") { }

        public ResetTokenException(string message)
            : base(message) { }
    }

    /// <summary>A record with the same unique key already exists (e.g. duplicate email).</summary>
    public class DuplicateRecordException : Exception
    {
        public DuplicateRecordException()
            : base("A record with this value already exists.") { }

        public DuplicateRecordException(string message)
            : base(message) { }
    }

    /// <summary>A requested record could not be found.</summary>
    public class NotFoundException : Exception
    {
        public NotFoundException()
            : base("The requested record was not found.") { }

        public NotFoundException(string message)
            : base(message) { }
    }
}
