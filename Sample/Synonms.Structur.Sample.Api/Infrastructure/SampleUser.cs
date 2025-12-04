using Synonms.Structur.Application.Users;

namespace Synonms.Structur.Sample.Api.Infrastructure;

public class SampleUser : StructurUser
{
    public required string Name { get; set; }
}