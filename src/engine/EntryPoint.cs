using Godot;

public class EntryPoint : Node
{
    public override void _Ready()
    {
        Invoke.Instance.Queue(SelectSceneToSwitch);
    }

    private void SelectSceneToSwitch()
    {
        if (OS.HasFeature("server"))
        {
            GD.Print("Running the game as a dedicated server");

            // TODO: Read from server configuration file
            NetworkManager.Instance.CreateServer(new Vars());
            NetworkManager.Instance.Join();
        }
        else
        {
            var scene = SceneManager.Instance.LoadScene("res://src/general/MainMenu.tscn");
            var mainMenu = (MainMenu)scene.Instance();
            SceneManager.Instance.SwitchToScene(mainMenu);
        }
    }
}
