namespace UnlockConstraints
{
    public interface IUnlockCondition
    {
        bool Satisfied();

        void GenerateTooltip(LocalizedStringBuilder builder);
    }
}
