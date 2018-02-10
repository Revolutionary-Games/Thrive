-- Holds config file contents, translated into lua table form

--[[
Style tips:
- Always indent, only with spaces, 4 spaces to an indent.
- Always leave trailing commas on the last elements of multiline tables
    then, the next time something gets added, you don't need to change the line above
    just keeps things cleaner.
- Always leave a trailing newline at the end of the file. 
    Google it, it is the right way to end text files.
]]

agents = {
    oxytoxy = {
        name = "OxyToxy NT",
        weight = 1,
        mesh = "oxytoxy.mesh",
        size = 0.3,
        effect = oxytoxyEffect,
    },
}
-- must be global
chloroplast_Organelle_Number = 0
toxin_Organelle_Number = 0
chloroplast_unlocked = false
toxin_unlocked = false

function toxinEffect()
    toxin_Organelle_Number = toxin_Organelle_Number + 1
    print (toxin_Organelle_Number)
    if toxin_Organelle_Number >= 3 then -- 3 is an example
        toxin_unlock()
        toxin_call_Notification()
    end
    return true
end

function toxin_unlock(entityId)
    if Engine:playerData():lockedMap():isLocked("Toxin") then
        showMessage("Toxin Unlocked!")
        Engine:playerData():lockedMap():unlock("Toxin")
        local guiSoundEntity = Entity("gui_sounds")
        getComponent(guiSoundEntity, SoundSourceComponent):playSound("microbe-pickup-organelle")
    end
end

function chloroplastEffect()
    chloroplast_Organelle_Number = chloroplast_Organelle_Number + 1
    if chloroplast_Organelle_Number >= 3 then  -- 3 is an example
        chloroplast_unlock()
        chloroplast_call_Notification()
    end
    return true
end

function chloroplast_unlock(entityId)
    if Engine:playerData():lockedMap():isLocked("chloroplast") then
        showMessage("Chloroplast Unlocked!")
        Engine:playerData():lockedMap():unlock("chloroplast")
        local guiSoundEntity = Entity("gui_sounds")
        getComponent(guiSoundEntity, SoundSourceComponent):playSound("microbe-pickup-organelle")
    end
end

function chloroplast_call_Notification()
    if chloroplast_unlocked == false then
        global_activeMicrobeStageHudSystem:chloroplastNotificationenable()
        chloroplast_unlocked = true
    end
end
function toxin_call_Notification()
    if toxin_unlocked == false then
        global_activeMicrobeStageHudSystem:toxinNotificationenable()
        toxin_unlocked = true
    end
end

--[[
Placing organelles can get downright annoying if you don't
map them out. To make it easier, download a few sheets of hexgrid 
off the internet. Before you print them though, set up the axes
properly. See http://i.imgur.com/kTxHFMC.png for how. When you're
drawing out your microbe, keep in mind that it faces forward along
the +r direction.
0 degrees is considered up for the rotation (+r), and you can rotate
in 60 degree intervals counter clockwise.
The colour of the microbe should never be lower than (0.3, 0.3, 0.3)
]]

starter_microbes = {
    Default = {
        spawnDensity = 1/14000,
        compounds = {
            atp = {amount=60},
            glucose = {amount = 5},
            oxygen = {amount = 10}
        },
        organelles = {
            {name="nucleus",q=0,r=0, rotation=0},
            {name="mitochondrion",q=-1,r=-2, rotation=240},
            {name="vacuole",q=1,r=-3, rotation=0},
            {name="flagellum",q=-1,r=-3, rotation=0},
            {name="flagellum",q=1,r=-4, rotation=0},
        },
        colour = {r=1,g=1,b=1},
    },
}
