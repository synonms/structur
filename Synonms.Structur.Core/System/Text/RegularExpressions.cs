namespace Synonms.Structur.Core.System.Text;

public static class RegularExpressions
{
    public const string AddressLine = "^[\\da-zA-Z-'(),. ]+$";
    public const string EmailAddress = "^(?!\\.)(?!.*\\.\\.)([a-zA-Z0-9_'+\\-\\.]*)[a-zA-Z0-9_+-]@([a-zA-Z0-9][a-zA-Z0-9\\-]*\\.)+[a-zA-Z]{2,}$";
    public const string NationalInsuranceNumber = "^(?!BG)(?!GB)(?!NK)(?!KN)(?!TN)(?!NT)(?!ZZ)[A-Z&&[^DFIQUV][A-Z&&[^DFIOQUV][0-9]{6}[A-D]$";
    public const string Postcode = "^([Gg][Ii][Rr] 0[Aa]{2})|((([A-Za-z][0-9]{1,2})|(([A-Za-z][A-Ha-hJ-Yj-y][0-9]{1,2})|(([A-Za-z][0-9][A-Za-z])|([A-Za-z][A-Ha-hJ-Yj-y][0-9]?[A-Za-z])))) ?[0-9][A-Za-z]{2})$";
    public const string TelephoneNumber = "^(\\+44|0)\\d{10}$";
}