//! Adds a stabby thing to microbe
class Pilus : OrganelleComponent{
    Pilus(){
    }

    void
    onAddedToMicrobe(
        ObjectID microbeEntity,
        int q, int r, int rotation,
        PlacedOrganelle@ organelle
    ) override {

        this.organellePos = Hex::axialToCartesian(q, r);

        createShape(organelle);

        // TODO: the shape can't be attached here with
        // organelle.addChildCollision because the final position can
        // only be calculated in update
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
        const Float3 middle = Hex::axialToCartesian(0, 0);
        const auto delta = middle - organellePos;
        const Float3 exit = middle - delta;
        auto membraneComponent = organelle.world.GetComponent_MembraneComponent(microbeEntity);
        auto membraneCoords = membraneComponent.GetExternalOrganelle(exit.X, exit.Z);

        if(membraneCoords != lastCalculatedPos || !addedChildShape){

            float angle = atan2(-delta.Z, delta.X);
            if(angle < 0){
                angle = angle + (2 * PI);
            }

            angle = ((angle * 180)/PI - 90) % 360;

            Quaternion rotation = createRotationForExternal(angle);

            auto renderNode = organelle.world.GetComponent_RenderNode(
                organelle.organelleEntity);
            if(renderNode !is null && IsInGraphicalMode())
            {
                renderNode.Node.SetPosition(membraneCoords);
                renderNode.Node.SetOrientation(rotation);
            }

            const Float3 membranePointDirection = (membraneCoords - middle).Normalize();

            membraneCoords += membranePointDirection * HEX_SIZE * 2;

            if(organelle.species.isBacteria){
                membraneCoords /= 2.f;
            }

            Quaternion physicsRotation = Quaternion(Float3(-1, 0, 0), Degree(90)) *
                Quaternion(Float3(0, 0, -1), Degree(180 - angle));

            PhysicsShape@ collisionShape =
                MicrobeOperations::getMicrobeCollisionShapeForEditing(organelle.world,
                    microbeEntity);

            if(addedChildShape){
                // Need to remove the old copy first
                collisionShape.RemoveChildShape(pilusShape);
            }

            collisionShape.AddChildShape(pilusShape, membraneCoords, physicsRotation);
            MicrobeOperations::finishMicrobeCollisionShapeEditing(organelle.world,
                microbeEntity);
            addedChildShape = true;
        }
    }

    void
    onRemovedFromMicrobe(
        ObjectID microbeEntity,
        PlacedOrganelle@ organelle
    ) override {
        if(addedChildShape){
            // The collision shape is already being edited here (as
            // hex collisions are always removed) so we don't have to
            // call finish collision shape editing
            PhysicsShape@ collisionShape =
                MicrobeOperations::getMicrobeCollisionShapeForEditing(organelle.world,
                    microbeEntity);
            // Not a serious problem as the cell may be killed already
            if(collisionShape is null)
                return;

            collisionShape.RemoveChildShape(pilusShape);
            addedChildShape = false;
        }
    }

    void
    createShape(PlacedOrganelle@ organelle)
    {
        float pilusSize = 4.6f;

        // Scale the size down for bacteria
        if(organelle.species.isBacteria){
            pilusSize /= 2.f;
        }

        @pilusShape = organelle.world.GetPhysicalWorld().CreateCone(pilusSize / 10.f,
            pilusSize);

        pilusShape.SetCustomTag(PHYSICS_PILUS_TAG);
    }

    //! Needed to calculate final pos on update
    private Float3 organellePos;

    //! Last calculated position, Used to not have to recreate the constraint all the time
    private Float3 lastCalculatedPos = Float3(0, 0, 0);


    private PhysicsShape@ pilusShape;
    private bool addedChildShape = false;
}
