namespace api.Services;

public class UsernameConflictException : Exception
{
    public UsernameConflictException(string username)
        : base($"The username \"{username}\" is already taken.")
    {
        Username = username;
    }

    public string Username { get; }
}

public class ProfileValidationException : Exception
{
    public ProfileValidationException(string message) : base(message)
    {
    }
}

public class SelfFollowException : Exception
{
    public SelfFollowException() : base("You cannot follow yourself.")
    {
    }
}
