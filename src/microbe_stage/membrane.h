#pragma once

//----------------------------------------------------------------------- 
//   
//  Name: Membrane.h
//   
//  Author: Michael Silver 2015
// 
//  Desc: creates a dynamic membrane around organelles
// 
//------------------------------------------------------------------------ 
 
#include <windows.h> 
#include <vector>
#include <algorithm>
#include <math.h>
 
using namespace std;

class Membrane 
{ 
private:

	// Stores the generated 2-Dimensional membrane.
	vector<Point>		MembraneVertices2D;

	// Stores the 3-Dimensional membrane such that every 3 points make up a triangle.
	vector<Point>		MembraneVertices3D;

	// Stores the positions of the organelles.
	vector<Point>		organellePos;
     
public: 
 
	Membrane(vector<Point> organellePos);
 
	// This function gives out the commands to create and draw the membrane.
	bool	Update(); 

	// Creates the 2D points in the membrane by looking at the positions of the organelles
	void	DrawMembrane(vector<SPoint> organellePos);

	// Return the position of the closest organelle to the target point if it is less then a certain threshold away.
	Point	FindClosestOrganelles(Point target, vector<Point> organelles);

	// Desides where the point needs to move based on the position of the closest organelle.
	Vector	GetMovement(SPoint target, SPoint closestOrganelle);

	// Creates a 3D prism from the 2D vertices.
	void	WriteData(vector<Point> vertices);
};