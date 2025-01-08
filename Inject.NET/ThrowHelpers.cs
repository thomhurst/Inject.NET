namespace Inject.NET;

public class ThrowHelpers
{
    public static T Throw<T>(string message)
    {
        throw new Exception(message);
    }
}