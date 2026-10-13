using System.Globalization;
using Synonms.Structur.Domain.ValueObjects;
using Synonms.Structur.Domain.ValueObjects.Abstractions;

namespace Synonms.Structur.Domain.Tests.Unit.ValueObjects;

public class ScalarValueObjectFormattingTests
{
    [Fact]
    public void ToString_UsesInvariantCultureAndDecimalDisplayPrecision()
    {
        CultureInfo originalCulture = CultureInfo.CurrentCulture;

        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
            DecimalValueObject valueObject = new(1234.567m);

            Assert.Equal("1234.57", valueObject.ToString());
            Assert.Equal(1234.567m, valueObject.Value);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    [Fact]
    public void ToString_WithFormatAndProvider_UsesRequestedFormatting()
    {
        DecimalValueObject valueObject = new(1234.567m);

        string formatted = valueObject.ToString("F3", CultureInfo.GetCultureInfo("fr-FR"));

        Assert.Equal("1234,567", formatted);
    }

    [Fact]
    public void ToString_UsesDatePoliciesWithoutChangingDateTimeKind()
    {
        BirthDate birthDate = BirthDate.Rehydrate(new DateOnly(2024, 3, 9));
        DateTime dateTime = DateTime.SpecifyKind(new DateTime(2024, 3, 9, 10, 11, 12), DateTimeKind.Unspecified);
        EventDateTime eventDateTime = EventDateTime.Rehydrate(dateTime);
        DateTimeOffset dateTimeOffset = new(2024, 3, 9, 10, 11, 12, TimeSpan.FromHours(2));
        DateTimeOffsetValueObject offsetValueObject = new(dateTimeOffset);

        Assert.Equal("2024-03-09", birthDate.ToString());
        Assert.Equal(dateTime.ToString("O", CultureInfo.InvariantCulture), eventDateTime.ToString());
        Assert.Equal(DateTimeKind.Unspecified, eventDateTime.Value.Kind);
        Assert.DoesNotContain("Z", eventDateTime.ToString());
        Assert.Equal(dateTimeOffset.ToString("O", CultureInfo.InvariantCulture), offsetValueObject.ToString());
    }

    [Fact]
    public void ToString_RetainsGeneralFormattingForIntegralAndStringValues()
    {
        Units units = Units.Rehydrate(12);
        Moniker moniker = Moniker.Rehydrate("Ada");

        Assert.Equal("12", units.ToString());
        Assert.Equal("Ada", moniker.ToString());
    }

    [Fact]
    public void DefaultFormat_CanBeOverriddenByDomainSpecificScalar()
    {
        ThreeDecimalPlacesValueObject valueObject = new(1.23456m);

        Assert.Equal("1.2346", valueObject.ToString());
    }

    private class DecimalValueObject(decimal value) : ScalarValueObject<DecimalValueObject, decimal>
    {
        public override decimal Value { get; } = value;
    }

    private sealed class ThreeDecimalPlacesValueObject(decimal value) : ScalarValueObject<ThreeDecimalPlacesValueObject, decimal>
    {
        public override decimal Value { get; } = value;

        protected override string? DefaultFormat => "F4";
    }

    private sealed class DateTimeOffsetValueObject(DateTimeOffset value) : ScalarValueObject<DateTimeOffsetValueObject, DateTimeOffset>
    {
        public override DateTimeOffset Value { get; } = value;
    }
}
