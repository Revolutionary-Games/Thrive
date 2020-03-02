using System;
using System.Collections.Generic;

public class NameGenerator : IRegistryType
{
    public List<string> PrefixCofix;
    public List<string> PrefixesV;
    public List<string> PrefixesC;
    public List<string> CofixesV;
    public List<string> CofixesC;
    public List<string> SuffixesV;
    public List<string> SuffixesC;

    public void Check(string name)
    {
        if (PrefixCofix.Count < 1)
        {
            throw new InvalidRegistryData("NameGenerator", this.GetType().Name,
                "PrefixCofix is empty");
        }

        if (PrefixesV.Count < 1)
        {
            throw new InvalidRegistryData("NameGenerator", this.GetType().Name,
                "PrefixesV is empty");
        }

        if (PrefixesC.Count < 1)
        {
            throw new InvalidRegistryData("NameGenerator", this.GetType().Name,
                "PrefixesC is empty");
        }

        if (CofixesV.Count < 1)
        {
            throw new InvalidRegistryData("NameGenerator", this.GetType().Name,
                "CofixesV is empty");
        }

        if (CofixesC.Count < 1)
        {
            throw new InvalidRegistryData("NameGenerator", this.GetType().Name,
                "CofixesC is empty");
        }

        if (SuffixesV.Count < 1)
        {
            throw new InvalidRegistryData("NameGenerator", this.GetType().Name,
                "SuffixesV is empty");
        }

        if (SuffixesC.Count < 1)
        {
            throw new InvalidRegistryData("NameGenerator", this.GetType().Name,
                "SuffixesC is empty");
        }
    }
}
