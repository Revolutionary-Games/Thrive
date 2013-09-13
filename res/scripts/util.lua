
function ADD_SYSTEM(cls)
    Engine:addScriptSystem(cls())
end

function REGISTER_COMPONENT(name, cls)
    Engine.componentFactory:registerComponentType(
        name,
        cls
    )
end

function Entity:getOrCreate(componentCls)
    component = self:getComponent(componentCls.TYPE_ID)
    if component == nil then
        component = componentCls()
        self:addComponent(component)
    end
    return component
end
