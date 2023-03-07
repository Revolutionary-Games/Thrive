﻿using System;
using Godot;

/// <summary>
///   Singleton managing changing game scenes
/// </summary>
public class SceneManager : Node
{
    private static SceneManager? instance;

    private bool alreadyQuit;

#pragma warning disable CA2213
    private Node internalRootNode = null!;
#pragma warning restore CA2213

    private SceneManager()
    {
        instance = this;
    }

    public static SceneManager Instance => instance ?? throw new InstanceNotLoadedYetException();

    public override void _Ready()
    {
        internalRootNode = GetTree().Root;
    }

    /// <summary>
    ///   Switches to a game state
    /// </summary>
    /// <param name="state">The game state to switch to, this automatically looks up the right scene</param>
    /// <returns>The scene that was switched to</returns>
    public Node SwitchToScene(MainGameState state)
    {
        var scene = LoadScene(state).Instance();
        SwitchToScene(scene);

        return scene;
    }

    public Node SwitchToScene(string scenePath)
    {
        var scene = LoadScene(scenePath).Instance();
        SwitchToScene(scene);

        return scene;
    }

    /// <summary>
    ///   Switched to a new scene
    /// </summary>
    /// <param name="newSceneRoot">The new scene root</param>
    /// <param name="keepOldRoot">If true the old root is preserved (not freed)</param>
    /// <returns>
    ///   When keeping the old root, this will be a reference to the old scene. When not null this must be switched
    ///   back to later or freed manually by the code calling this.
    /// </returns>
    public Node? SwitchToScene(Node newSceneRoot, bool keepOldRoot = false)
    {
        var oldRoot = GetTree().CurrentScene;
        GetTree().CurrentScene = null;

        if (oldRoot != null)
        {
            internalRootNode.RemoveChild(oldRoot);
        }

        internalRootNode.AddChild(newSceneRoot);
        GetTree().CurrentScene = newSceneRoot;
        ModLoader.ModInterface.TriggerOnSceneChanged(newSceneRoot);

        if (!keepOldRoot)
        {
            oldRoot?.QueueFree();
            return null;
        }

        return oldRoot;
    }

    /// <summary>
    ///   Switches a scene to the main menu
    /// </summary>
    public void ReturnToMenu()
    {
        var scene = LoadScene("res://src/general/MainMenu.tscn");

        var mainMenu = (MainMenu)scene.Instance();

        mainMenu.IsReturningToMenu = true;

        SwitchToScene(mainMenu);
    }

    /// <summary>
    ///   Adds the specified scene to the scene tree and then removes it
    /// </summary>
    public void AttachAndDetachScene(Node scene)
    {
        AttachScene(scene);
        DetachScene(scene);
    }

    public void AttachScene(Node scene)
    {
        internalRootNode.AddChild(scene);
    }

    public void DetachScene(Node scene)
    {
        internalRootNode.RemoveChild(scene);
    }

    /// <summary>
    ///   Detaches the current scene without attaching a new one
    /// </summary>
    public void DetachCurrentScene()
    {
        var oldRoot = GetTree().CurrentScene;
        GetTree().CurrentScene = null;

        if (oldRoot != null)
        {
            internalRootNode.RemoveChild(oldRoot);
        }

        oldRoot?.QueueFree();
    }

    public PackedScene LoadScene(MainGameState state)
    {
        switch (state)
        {
            case MainGameState.MicrobeStage:
                return LoadScene("res://src/microbe_stage/MicrobeStage.tscn");
            case MainGameState.MicrobeEditor:
                return LoadScene("res://src/microbe_stage/editor/MicrobeEditor.tscn");
            case MainGameState.EarlyMulticellularEditor:
                return LoadScene("res://src/early_multicellular_stage/editor/EarlyMulticellularEditor.tscn");
            case MainGameState.MulticellularStage:
                return LoadScene("res://src/late_multicellular_stage/MulticellularStage.tscn");
            case MainGameState.LateMulticellularEditor:
                return LoadScene("res://src/late_multicellular_stage/editor/LateMulticellularEditor.tscn");
            default:
                throw new ArgumentException("unknown scene path for given game state");
        }
    }

    public PackedScene LoadScene(string scenePath)
    {
        return GD.Load<PackedScene>(scenePath);
    }

    public PackedScene LoadScene(SceneLoadedClassAttribute? sceneLoaded)
    {
        if (string.IsNullOrEmpty(sceneLoaded?.ScenePath))
        {
            throw new ArgumentException(
                "The specified class to load a scene for didn't have SceneLoadedClassAttribute");
        }

        return LoadScene(sceneLoaded!.ScenePath);
    }

    /// <summary>
    ///   Use this method when closing the game. This is needed to do the necessary actions when quitting.
    /// </summary>
    public void QuitThrive()
    {
        if (!alreadyQuit)
            GD.Print(Constants.USER_REQUESTED_QUIT);

        GetTree().Quit();

        alreadyQuit = true;
    }
}
