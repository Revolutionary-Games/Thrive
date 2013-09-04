
function ADD_SYSTEM(cls)
    Engine:addScriptSystem(cls())
end

function REGISTER_COMPONENT(name, cls)
    Engine.componentFactory:registerComponentType(
        name,
        cls
    )
end
