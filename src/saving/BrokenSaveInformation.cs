public class BrokenSaveInformation : SaveInformation
{
    public override string ThriveVersion { get; set; } = "INVALID";
    public override string Platform { get; set; } = "INVALID";
    public override string Creator { get; set; } = "INVALID";
    public override SaveType Type { get; set; } = SaveType.Broken;
}
