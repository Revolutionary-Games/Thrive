/// <summary>
///   Interpreted response from Steam
/// </summary>
public class WorkshopResult
{
    public WorkshopResult(bool success, string translatedError, bool termsOfServiceSigningRequired, ulong itemId)
    {
        Success = success;
        TranslatedError = translatedError;
        TermsOfServiceSigningRequired = termsOfServiceSigningRequired;
        ItemId = itemId;
    }

    public bool Success { get; set; }
    public string TranslatedError { get; set; }
    public bool TermsOfServiceSigningRequired { get; set; }
    public ulong ItemId { get; set; }
}
