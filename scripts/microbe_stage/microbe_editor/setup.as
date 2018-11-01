#include "microbe_editor_hud.as"

// Called from ThriveGame when the editor has been entered and it should be setup
void onEditorEntry(MicrobeEditorWorld@ world){

    LOG_INFO("Running microbe editor script setup");

    // This doesn't overwrite the object when called again so
    // setupHUDAfterEditorEntry must succeed when called again on
    // future edit sessions
    world.RegisterScriptSystem("MicrobeEditorHudSystem", MicrobeEditorHudSystem());

    // The world is cleared by the C++ code so we setup all of our entities again each time
    setupBackground(world);

    // The world already has a created camera in C++ so if it needs to
    // be moved change the position there

    cast<MicrobeEditorHudSystem>(world.GetScriptSystem("MicrobeEditorHudSystem")).
        setupHUDAfterEditorEntry();
}

void setupBackground(MicrobeEditorWorld@ world){

    LOG_ERROR("TODO: editor setupBackground");

    // auto entity = Entity("background", MicrobeEditorWorld@)
    // auto skyplane = SkyPlaneComponent();
    // skyplane.properties.plane.normal = Vector3(0, 0, 2000);
    // skyplane.properties.materialName = "background/blue_01";
    // skyplane.properties.scale = 4;
    // skyplane.properties.touch();
    // entity.addComponent(skyplane);
    // //Create floating arrow entity
    // entity = Entity("directionarrow", MicrobeEditorWorld@);
    // auto sceneNode = OgreSceneNodeComponent();
    // sceneNode.meshName = "arrow.mesh";
    // sceneNode.transform.position = Vector3(0,7,-4);
    // sceneNode.transform.orientation = Quaternion(Radian(Degree(90)), Vector3(1, 1, 1));
    // sceneNode.transform.scale = Vector3(0.5,0.5,0.5);
    // sceneNode.transform.touch();
    // sceneNode.playAnimation("Stand", true);
    // entity.addComponent(sceneNode);
}

