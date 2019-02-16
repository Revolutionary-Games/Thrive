#pragma once

#include "engine/component_types.h"

#include "Entities/Component.h"
#include "Entities/System.h"



namespace thrive {


/**
 * @brief Component for entities we wnat to hold little bits of extra data
 */
class AgentProperties : public Leviathan::Component {
public:
    AgentProperties();
    // void
    // load(
    //     const StorageContainer& storage
    // ) override;

    // StorageContainer
    // storage() const override;

    REFERENCE_HANDLE_UNCOUNTED_TYPE(AgentProperties);

    static constexpr auto TYPE =
        componentTypeConvert(THRIVE_COMPONENT::PROPERTIES);

    // For now this will be how we check species and agent type and the entity
    // ID of the parent cell, i know its not the most elegant but for what i
    // want i dont think creating a whole array of generic "properties" is a
    // good use of time just yet.
    std::string
        getSpeciesName();
    std::string
        getAgentType();
    ObjectID
        getParentEntity();
    void
        setSpeciesName(std::string newString);
    void
        setAgentType(std::string newString);
    void
        setParentEntity(ObjectID parentId);


    std::string speciesName;
    std::string agentType;
    ObjectID parentId = NULL_OBJECT;
};
} // namespace thrive
