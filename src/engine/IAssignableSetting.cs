/// <summary>
///   Interface used for the SettingValue class to allow casting of an object to the correct concrete class.
/// </summary>
public interface IAssignableSetting
{
    public void AssignFrom(object obj);
}
