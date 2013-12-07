--------------------------------------------------------------------------------
-- A storage organelle class
--------------------------------------------------------------------------------
class 'StorageOrganelle' (Organelle)

-- Constructor
--
-- @param bandwidth
--  The rate of transfer of this organelle
--
-- @param capacity
--  The maximum stored amount
function StorageOrganelle:__init(bandwidth, capacity)
    Organelle.__init(self)
    self.bandwidth = bandwidth
    self.capacity = capacity
    self.compounds = {}
    self.stored = 0
end


function StorageOrganelle:load(storage)
    Organelle.load(self, storage)
    self.bandwidth = storage:get("bandwidth", 10)
    self.capacity = storage:get("capacity", 100)
end


function StorageOrganelle:storage()
    local storage = Organelle.storage(self)
    storage:set("bandwidth", self.bandwidth)
    storage:set("capacity", self.capacity)
    return storage
end

-- Overridded from Organelle:onAddedToMicrobe
function StorageOrganelle:onAddedToMicrobe(microbe, q, r)
    Organelle.onAddedToMicrobe(self, microbe, q, r)
    microbe:addStorageOrganelle(self)
end

-- Overridded from Organelle:onRemovedFromMicrobe
function StorageOrganelle:onRemovedFromMicrobe(microbe, q, r)
    Organelle.onRemovedFromMicrobe(self, microbe, q, r)
    microbe:removeStorageOrganelle(self)
end

--Stores as much of the compound as possible, returning the amount that wouldn't fit
function StorageOrganelle:storeCompound(compoundId, amount)
    local canFit = (capacity - stored)/CompoundRegistry.getCompoundSize(compoundID)
    if canFit>=amount then
        if amount<=self.bandwidth then
            self.compounds[compoundId] = self.compounds[compoundId] + amount
            self.stored = self.stored + COMPOUND_SIZE[compoundID]*amount
            return 0 else
            self.compounds[compoundId] = self.compounds[compoundId] + bandwidth
            self.stored = self.stored + COMPOUND_SIZE[compoundID]*bandwidth
            return amount-bandwidth
        end else
        if canFit<=self.bandwidth then
            self.compounds[compoundId] = self.compounds[compoundId] + canFit
            self.stored = self.stored + COMPOUND_SIZE[compoundID]*canFit 
            return amount-canFit else
            self.compounds[compoundId] = self.compounds[compoundId] + bandwidth
            self.stored = self.stored + COMPOUND_SIZE[compoundID]*bandwidth
            return amount-bandwidth
        end
    end
end

--Ejects as much of the compound as possible, returning any in excess of bandwidth
function StorageOrganelle:ejectCompound(compoundId, amount)
    if amount<=self.bandwidth then
        self.compounds[compoundId] = self.compounds[compoundId] - amount
        self.stored = self.stored - COMPOUND_SIZE[compoundID]*amount
        if self.compounds[compoundId] < 0 then
            self.compounds[compoundId] = 0
        end
        if self.stored < 0 then
            self.stored = 0
        end
        return 0 else
        self.compounds[compoundId] = self.compounds[compoundId] - bandwidth
        self.stored = self.stored - COMPOUND_SIZE[compoundID]*bandwidth
        if self.compounds[compoundId] < 0 then
            self.compounds[compoundId] = 0
        end
        if self.stored < 0 then
            self.stored = 0
        end
        return amount-bandwidth
    end
end


function StorageOrganelle:update(microbe, milliseconds)
    Organelle.update(self, microbe, milliseconds)
    --vacuoles don't do anything... they just... sit there... any ideas what goes here?
end

