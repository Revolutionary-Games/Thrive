--------------------------------------------------------------------------------
-- Class processes that can be attached to process organelles
--------------------------------------------------------------------------------
class 'Process'

INPUT_CONCENTRATION_WEIGHT = 4
OUTPUT_CONCENTRATION_WEIGHT = 0.1

MAX_EXPECTED_PROCESS_ATP_COST = 2

-- Constructor
--
-- @param basicRate
-- How many times in a second, at maximum, the process will transform input to output
--
-- @param parentMicrobe
-- The microbe from which to take input compounds and to which to deposit output compounds
--
-- @param inputCompounds
-- A dictionary of used compoundIds as keys and amounts as values
--
-- @param outputCompounds
-- A dictionary of produced compoundIds as keys and amounts as values
--
function Process:__init(basicRate, atpCost, inputCompounds, outputCompounds)
    self.basicRate = basicRate
    self.inputCompounds = inputCompounds
    self.outputCompounds = outputCompounds
    self.priority = 0
    self.inConcentrationFactor = 1.0
    self.outConcentrationFactor = 1.0
    self.atpCost = atpCost
    -- costPriorityFactor and inputUnitSum are used as minor precalculation optimizations
    self.costPriorityFactor = 1 - atpCost* 0.5 / MAX_EXPECTED_PROCESS_ATP_COST
    self.inputUnitSum = 0 
    for _ ,amount in pairs(self.inputCompounds) do
        self.inputUnitSum = self.inputUnitSum + amount
    end
end


function Process:updateFactors(parentMicrobe)
    -- Update processPriority
    self.priority = 0
    for compoundId ,amount in pairs(self.outputCompounds) do
        self.priority = self.priority + (parentMicrobe.microbe.compoundPriorities[compoundId] * amount)
    end
    -- Update input concentration factor
    self.inConcentrationFactor = 1.0
   
   
    -- Find minimum concentration and use as limiting factor
    for compoundId ,_ in pairs(self.inputCompounds) do
        local compoundConcentration = (parentMicrobe:getCompoundAmount(compoundId) / parentMicrobe.microbe.capacity)
        if compoundConcentration < self.inConcentrationFactor then
            self.inConcentrationFactor = compoundConcentration
        end
    end
   
    --Alternate way of looking at concentrations - product of concentrations:
   -- Multiplying up concentrations for all input compounds
    --for compoundId ,_ in pairs(self.inputCompounds) do
    -- self.inConcentrationFactor = self.inConcentrationFactor * (parentMicrobe:getCompoundAmount(compoundId) / parentMicrobe.microbe.capacity)
   -- end


   -- Multiplying up concentrations for all output compounds
    self.outConcentrationFactor = 1.0
    for compoundId ,_ in pairs(self.outputCompounds) do
        self.outConcentrationFactor = self.outConcentrationFactor * (parentMicrobe:getCompoundAmount(compoundId) / parentMicrobe.microbe.capacity)
    end
end

