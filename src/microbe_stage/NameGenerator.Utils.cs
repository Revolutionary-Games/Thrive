using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Text;

/// <summary>
///   The microbe species name generator.
/// </summary>
public partial class NameGenerator
{
    private static readonly HashSet<char> Vocals = ['a', 'e', 'i', 'o', 'u'];
    private static readonly HashSet<char> Bilabials = ['p', 'b', 'm'];

    private static readonly FrozenDictionary<(char, char), char> VocalicTransitions = new Dictionary<(char, char), char>
    {
        { ('o', 'a'), 'o' },
        { ('e', 'a'), 'e' },
        { ('e', 'u'), 'u' },
    }.ToFrozenDictionary();

    private static StringBuilder? stringBuffer;

    public enum GrammaticalGender
    {
        Masculine,
        Feminine,
        Neuter,
        Ambiguous,
    }

    private static StringBuilder GetBuffer()
    {
        stringBuffer ??= new StringBuilder(64);

        stringBuffer.Length = 0;
        return stringBuffer;
    }

    private static T GetRandomElement<T>(Random random, T[] array)
    {
        return array[random.Next(array.Length)];
    }

    private static void PhonotacticsFriendlyAppend(StringBuilder stringBuilder, string data)
    {
        if (stringBuilder.Length == 0)
        {
            stringBuilder.Append(data);
            return;
        }

        var next = data[0];
        var previous = stringBuilder[^1];

        if (Vocals.Contains(next) && Vocals.Contains(previous))
        {
            // Both are vocals
            if (next == previous)
            {
                --stringBuilder.Length;
            }
            else if (VocalicTransitions.TryGetValue((previous, next), out var vocal))
            {
                --stringBuilder.Length;
                stringBuilder.Append(vocal);
                stringBuilder.Append(data, 1, data.Length - 1);

                return;
            }
        }
        else if (!Vocals.Contains(next) && !Vocals.Contains(previous))
        {
            switch (previous)
            {
                // Both are consonants
                case 'n' when Bilabials.Contains(next):
                    // n before bilabial becomes m
                    --stringBuilder.Length;
                    stringBuilder.Append('m');
                    break;
                case 'n' when next is 'r' or 'l':
                    // n before liquid gets assimilated
                    --stringBuilder.Length;
                    stringBuilder.Append(next);
                    break;
            }
        }

        stringBuilder.Append(data);
    }

    /*
    private string GetColorByGender(ColorAdjective color, GrammaticalGender gender)
    {
        return gender switch
        {
            GrammaticalGender.Masculine => color.Masculine,
            GrammaticalGender.Feminine => color.Feminine,
            GrammaticalGender.Neuter => color.Neuter,
            _ => color.Masculine,
        };
    }
    */

    private T GetRandomElement<T>(Random random, IList<T> list)
    {
        return list[random.Next(list.Count)];
    }

    public record NamingState(bool GenusIsNumbered = false, bool GenusIsProto = false, string GenusRoot = "",
        GrammaticalGender Gender = GrammaticalGender.Neuter, OrganelleDefinition? NumberedOrganelle = null)
    {
        public bool GenusIsNumbered = GenusIsNumbered;
        public bool GenusIsProto = GenusIsProto;
        public string GenusRoot = GenusRoot;
        public GrammaticalGender Gender = Gender;
        public OrganelleDefinition? NumberedOrganelle = NumberedOrganelle;
    }
}
