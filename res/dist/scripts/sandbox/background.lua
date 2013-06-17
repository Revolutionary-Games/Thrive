local background = Entity("background")

-- Set up skyplane
local skyplane = SkyPlaneComponent()
skyplane.workingCopy.plane.normal = Vector3(0, 0, 1)
skyplane.workingCopy.plane.d = 1000
skyplane:touch()
background:addComponent(skyplane)

