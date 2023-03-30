public interface IReturnableGameState : ILoadableGameState, ICurrentGameInfo
{
    public void OnReturnFromEditor();
}
