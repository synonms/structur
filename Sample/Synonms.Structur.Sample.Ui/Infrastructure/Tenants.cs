namespace Synonms.Structur.Sample.Ui.Infrastructure;

public static class Tenants
{
    public static class LaLakers
    {
        public static Guid Id => Guid.Parse("10000000-0000-0000-0000-000000000001");
        public static string Name => "Los Angeles Lakers";
    }
    
    public static class TottenhamHotspur
    {
        public static Guid Id => Guid.Parse("10000000-0000-0000-0000-000000000002");
        public static string Name => "Tottenham Hotspur";
    }
}