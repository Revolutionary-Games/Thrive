--------------------------------------------------------------------------------
-- Class for the single core organelle of any microbe
--------------------------------------------------------------------------------
class 'NucleusOrganelle' (ProcessOrganelle)

-- Constructor
function NucleusOrganelle:__init()
    ProcessOrganelle.__init(self)
    self.golgi = Entity()
	self.ER = Entity()
end


-- Overridded from Organelle:onAddedToMicrobe
function NucleusOrganelle:onAddedToMicrobe(microbe, q, r, rotation)
    local x, y = axialToCartesian(q-1, r-1)
    local sceneNode1 = OgreSceneNodeComponent()
    sceneNode1.meshName = "golgi.mesh"
    sceneNode1.parent = microbe:getOrganelleAt(q,r).entity
	sceneNode1.transform.position = Vector3(x,y,0)
    sceneNode1.transform.scale = Vector3(1, 1, 1)
    sceneNode1.transform.orientation = Quaternion(Radian(Degree(rotation)), Vector3(0, 0, 1))
    sceneNode1.transform:touch()
    microbe.entity:addChild(self.golgi)
    self.golgi:addComponent(sceneNode1)
	self.golgi.sceneNode = sceneNode1
	self.golgi:setVolatile(true)
	
	local sceneNode2 = OgreSceneNodeComponent()
    sceneNode2.meshName = "ER.mesh"
	sceneNode2.parent = microbe:getOrganelleAt(q,r).entity
	--local x1, y1 = axialToCartesian(q, r-2)
    --local x2, y2 = axialToCartesian(q, r-1)
	--local x3, y3 = axialToCartesian(q+1, r-2)
	--x = (x1+x2+x2+x3)/4
	--y = (y1+y2+y2+y3)/4
	sceneNode2.transform.position = Vector3(0,0,0)
    sceneNode2.transform.scale = Vector3(1, 1, 1)
    sceneNode2.transform.orientation = Quaternion(Radian(Degree(rotation+5)), Vector3(0, 0, 1))
    sceneNode2.transform:touch()
    microbe.entity:addChild(self.ER)
    self.ER:addComponent(sceneNode2)
	self.ER.sceneNode = sceneNode2
	self.ER:setVolatile(true)
	
    ProcessOrganelle.onAddedToMicrobe(self, microbe, q, r, rotation)
end

-- Overridded from Organelle:onRemovedFromMicrobe
function NucleusOrganelle:onRemovedFromMicrobe(microbe, q, r)
    ProcessOrganelle.onRemovedFromMicrobe(self, microbe, q, r)
end


function NucleusOrganelle:storage(storage)
    ProcessOrganelle.storage(self, storage)
    return storage
end


function NucleusOrganelle:load(storage)
    self.golgi = Entity()
	self.ER = Entity()
    ProcessOrganelle.load(self, storage)
end

function NucleusOrganelle:update(microbe, logicTime)
    if self.flashDuration ~= nil and self.sceneNode.entity ~= nil then
        self.flashDuration = self.flashDuration - logicTime
        
        local speciesColour = microbe:getSpeciesComponent().colour
        local colorSuffix =  "" .. math.floor(speciesColour.x * 256) .. math.floor(speciesColour.y * 256) .. math.floor(speciesColour.z * 256)
		
		local entity = self.sceneNode.entity
        local golgiEntity = self.golgi.sceneNode.entity
        local ER_entity = self.ER.sceneNode.entity
		-- How frequent it flashes, would be nice to update the flash function
		if math.fmod(self.flashDuration,600) < 300 then      
            entity:tintColour(self.name, self.colour)
            golgiEntity:tintColour("golgi", self.colour)
            ER_entity:tintColour("ER", self.colour)            
		else
			entity:setMaterial(self.name .. colorSuffix)
			golgiEntity:setMaterial("golgi" .. colorSuffix)
			ER_entity:setMaterial("ER" .. colorSuffix)
		end
		
        if self.flashDuration <= 0 then
            self.flashDuration = nil				
			entity:setMaterial(self.name .. colorSuffix)
			golgiEntity:setMaterial("golgi" .. colorSuffix)
			ER_entity:setMaterial("ER" .. colorSuffix)
        end
    end
end

function OrganelleFactory.make_nucleus(data, baseOrganelle)
    NucleusOrganelle.__init(baseOrganelle)
    -- nucleus:addProcess(global_processMap["ReproductaseSynthesis"])
    -- nucleus:addProcess(global_processMap["AminoAcidSynthesis"])
end

function OrganelleFactory.render_nucleus(data)
	local x, y = axialToCartesian(data.q, data.r)
	local translation = Vector3(-x, -y, 0)
	
	data.sceneNode[2].transform.position = translation
	OrganelleFactory.setColour(data.sceneNode[2], data.colour)
	
	local angle = (data.rotation / 60)
	local q = 1
	local r = 0
	for i=0, angle do
		q, r = rotateAxial(q, r)
	end
	x, y = axialToCartesian(q + data.q, r + data.r)
	translation = Vector3(-x, -y, 0)
	data.sceneNode[3].transform.position = translation
	OrganelleFactory.setColour(data.sceneNode[3], data.colour)
	
	q = 0
	r = 1
	for i=0, angle do
		q, r = rotateAxial(q, r)
	end
	x, y = axialToCartesian(q + data.q, r + data.r)
	translation = Vector3(-x, -y, 0)
	data.sceneNode[4].transform.position = translation
	OrganelleFactory.setColour(data.sceneNode[4], data.colour)
	
	data.sceneNode[1].meshName = "nucleus.mesh"
	data.sceneNode[1].transform.position = Vector3(0,0,0)
	data.sceneNode[1].transform.orientation = Quaternion(Radian(Degree(data.rotation)), Vector3(0, 0, 1))
end
