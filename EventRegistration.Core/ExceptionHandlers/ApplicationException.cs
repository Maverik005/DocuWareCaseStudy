
namespace EventRegistration.Core.ExceptionHandlers;
public abstract class ApplicationException: Exception
{
    protected ApplicationException(string message) : base(message) { }
}


public sealed class DuplicateRegistrationException: ApplicationException
{
    public string Email { get; }
    public int EventId { get; }
    public DuplicateRegistrationException(string email, int eventId)
        : base($"Email {email} is already registered for event {eventId}")
    {
        Email = email;
        EventId = eventId;
    }

}