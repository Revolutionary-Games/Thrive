#include "organelle_component.as"
#include "microbe_operations.as"

// Enables a microbe to move and turn

// See organelle_component.as for more information about the 
// organelle component methods and the arguments they receive.

// Calculate the momentum of the movement organelle based on angle towards nucleus
Float3 calculateForce(int q, int r, float momentum){
    Float3 organelle = Hex::axialToCartesian(q, r);
    Float3 nucleus = Hex::axialToCartesian(0, 0);
    auto delta = nucleus - organelle;
    return delta.Normalize() * momentum;
}

class MovementOrganelle : OrganelleComponent{

    // Constructor
    //
    // @param momentum
    //  The force this organelle can exert to move a microbe.
    //
    // @param torque
    //  The torque this organelle can exert to turn a microbe.
    MovementOrganelle(float momentum, float torque){

        // Store this here temporarily
        force = Float3(momentum);
        torque = torque;
    }

    Float3 force;
    float torque;
    float energyMultiplier = 0.025;
    // float backwards_multiplier = 0;
    private bool movingTail = false;
    
    void
    onAddedToMicrobe(
        ObjectID microbeEntity,
        int q, int r, int rotation,
        PlacedOrganelle@ organelle
    ) override {

        this.force = calculateForce(q, r, this.force.X);
        
        Float3 organellePos = Hex::axialToCartesian(q, r);
        Float3 nucleus = Hex::axialToCartesian(0, 0);
        auto delta = nucleus - organellePos;
        float angle = atan2(delta.Z, delta.X);
        if(angle < 0){
            angle = angle + (2 * PI);
        }
        
        angle = ((angle * 180)/PI + 180) % 360;

        auto sceneNode = organelle.world.GetComponent_RenderNode(organelle.organelleEntity);
        //Adding a mesh to the organelle.
        @this.model = organelle.world.Create_Model(organelle.organelleEntity,
            sceneNode.Node, organelle.organelle.mesh);

        // The organelles' scenenode is positioned by itself unlike
        // the lua version where that was also attempted here

        LOG_INFO("TODO: flagellum animation");
        // this.sceneNode.playAnimation("Move", true);
        // 0.25 is the "idle" animation speed when the flagellum isn't used
        // this.sceneNode.setAnimationSpeed(0.25);
    }

    // void MovementOrganelle.load(storage){
    // this.position = {}
    // this.energyMultiplier = storage.get("energyMultiplier", 0.025)
    // this.force = storage.get("force", Vector3(0,0,0))
    // this.torque = storage.get("torque", 500)
    // }

    // void MovementOrganelle.storage(){
    // auto storage = StorageContainer()
    // storage.set("energyMultiplier", this.energyMultiplier)
    // storage.set("force", this.force)
    // storage.set("torque", this.torque)
    // return storage
    // }

    private void _moveMicrobe(ObjectID microbeEntity, PlacedOrganelle@ organelle,
        int milliseconds
    ) {
        MicrobeComponent@ microbeComponent = cast<MicrobeComponent>(
            organelle.world.GetScriptComponentHolder("MicrobeComponent").Find(microbeEntity));

        auto rigidBodyComponent = organelle.world.GetComponent_Physics(microbeEntity);
        auto pos = organelle.world.GetComponent_Position(microbeEntity);

        Float3 direction = microbeComponent.movementDirection;
    
        auto forceMagnitude = this.force.Dot(direction);
        if(forceMagnitude > 0){
            if(direction.LengthSquared() < EPSILON || this.force.LengthSquared() < EPSILON){
                this.movingTail = false;
                // this.sceneNode.setAnimationSpeed(0.25);
                return;
            }
            
            this.movingTail = true;
            // this.sceneNode.setAnimationSpeed(1.3);
        
            auto energy = abs(this.energyMultiplier * forceMagnitude * milliseconds / 1000);
            auto availableEnergy = MicrobeOperations::takeCompound(organelle.world,
                microbeEntity,  SimulationParameters::compoundRegistry().getTypeId("atp"),
                energy);
            
            if(availableEnergy < energy){
                forceMagnitude = sign(forceMagnitude) * availableEnergy * 1000 / milliseconds /
                    this.energyMultiplier;
                this.movingTail = false;
                // this.sceneNode.setAnimationSpeed(0.25);
            }
            float impulseMagnitude = microbeComponent.movementFactor * milliseconds *
                forceMagnitude / 1000;

            Float3 impulse = direction * impulseMagnitude;
            // TODO: this was just multiplication here before so check
            // if it meant Dot, Cross or element wise multiplication
            Float3 a = pos._Orientation.ToAxis().Dot(impulse);
            rigidBodyComponent.GiveImpulse(a);
        } else {
            if(this.movingTail){
                this.movingTail = false;
                // this.sceneNode.setAnimationSpeed(0.25);
            }
        }
    }

    // TODO: Add logictime considerations. This is now restricted to
    // TICKSPEED so it should be fine
    void _turnMicrobe(ObjectID microbeEntity, PlacedOrganelle@ organelle){

        if(this.torque == 0){
            return;
        }
        
        MicrobeComponent@ microbeComponent = cast<MicrobeComponent>(
            organelle.world.GetScriptComponentHolder("MicrobeComponent").Find(microbeEntity));
        auto rigidBodyComponent = organelle.world.GetComponent_Physics(microbeEntity);
        auto pos = organelle.world.GetComponent_Position(microbeEntity);
        
        auto targetDirection = microbeComponent.facingTargetPoint - pos._Position;
        // TODO: direct multiplication was also used here
        Float3 localTargetDirection = pos._Orientation.Inverse().RotateVector(targetDirection);
        // localTargetDirection.Y = 0; // improper fix. facingTargetPoint somehow gets a non-zero y value.
        assert(localTargetDirection.Y < 0.01,
            "Microbes should only move in the 2D plane with y = 0");
        
        auto alpha = abs(atan2(-localTargetDirection.X, localTargetDirection.Z) *
            RADIANS_TO_DEGREES);
        microbeComponent.microbetargetdirection = alpha;
        if(alpha > 1){
            
            rigidBodyComponent.SetTorque(Float3(0,
                    this.torque * alpha * microbeComponent.movementFactor, 0));
        }
    }

    void
    update(
        ObjectID microbeEntity,
        PlacedOrganelle@ organelle,
        int logicTime
    ) override {

        // TODO: what does this code attempt to do. Don't we already set the organelle's
        // scenenode to the right position already (in PlacedOrganelle.onAddedToMicrobe)?
        // auto membraneComponent = getComponent(microbeEntity, MembraneComponent);
        // local x, y = axialToCartesian(organelle.position.q, organelle.position.r);
        // auto membraneCoords = membraneComponent.getExternOrganellePos(x, y);
        // auto translation = Vector3(membraneCoords[1], membraneCoords[2], 0);
        // this.sceneNode.transform.position = translation;
        // this.sceneNode.transform.touch();

        // TODO: these both access the same components and thus should
        // be stored to save looking them up twice
        _turnMicrobe(microbeEntity, organelle);
        _moveMicrobe(microbeEntity, organelle, logicTime);
    }

    private Model@ model;
}
