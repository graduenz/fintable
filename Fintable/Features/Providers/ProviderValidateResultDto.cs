namespace Fintable.Features.Providers;

public class ProviderValidateResultDto
{
    public required IReadOnlyList<string> RequiredKeys { get; set; }
    public bool IsFullySetUp { get; set; }
    public required IReadOnlyDictionary<string, ProviderValidateEntryDto> Providers { get; set; }
}
