--limits the size of the initial stringCodes
local MIN_INITIAL_LENGTH = 5
local MAX_INITIAL_LENGTH = 15

organelleLetters = {}
VALID_LETTERS = {}

--Getting the organelle letters from the organelle table.
for organelleName, organelleInfo in pairs(organelleTable) do
    organelleLetters[organelleInfo.gene] = organelleName

    if organelleInfo.components.NucleusOrganelle == nil then
        table.insert(VALID_LETTERS, organelleInfo.gene)
    end
end

--returns a random organelle letter
function getRandomLetter()
    return VALID_LETTERS[math.random(1, #VALID_LETTERS)]
end

--checks whether an organelle in a certain position would fit within a list of other organelles.
function isValidPlacement(organelleName, q, r, rotation, organelleList)
    --this is super hacky :/
    local data = {
        ["name"] = organelleName,
        ["q"] = q,
        ["r"] = r,
        ["rotation"] = rotation
    }

    local organelleHexes = OrganelleFactory.checkSize(data)

    for _, otherOrganelle in pairs(organelleList) do
        local otherOrganelleHexes = OrganelleFactory.checkSize(otherOrganelle)
        for __, hex in pairs(organelleHexes) do
            for ___, otherHex in pairs(otherOrganelleHexes) do
                if hex.q + q == otherHex.q + otherOrganelle.q and hex.r + r == otherHex.r + otherOrganelle.r then
                    return false
                end
            end
        end
    end
    
    return true
end

--finds a valid position to place the organelle and returns it
--maybe the values should be saved?
function getPosition(organelleName, organelleList)

    local q = 0
    local r = 0

    --Checks whether the center is free.
    for j = 0, 5 do
        rotation = 360 * j / 6
        if isValidPlacement(organelleName, q, r, rotation, organelleList) then
            return q, r, rotation
        end
    end

    local radius = 1
    while true do
        --Moves into the ring of radius "radius" and center (0, 0)
        q = q + HEX_NEIGHBOUR_OFFSET[HEX_SIDE.BOTTOM_LEFT][1]
        r = r + HEX_NEIGHBOUR_OFFSET[HEX_SIDE.BOTTOM_LEFT][2]

        --Iterates in the ring
        for side = 1, 6 do --necesary due to lua not ordering the tables.
            local offset = HEX_NEIGHBOUR_OFFSET[side]
            print(name)
            --Moves "radius" times into each direction
            for i = 1, radius do
                q = q + offset[1]
                r = r + offset[2]
                --print(q, r)

                --Checks every possible rotation value.
                for j = 0, 5 do
                    rotation = 360 * j / 6
                    if isValidPlacement(organelleName, q, r, rotation, organelleList) then
                        return q, r, rotation
                    end
                end
            end
        end
        radius = radius + 1
    end
end

--creates a list of organelles from the stringCode.
function positionOrganelles(stringCode)
    local organelleList = {{
        ["name"] = organelleLetters[string.sub(stringCode, 1, 1)],
        ["q"] = 0,
        ["r"] = 0,
        ["rotation"] = 0
    }}

    for i = 2, string.len(stringCode) do
        local organelle = organelleLetters[string.sub(stringCode, i, i)]
        q, r, rotation = getPosition(organelle, organelleList)
        local newOrganelleData = {
            ["name"] = organelle,
            ["q"] = q,
            ["r"] = r,
            ["rotation"] = rotation
        }

        table.insert(organelleList, newOrganelleData)
    end
    return organelleList
end
