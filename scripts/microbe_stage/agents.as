const auto AGENT_EMISSION_VELOCITY = 20;


interface AgentEffect{

    void applyEffect();
}


class Agent{

    Agent(string name, float weight, string mesh, float size, AgentEffect@ effect){

        this.name = name;
        this.weight = weight;
        this.mesh = mesh;
        this.size = size;
        @this.effect = @effect;
    }

    string name;
    float weight;
    string mesh;
    float size;
    AgentEffect@ effect;
}

class OxytoxyEffect : AgentEffect{

    void applyEffect(){

    }
}


const dictionary AGENTS = {
    {"oxytoxy", Agent("OxyToxy NT", 1, "oxytoxy_fluid.mesh", 0.3, OxytoxyEffect())}
};





