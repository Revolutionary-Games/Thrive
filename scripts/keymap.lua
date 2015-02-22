-- Holds the keymap

kmp = {}

-- Microbe Editor --

kmp.undo = {"ctrl", "U"}
kmp.redo = {"ctrl", "R"}

kmp.remove = {"R"}
kmp.newmicrobe = {"C"}

kmp.vacuole = {"S"}
kmp.mitochondrion = {"M"}
kmp.oxytoxyvacuole = {"T"}
kmp.flagellum = {"F"}
kmp.chloroplast = {"P"}

kmp.togglegrid = {"G"}

kmp.rename = {"F12"}
kmp.gotostage = {"F2"}

-- Microbe Stage --

kmp.forward = {"W"}
kmp.backward = {"S"}
kmp.leftward = {"A"}
kmp.rightward = {"D"}

kmp.shootoxytoxy = {"E"}
kmp.reproduce = {"P"}

kmp.togglemenu = {"ESCAPE"}
kmp.gotoeditor = {"F2"}
kmp.altuniverse = {"F1"}

-- this is the perfect kind of thing to move into C++
-- it shouldn't require anything in Lua, it'll just get a table of strings, and do comparisons
function keyCombo(combo)
    -- Boolean function, used to check if key combo is pressed
    -- doesn't handle modifier keys properly yet (eg, ctrl+R will activate R)

    mods = {} -- holds whether each particular modifier key (left-right-agnostic) is pressed
    mods.ctrl = false
    mods.alt = false
    mods.shift = false

    for _, key in ipairs(combo) do
        if key == "ctrl" then
            mods.ctrl = true
        elseif key == "shift" then
            mods.shift = true
        elseif key == "alt" then
            mods.alt = true
        elseif not Engine.keyboard:wasKeyPressed(Keyboard["KC_"..key]) then
            return false
        end
    end
    -- fail if any modkey pressed unmatches required mods

    if (Engine.keyboard:isKeyDown(Keyboard.KC_LCONTROL) 
        or Engine.keyboard:isKeyDown(Keyboard.KC_RCONTROL)
        ) ~= mods.ctrl then
        return false
    end
    if (Engine.keyboard:isKeyDown(Keyboard.KC_LSHIFT)
        or Engine.keyboard:isKeyDown(Keyboard.KC_RSHIFT)
        ) ~= mods.shift then
        return false
    end
    if (Engine.keyboard:isKeyDown(Keyboard.KC_LMENU)
        or Engine.keyboard:isKeyDown(Keyboard.KC_RMENU)
        ) ~= mods.alt then
        return false
    end
    return true
end
