using FluentValidation;
using PokeSync.Api.Contracts.Upsert;

namespace PokeSync.Api.Contracts.Upsert.Validation;

public sealed class PokemonUpsertItemDtoValidator : AbstractValidator<PokemonUpsertItemDto>
{
    private static readonly IReadOnlySet<string> AllowedStatNames =
     new HashSet<string> { "hp", "attack", "defense", "special-attack", "special-defense", "speed" };

    public PokemonUpsertItemDtoValidator()
    {
        RuleFor(x => x.ExternalId)
            .GreaterThan(0).WithMessage("ExternalId must be positive.");

        RuleFor(x => x.Number)
            .GreaterThan(0).WithMessage("Number must be positive.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(100).WithMessage("Name too long (100 max).");

        RuleFor(x => x.GenerationNumber)
            .GreaterThan(0).WithMessage("GenerationNumber must be positive.");

        RuleFor(x => x.Types)
            .NotNull().WithMessage("Types are required.")
            .Must(t => t.Count > 0).WithMessage("At least one type is required.");

        RuleForEach(x => x.Types)
            .NotEmpty().WithMessage("Type name is required.")
            .Matches("^[a-z0-9\\-]+$").WithMessage("Type must be lowercase kebab-case.");

        When(x => x.Stats is not null, () =>
        {
            RuleForEach(x => x.Stats!).ChildRules(stat =>
            {
                stat.RuleFor(s => s.Name)
                   .Must(name => AllowedStatNames.Contains(name))
                   .WithMessage($"Stat name must be one of: {string.Join(", ", AllowedStatNames)}");

                stat.RuleFor(s => s.Value)
                   .InclusiveBetween(1, 255).WithMessage("Stat value must be between 1 and 255.");
            });
        });

        When(x => x.Flavors is not null, () =>
        {
            RuleForEach(x => x.Flavors!).ChildRules(flavor =>
            {
                flavor.RuleFor(f => f.GenerationNumber)
                    .GreaterThan(0).WithMessage("Flavor.GenerationNumber must be positive.");

                flavor.RuleFor(f => f.Language)
                    .NotEmpty().WithMessage("Flavor.Language is required.")
                    .Length(2, 5).WithMessage("Language code invalid.");

                flavor.RuleFor(f => f.Text)
                    .NotEmpty().WithMessage("Flavor text is required.")
                    .MaximumLength(300).WithMessage("Flavor text too long (300 max).");
            });
        });
    }
}
