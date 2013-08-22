local background = Entity("background")

-- Set up skyplane
local skyplane = SkyPlaneComponent()
skyplane.properties.plane.normal = Vector3(0, 0, 1)
skyplane.properties.plane.d = 1000
skyplane.properties:touch()
background:addComponent(skyplane)

