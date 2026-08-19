using System.Reflection;

namespace Synonms.Structur.Sample.AppHost;

public static class SampleAppHostProject
{
    public static Assembly Assembly => typeof(SampleAppHostProject).Assembly;
}