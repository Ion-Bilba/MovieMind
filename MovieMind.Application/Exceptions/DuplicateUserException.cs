namespace MovieMind.Application.Exceptions;

public sealed class DuplicateUserException : Exception
{
    public DuplicateUserException()
        : base("Există deja un utilizator cu acest nume sau email.")
    {
    }
}