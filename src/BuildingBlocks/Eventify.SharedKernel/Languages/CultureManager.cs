namespace Eventify.SharedKernel.Languages;

public static class CultureManager
{
    public static readonly Dictionary<Languages, string> CultureNames = new Dictionary<Languages, string>
    {
        { Languages.En, "en-US" }, { Languages.Uk, "uk-UA" }
    };
}