INPUT_CONCENTRATION_WEIGHT = 1
OUTPUT_CONCENTRATION_WEIGHT = 0.5
CAPACITY_EVEN_FACTOR = 0.4
-- Run the process for a given amount of time
--
-- @param milliseconds
-- The simulation time
--
function Process:produce(milliseconds, capacityFactor, parentMicrobe)
    -- Factor here is the value we multiply on to the compound amounts to be produced and consumed,
    -- so a factor=1.0 would mean that respiration would use 1 glucose and produce 6 Oxygen
    -- Lower values for in and out weights here bring the factors closer to constant 1 (no effect)
    -- We want to do 1-concentration for output concentration so that higher concentration gives lower factor
    local factor = (self.inConcentrationFactor^INPUT_CONCENTRATION_WEIGHT) *
                    (1-(OUTPUT_CONCENTRATION_WEIGHT * self.outConcentrationFactor)) *
                    self.basicRate *
                    (milliseconds/1000) *
                    capacityFactor
    local cost =  factor * self.atpCost
    if parentMicrobe:getCompoundAmount(CompoundRegistry.getCompoundId("atp")) > cost then
        -- Making sure microbe has the required amount of input compounds, otherwise lower factor
        for compoundId ,baseAmount in pairs(self.inputCompounds) do
            if factor * baseAmount > parentMicrobe:getCompoundAmount(compoundId) then
                factor = parentMicrobe:getCompoundAmount(compoundId)/self.inputCompounds[compoundId]
            end
        end
        -- Take compounds from microbe
        for compoundId ,baseAmount in pairs(self.inputCompounds) do
            parentMicrobe:takeCompound(compoundId, baseAmount*factor)
        end
        -- Give compounds to microbe
        for compoundId ,baseAmount in pairs(self.outputCompounds) do
            parentMicrobe:storeCompound(compoundId, baseAmount*factor)
        end
        parentMicrobe:takeCompound(CompoundRegistry.getCompoundId("atp"), cost)
    else
        factor = 0
    end
    return factor
end

function Process:storage()
    storage:set("basicRate", self.basicRate)
    storage:set("priority", self.priority)
    storage:set("inConcentrationFactor", self.inConcentrationFactor)
    storage:set("outConcentrationFactor", self.outConcentrationFactor)
    storage:set("atpCost", self.atpCost)
    storage:set("inputUnitSum", self.inputUnitSum)
    local inputCompoundsSt = StorageList()
    for compoundId, amount in pairs(self.inputCompounds) do
        inputStorage = StorageContainer()
        inputStorage:set("compoundId", compoundId)
        inputStorage:set("amount", amount)
        inputCompoundsSt:append(inputStorage)
    end
    storage:set("inputCompounds", inputCompoundsSt)
    local outputCompoundsSt = StorageList()
    for compoundId, amount in pairs(self.outputCompounds) do
        outputStorage = StorageContainer()
        outputStorage:set("compoundId", compoundId)
        outputStorage:set("amount", amount)
        outputCompoundsSt:append(outputStorage)
    end
    storage:set("outputCompounds", outputCompoundsSt)
    return storage
end


function Process:load(storage)
    self.originalColour = self._colour
    self.basicRate = storage:get("basicRate", 0)
    self.priority = storage:get("priority", 0)
    self.inConcentrationFactor = storage:get("inConcentrationFactor", 1.0)
    self.outConcentrationFactor = storage:get("outConcentrationFactor", 1.0)
    self.atpCost = storage:get("atpCost", 0)
    self.inputUnitSum = storage:get("inputUnitSum", 0)
    self.costPriorityFactor = 1 - self.atpCost* 0.5 / MAX_EXPECTED_PROCESS_ATP_COST
    local inputCompoundsSt = storage:get("inputCompounds", {})
    for i = 1,inputCompoundsSt:size() do
        local inputStorage = inputCompoundsSt:get(i)
        self.inputCompounds[inputStorage:get("compoundId", 0)] = inputStorage:get("amount", 0)
    end
    local outputCompoundsSt = storage:get("outputCompounds", {})
    for i = 1,outputCompoundsSt:size() do
        local outputStorage = outputCompoundsSt:get(i)
        self.outputCompounds[outputStorage:get("compoundId", 0)] = outputStorage:get("amount", 0)
    end
end


--------------------------------------------------------------------------------
-- Class for Organelles capable of producing compounds
--------------------------------------------------------------------------------
class 'ProcessOrganelle' (Organelle)

PROCESS_CAPACITY_UPDATE_INTERVAL = 1000

-- Constructor
function ProcessOrganelle:__init()
    Organelle.__init(self)
    self.originalColour = ColourValue(1,1,1,1)
    self.processes = {}
    self.capacityIntervalTimer = PROCESS_CAPACITY_UPDATE_INTERVAL
end

