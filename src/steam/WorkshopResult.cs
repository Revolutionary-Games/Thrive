/// <summary>
///   Interpreted response from Steam
/// </summary>
public class WorkshopResult
{
    public bool Success { get; set; }
    public string TranslatedError { get; set; }
    public bool TermsOfServiceSigningRequired { get; set; }
    public ulong ItemId { get; set; }
}
