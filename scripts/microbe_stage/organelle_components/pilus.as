// Adds a stabby thing to microbe

class Pilus : OrganelleComponent{
    Pilus(){
    }

    void
    onAddedToMicrobe(
        ObjectID microbeEntity,
        int q, int r, int rotation,
        PlacedOrganelle@ organelle
    ) override {

        auto microbePosition = organelle.world.GetComponent_Position(microbeEntity);

        this.organellePos = Hex::axialToCartesian(q, r);

        Float3 jumpPos = this.organellePos;

        // See the comment in update why the position for this organelle is not calculated here
        constraintCreated = false;

        float pilusSize = 4.6f;
        float mass = organelle.organelle.mass * 2.f;

        // Scale the size down for bacteria
        if(organelle.species.isBacteria){
            pilusSize /= 2.f;
            mass /= 2.f;
            jumpPos /= 2.f;
        }

        jumpPos += microbePosition._Position;

        // Create physics for this
        auto position = organelle.world.Create_Position(organelle.organelleEntity,
            jumpPos, Float4::IdentityQuaternion);
        auto physics = organelle.world.Create_Physics(organelle.organelleEntity, position);

        auto shape = organelle.world.GetPhysicalWorld().CreateCone(pilusSize / 5.f, pilusSize);

        physics.CreatePhysicsBody(organelle.world.GetPhysicalWorld(), shape,
            mass, organelle.world.GetPhysicalMaterial("pilus"));

        physics.JumpTo(position);

        // Detach the node from the parent to let physics handle the model positioning
        auto renderNode = organelle.world.GetComponent_RenderNode(
            organelle.organelleEntity);
        if (renderNode !is null && IsInGraphicalMode())
        {
            renderNode.Node.setParent(organelle.world.GetRootSceneObject(), true);
        }
    }

    void
    update(
        ObjectID microbeEntity,
        PlacedOrganelle@ organelle,
        float elapsed
    ) override {

        // TODO: find a cleaner way than having to call this every tick
        // This is because GetExternalOrganelle only works after the membrane has initialized,
        // which happens on the next tick
        // This doesnt work properly
        Float3 middle = Hex::axialToCartesian(0, 0);
        auto delta = middle - organellePos;
        const Float3 exit = middle - delta;
        auto membraneComponent = organelle.world.GetComponent_MembraneComponent(microbeEntity);
        auto membraneCoords = membraneComponent.GetExternalOrganelle(exit.X, exit.Z);

        if(membraneCoords != lastCalculatedPos || !constraintCreated){

            if(pilusConstraint !is null){
                organelle.world.GetPhysicalWorld().DestroyConstraint(pilusConstraint);
                @pilusConstraint = null;
            }

            float angle = atan2(-delta.Z, delta.X);
            if(angle < 0){
                angle = angle + (2 * PI);
            }

            if(organelle.species.isBacteria){
                membraneCoords /= 2.f;
            }

            angle = ((angle * 180)/PI - 90) % 360;

            const bs::Quaternion rotation = bs::Quaternion(bs::Degree(180),
                bs::Vector3(0, 1, 0)) * bs::Quaternion(bs::Degree(angle),
                    bs::Vector3(0, 1, 0));

            auto microbePhysics = organelle.world.GetComponent_Physics(microbeEntity);
            auto physics = organelle.world.GetComponent_Physics(organelle.organelleEntity);

            @pilusConstraint = organelle.world.GetPhysicalWorld().CreateFixedConstraint(
                microbePhysics.Body, physics.Body, membraneCoords, rotation,
                // Side b transform
                Float3(0, 0, 0), Float4::IdentityQuaternion);
            constraintCreated = true;
        }
    }

    //! Needed to calculate final pos on update
    private Float3 organellePos;

    //! Last calculated position, Used to not have to recreate the constraint all the time
    private Float3 lastCalculatedPos = Float3(0, 0, 0);
    private bool constraintCreated = false;

    private PhysicsConstraint@ pilusConstraint;
}
