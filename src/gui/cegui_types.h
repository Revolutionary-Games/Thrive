#pragma once


#ifdef CEGUI_GLM
using CEGUIVector2 = glm::vec2;
#else
using CEGUIVector2 = CEGUI::Vector2f;
#endif