-- Adds a process to the processing organelle
-- The organelle will distribute its capacity between processes
--
-- @param process
-- The process to add
function ProcessOrganelle:addProcess(process)
    table.insert(self.processes, process)
end


-- Overridded from Organelle:onAddedToMicrobe
function ProcessOrganelle:onAddedToMicrobe(microbe, q, r)
    Organelle.onAddedToMicrobe(self, microbe, q, r)
    microbe:addProcessOrganelle(self)
end

-- Overridded from Organelle:onRemovedFromMicrobe
function ProcessOrganelle:onRemovedFromMicrobe(microbe, q, r)
    microbe:removeProcessOrganelle(self)
    Organelle.onRemovedFromMicrobe(self, microbe, q, r)
end

-- Private function used to update colour of organelle based on how full it is
function ProcessOrganelle:_updateColourDynamic(factorProduct)
    -- Scaled Factor Product (using a sigmoid to accommodate that factor will be low)
    local SFP = 1/(0.8+2^(-factorProduct*64))-0.5
    
    self._colour = ColourValue(0.6 + (self.originalColour.r-0.6)*SFP,
                               0.6 + (self.originalColour.g-0.6)*SFP,
                               0.6 + (self.originalColour.b-0.6)*SFP, 1) -- Calculate colour relative to how close the organelle is to have enough input compounds to produce
end


-- Called by Microbe:update
--
-- Produces compounds for the process at intervals
--
-- @param microbe
-- The microbe containing the organelle
--
-- @param milliseconds
-- The time since the last call to update()
function ProcessOrganelle:update(microbe, milliseconds)
    Organelle.update(self, microbe, milliseconds)
    
    self.capacityIntervalTimer = self.capacityIntervalTimer + milliseconds
    processFactoredPriorities = {}
    factorProduct = 1.0
    if self.capacityIntervalTimer > PROCESS_CAPACITY_UPDATE_INTERVAL then
        local prioritySum = 0
        for _, process in ipairs(self.processes) do
            process:updateFactors(microbe)
            -- We use input compound concentrations to reduce the priority of the process
            local priorityValue = process.priority * process.inConcentrationFactor
            processFactoredPriorities[process] = priorityValue
            prioritySum = prioritySum + process.priority
        end
        -- calculate capacity distribution and perform processing:
        if prioritySum > 0 then -- Avoid division by 0
            for _, process in ipairs(self.processes) do
                    -- (processPriority / sumOfAllProcessPriorities) * (1-X) + X/numOfProcesses
                    local capacityFactor = (process.priority / prioritySum) * (1-CAPACITY_EVEN_FACTOR) + CAPACITY_EVEN_FACTOR/#self.processes
                    factorProduct = factorProduct * process:produce(self.capacityIntervalTimer, capacityFactor, microbe)
            end
        else
            factorProduct = 0
        end
        self.capacityIntervalTimer = 0
        self._needsColourUpdate = true -- Update colours for displaying completeness of organelle production
        self:_updateColourDynamic(factorProduct)
    end
end


-- Override from Organelle:setColour
function ProcessOrganelle:setColour(colour)
    Organelle.setColour(self, colour)
    self.originalColour = colour
    self:_updateColourDynamic(0)
    self._needsColourUpdate = true
end


function ProcessOrganelle:storage()
    local storage = Organelle.storage(self)
    storage:set("capacityIntervalTimer", self.capacityIntervalTimer)
    local processes = StorageList()
    for _, process in ipairs(self.processes) do
        processes:append(process:storage())
    end
    storage:set("processes", processes)
    return storage
end


function ProcessOrganelle:load(storage)
    Organelle.load(self, storage)
    self.originalColour = self._colour -- _colour was loaded by the base-class load
    self.capacityIntervalTimer = storage:get("capacityIntervalTimer", 0)
    local processes = storage:get("processes", {})
    for i = 1,processes:size() do
        local process = Process(0, 0, {},{})
        process:load(processes:get(i))
        self:addProcess(process)
    end
end
