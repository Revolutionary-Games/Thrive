#include "Node.h"

MoveNode::MoveNode()
{
    
}
std::string MoveNode::getType()
{
    return "Move";
}

std::string RenderNode::getType()
{
    return "Render";
}

ControllerNode::ControllerNode()
{
    
}

std::string ControllerNode::getType()
{
    return "Controller";
}