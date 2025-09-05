namespace ThriveTest.Utils;

public class TestOrganelleUpgrade : AvailableUpgrade
{
    public TestOrganelleUpgrade(string name, int cost, bool defaultUpgrade = false)
    {
        InternalName = name;
        Name = name;
        MPCost = cost;

        if (defaultUpgrade)
            IsDefault = true;
    }
}
