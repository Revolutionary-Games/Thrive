#pragma once

//-----------------------------------------------------------------------
//
//  Name: Membrane.h
//
//  Author: Michael Silver, 2015
//
//  Desc: Creates a dynamic membrane around organelles
//
//------------------------------------------------------------------------

#include <vector>
#include <algorithm>
#include <fstream>
#include <math.h>
#include <OgreVector3.h>

#include "general/mesh.h"

class Membrane: public Mesh
{
private:
    // The length in pixels of a side of the square that bounds the membrane.
    int cellDimensions;

    // The amount of points on the side of the membrane.
    int membraneResolution;

    // Stores the generated 2-Dimensional membrane.
    std::vector<Ogre::Vector3>   vertices2D;

public:
    bool isInitialized;

    // Stores the positions of the organelles.
    std::vector<Ogre::Vector3>   organellePos;

public:
    // Creates a membrane object from the positions of the organelles.
	Membrane();

	// This function gives out the commands to create and draw the membrane.
	// At the moment it does nothing to save fps.
	void	Update(std::vector<Ogre::Vector3> organellePos);

	// Creates a static membrane, pretty much a copy of the update function.
	void    Initialize(std::vector<Ogre::Vector3> organellePos);

	// Creates the 2D points in the membrane by looking at the positions of the organelles.
	void	DrawMembrane();

	// Finds the position of external organelles based on its "internal" location.
	Ogre::Vector3 GetExternalOrganelle(double x, double y);

	// Return the position of the closest organelle to the target point if it is less then a certain threshold away.
	Ogre::Vector3 FindClosestOrganelles(Ogre::Vector3 target);

	// Decides where the point needs to move based on the position of the closest organelle.
	Ogre::Vector3	GetMovement(Ogre::Vector3 target, Ogre::Vector3 closestOrganelle);

	// Creates a 3D prism from the 2D vertices.
	void	MakePrism();
};
