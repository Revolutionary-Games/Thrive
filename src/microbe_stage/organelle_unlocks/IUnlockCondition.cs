namespace UnlockConstraints
{
    public interface IUnlockCondition
    {
        public bool Satisfied();

        public void GenerateTooltip(LocalizedStringBuilder builder);

        public void Check(string name);
    }
}
