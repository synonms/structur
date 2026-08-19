namespace Synonms.Structur.Domain.Validation;

public static class Validator
{
    public static ValidatedInstanceBuilder<TValueObject> CreateBuilder<TValueObject>() => new ValidatedInstanceBuilder<TValueObject>();
}