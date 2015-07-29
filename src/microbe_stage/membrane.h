#pragma once

//-----------------------------------------------------------------------
//
//  Name: Membrane.h
//
//  Author: Michael Silver, 2015
//
//  Desc: creates a dynamic membrane around organelles
//
//------------------------------------------------------------------------

#include <windows.h>
#include <vector>
#include <algorithm>
#include <fstream>
#include <math.h>
#include <OgreVector3.h>

class Membrane
{
private:
    // The length in pixels of a side of the square that bounds the membrane.
    int cellDimensions;

    // The amount of points on the side of the membrane.
    int membraneResolution;

    // Stores the generated 2-Dimensional membrane.
    std::vector<Ogre::Vector3>   Vertices2D;

    // Stores the 3-Dimensional membrane such that every 3 points make up a triangle.
    std::vector<Ogre::Vector3>   Vertices3D;

    // Stores the positions of the organelles.
    std::vector<Ogre::Vector3>   organellePos;

public:

	Membrane(std::vector<Ogre::Vector3> organellePos);

	// This function gives out the commands to create and draw the membrane.
	bool	Update();

	// Creates the 2D points in the membrane by looking at the positions of the organelles.
	void	DrawMembrane();

	// Return the position of the closest organelle to the target point if it is less then a certain threshold away.
	Ogre::Vector3 FindClosestOrganelles(Ogre::Vector3 target);

	// Decides where the point needs to move based on the position of the closest organelle.
	Ogre::Vector3	GetMovement(Ogre::Vector3 target, Ogre::Vector3 closestOrganelle);

	// Creates a 3D prism from the 2D vertices.
	void	WriteData(std::vector<Ogre::Vector3> vertices);
};
