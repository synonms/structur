namespace Synonms.Structur.Infrastructure.MongoDb;

public static class MongoDbConstants
{
    public static class Database
    {
        public static class Collections
        {
            public const string ChangeStreamSettings = "change-stream-settings";
            public const string DomainEvents = "domain-events";
            public const string Tenants = "tenants";
            public const string Webhooks = "webhooks";
        }
    }
}