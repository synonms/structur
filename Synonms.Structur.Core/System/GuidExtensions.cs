namespace Synonms.Structur.Core.System;

public static class GuidExtensions
{
    /// <summary>
    /// Generate a new <see cref="Guid"/> using the comb algorithm.
    /// COMB Guids embed date and time data in to a random Guid to become sequential when used by SQL Server, improving indexing and sorting. 
    /// </summary>
    /// <param name="originalGuid"></param>
    /// <returns>New <see cref="Guid"/>.</returns>
    public static Guid ToComb(this Guid originalGuid)
    {
        byte[] guidBytes = originalGuid.ToByteArray();

        DateTime now = DateTime.Now;
        TimeSpan period = now - DateTime.UnixEpoch;
            
        byte[] daysBytes = BitConverter.GetBytes(period.Days);
        byte[] msBytes = BitConverter.GetBytes((long) ((double) now.Ticks / 3.333333d / 10000.0d));
            
        // Reverse the bytes to match SQL Servers ordering
        Array.Reverse(daysBytes);
        Array.Reverse(msBytes);
            
        Array.Copy(daysBytes, daysBytes.Length - 2, guidBytes, guidBytes.Length - 6, 2);
        Array.Copy(msBytes, msBytes.Length - 4, guidBytes, guidBytes.Length - 4, 4);

        return new Guid(guidBytes);
    }
}