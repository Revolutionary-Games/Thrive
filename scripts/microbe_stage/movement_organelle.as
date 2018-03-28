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

//! \todo flagellum animation
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
        this.force = Float3(momentum);
        this.torque = torque;
    }
    
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
        float angle = atan2(-delta.Z, delta.X);
        if(angle < 0){
            angle = angle + (2 * PI);
        }
        
        angle = ((angle * 180)/PI + 180) % 360;

        // This is already added by the PlacedOrganlle.onAddedToMicrobe
        Model@ model = organelle.world.GetComponent_Model(organelle.organelleEntity);

        if(model is null)
            assert(false, "MovementOrganelle added to Organelle that has no Model component");

        // The organelles' scenenode is positioned by itself unlike
        // the lua version where that was also attempted here

        // Create animation component
        Animated@ animated = organelle.world.Create_Animated(organelle.organelleEntity,
            model.GraphicalObject);
        SimpleAnimation moveAnimation("Move");
        moveAnimation.Loop = true;
        // 0.25 is the "idle" animation speed when the flagellum isn't used
        moveAnimation.SpeedFactor = 0.25f;
        animated.AddAnimation(moveAnimation);
        // Don't forget to mark to apply the new animation
        animated.Marked = true;

        
        auto@ renderNode = organelle.world.GetComponent_RenderNode(organelle.organelleEntity);
        renderNode.Node.setPosition(organellePos);
        renderNode.Node.setOrientation(Ogre::Quaternion(Ogre::Degree(angle),
                Ogre::Vector3(0, 1, 0)));
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
        int milliseconds, MicrobeComponent@ microbeComponent, Physics@ rigidBodyComponent,
        Position@ pos
    ) {
        // The movementDirection is the player or AI input
        Float3 direction = microbeComponent.movementDirection;

        // For changing animation speed
        Animated@ animated = organelle.world.GetComponent_Animated(organelle.organelleEntity);
    
        auto forceMagnitude = this.force.Dot(direction);
        if(forceMagnitude > 0){
            if(direction.LengthSquared() < EPSILON || this.force.LengthSquared() < EPSILON){
                this.movingTail = false;
                animated.GetAnimation(0).SpeedFactor = 0.25f;
                return;
            }
            
            this.movingTail = true;
            animated.GetAnimation(0).SpeedFactor = 1.3;
        
            auto energy = abs(this.energyMultiplier * forceMagnitude * milliseconds / 1000.f);
            auto availableEnergy = MicrobeOperations::takeCompound(organelle.world,
                microbeEntity,  SimulationParameters::compoundRegistry().getTypeId("atp"),
                energy);
            
            if(availableEnergy < energy){
                forceMagnitude = sign(forceMagnitude) * availableEnergy * 1000.f /
                    milliseconds / this.energyMultiplier;
                this.movingTail = false;
                animated.GetAnimation(0).SpeedFactor = 0.25f;
            }
            float impulseMagnitude = microbeComponent.movementFactor * milliseconds *
                forceMagnitude / 1000.f;

            // Rotate the 'thrust' based on our orientation
            direction = pos._Orientation.RotateVector(direction);

            // This isn't needed
            //direction.Y = 0;
            //direction = direction.Normalize();
            Float3 impulse = direction * impulseMagnitude;
            rigidBodyComponent.GiveImpulse(impulse, pos._Position);
        } else {
            if(this.movingTail){
                this.movingTail = false;
                animated.GetAnimation(0).SpeedFactor = 0.25f;
            }
        }
    }

    void _turnMicrobe(ObjectID microbeEntity, PlacedOrganelle@ organelle, int logicTime,
        MicrobeComponent@ microbeComponent, Physics@ rigidBodyComponent, Position@ pos){
        
        if(this.torque == 0){
            return;
        }

        const auto target = Float4::QuaternionLookAt(pos._Position,
            microbeComponent.facingTargetPoint);
        const auto current = pos._Orientation;
        // Slerp 50% of the way each call
        const auto interpolated = current.Slerp(target, 0.5f);
        // const auto interpolated = target;
        
        // Not sure if updating the Position component here does anything
        pos._Orientation = interpolated;
        pos.Marked = true;

        // LOG_WRITE("turn = " + pos._Orientation.X + ", " + pos._Orientation.Y + ", "
        //     + pos._Orientation.Z + ", " + pos._Orientation.W);
        
        rigidBodyComponent.SetOnlyOrientation(interpolated);
        return;

        // auto targetDirection = microbeComponent.facingTargetPoint - pos._Position;
        // // TODO: direct multiplication was also used here
        // // Float3 localTargetDirection = pos._Orientation.Inverse().RotateVector(targetDirection);
        // Float3 localTargetDirection = pos._Orientation.Inverse().RotateVector(targetDirection);
        
        // // Float3 localTargetDirection = pos._Orientation.ToAxis() - targetDirection;
        // // localTargetDirection.Y = 0; // improper fix. facingTargetPoint somehow gets a non-zero y value.
        // LOG_WRITE("local direction = " + localTargetDirection.X + ", " +
        //     localTargetDirection.Y + ", " + localTargetDirection.Z);
        
        // assert(localTargetDirection.Y < 0.01,
        //     "Microbes should only move in the 2D plane with y = 0");

        // // This doesn't help with the major jitter
        // // // Round to zero if either is too small
        // // if(abs(localTargetDirection.X) < 0.01)
        // //     localTargetDirection.X = 0;
        // // if(abs(localTargetDirection.Z) < 0.01)
        // //     localTargetDirection.Z = 0;
        
        // float alpha = atan2(-localTargetDirection.X, -localTargetDirection.Z);
        // float absAlpha = abs(alpha) * RADIANS_TO_DEGREES;
        // microbeComponent.microbetargetdirection = absAlpha;
        // if(absAlpha > 1){

        //     LOG_WRITE("Alpha is: " + alpha);
        //     Float3 torqueForces = Float3(0, this.torque * alpha * logicTime *
        //         microbeComponent.movementFactor * 0.00001f, 0);
        //     rigidBodyComponent.AddOmega(torqueForces);

        //     // Rotation is the same for each flagella so doing this
        //     // makes things less likely to break and still work. Only
        //     // tweak should be that there should be
        //     // microbeComponent.movementFactor alternative for
        //     // rotation that depends on flagella and cilia. The
        //     // problem with this is that there are weird spots where
        //     // this gets stuck at (hopefully works better with the
        //     // rounding of X and Z)
        //     // Float3 torqueForces = Float3(0, this.torque * alpha * logicTime *
        //     //     microbeComponent.movementFactor * 0.0001f, 0);
        //     // rigidBodyComponent.SetOmega(torqueForces);
            
        // } else {
        //     // Doesn't work
        //     // // Slow down rotation if there is some
        //     // auto omega = rigidBodyComponent.GetOmega();
        //     // rigidBodyComponent.SetOmega(Float3(0, 0, 0));

        //     // if(abs(omega.X) > 1 || abs(omega.Z) > 1){

        //     //     rigidBodyComponent.AddOmega(Float3(-omega.X * 0.01f, 0, -omega.Z * 0.01f));
        //     // }
        // }
    }

    void
    update(
        ObjectID microbeEntity,
        PlacedOrganelle@ organelle,
        int logicTime
    ) override {

        // TODO: what does this code attempt to do. Don't we already set the organelle's
        // scenenode to the right position already (in PlacedOrganelle.onAddedToMicrobe)?
        // The point of this is to make it auto-update when the membrane changes shape probably.
        // auto membraneComponent = getComponent(microbeEntity, MembraneComponent);
        // local x, y = axialToCartesian(organelle.position.q, organelle.position.r);
        // auto membraneCoords = membraneComponent.getExternOrganellePos(x, y);
        // auto translation = Vector3(membraneCoords[1], membraneCoords[2], 0);
        // this.sceneNode.transform.position = translation;
        // this.sceneNode.transform.touch();

        MicrobeComponent@ microbeComponent = cast<MicrobeComponent>(
            organelle.world.GetScriptComponentHolder("MicrobeComponent").Find(microbeEntity));
        auto rigidBodyComponent = organelle.world.GetComponent_Physics(microbeEntity);
        auto pos = organelle.world.GetComponent_Position(microbeEntity);

        if(rigidBodyComponent.Body is null){

            LOG_WARNING("Skipping movement organelle update for microbe without physics body");
            return;
        }
        
        _turnMicrobe(microbeEntity, organelle, logicTime, microbeComponent,
            rigidBodyComponent, pos);
        _moveMicrobe(microbeEntity, organelle, logicTime, microbeComponent,
            rigidBodyComponent, pos);
    }

    private Float3 force;
    private float torque;
    float energyMultiplier = 0.025;
    // float backwards_multiplier = 0;
    private bool movingTail = false;    
}
