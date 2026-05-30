namespace Eventify.SharedKernel.Options;

public interface IOption
{
    static abstract string SectionName { get; }

    static string GetSectionName<TOption>() where TOption : IOption
    {
        var name = typeof(TOption).Name;

        return name.EndsWith(Constants.Options)
            ? name[..^Constants.Options.Length]
            : name;
    }
}
