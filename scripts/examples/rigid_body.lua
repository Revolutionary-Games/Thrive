local entity = Entity("object")
local rigidBody = RigidBodyComponent()

-- Set collision shape
rigidBody.properties.shape = CylinderShape(
    CollisionShape.AXIS_X, 
    0.4,
    2.0
)

-- Set initial transform and velocities
rigidBody:setDynamicProperties(
    Vector3(10, 0, 0), -- Position
    Quaternion(Radian(Degree(0)), Vector3(1, 0, 0)), -- Orientation
    Vector3(0, 0, 0), -- Linear velocity
    Vector3(0, 0, 0)  -- Angular velocity
)

-- Mark properties as changed
rigidBody.properties:touch()

-- Add to entity
entity:addComponent(rigidBody)
