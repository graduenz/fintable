using FluentValidation;

namespace Fintable.Features.Providers
{
    public class ProviderDtoValidator : AbstractValidator<ProviderDto>
    {
        private static readonly string[] AllowedTypes = [ProviderType.Organizze];

        public ProviderDtoValidator()
        {
            RuleFor(x => x.Name).NotEmpty();
            RuleFor(x => x.Type)
                .NotEmpty()
                .Must(t => AllowedTypes.Contains(t?.Trim().ToLowerInvariant()))
                .WithMessage($"Type must be one of: {string.Join(", ", AllowedTypes)}");
        }
    }
}
