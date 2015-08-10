#pragma once

//-----------------------------------------------------------------------
//
//  Name: Mesh.h
//
//  Author: Michael Silver, 2015
//
//  Desc: Describes an efficient way to store mesh data and allows for
//        the Catmull-Clark Subdivision of such meshes
//
//------------------------------------------------------------------------

#include <OgreVector3.h>
#include <vector>

// Definition of the Half-Edge structure.
struct HE_edge;
struct HE_vert;
struct HE_face;

struct HE_edge
{
    // The vertex the half-edge is coming out of.
	HE_vert* vert;
	// The twin half-edge.
	HE_edge* pair;
	// The face bordering this half-edge.
	HE_face* face;
	// The next edges if going clockwise around the face.
	HE_edge* nextEdge;

	// The edge point created from the edge's midpoint and the average of the face points.
	Ogre::Vector3 avg;
};

struct HE_vert
{
    // The coordinates of the vertex.
	double x, y, z;

    // Constructor.
	HE_vert(Ogre::Vector3 a = Ogre::Vector3(0,0,0)):x(a.x), y(a.y), z(a.z){};
    // An edge coming out of this vertex.
	HE_edge* edge;
	// The number of edges coming out of a vertex.
	int edgeCount;

    // The vertex point created from a weighted average of the face, edge, and old vertex points.
	Ogre::Vector3 avg;
};

// Fuzzy comparison function for HE_verts
inline bool operator<(const HE_vert &lhs, const HE_vert &rhs)
{
	double error = 0.0000001;

	if(lhs.x<(rhs.x-error))
	{
		return true;
	}
	else if(lhs.x>(rhs.x-error) && lhs.x<(rhs.x+error))
	{
		if(lhs.y<(rhs.y-error))
		{
			return true;
		}
		else if(lhs.y>(rhs.y-error) && lhs.y<(rhs.y+error))
		{
			if(lhs.z<(rhs.z-error))
			{
				return true;
			}
			else
			{
				return false;
			}
		}
		else
		{
			return false;
		}
	}
	else
	{
		return false;
	}
}

struct HE_face
{
    // A pointer to an edge this face is part of.
	HE_edge* edge;

	// The average of all the points making up this face.
	Ogre::Vector3 avg;
};

// A class that encompasses all procedural meshes and allows for their subdivision and rendering.
class Mesh
{
private:
    // Functions that calculates the face, edge, and vertex averages, respectively, needed for Catmull-Clark Subdivision.
	void FaceAvg(HE_face* face);
	void EdgeAvg(HE_edge* edge);
	void VertAvg(HE_vert* vert);

    // Converts from storing vertices in a vector to storing them in the half-edge structure.
	void VectorToHE(std::vector<Ogre::Vector3> Points);

protected:
    // All the faces of the Mesh in HE structure.
	std::vector<HE_face*> faces;

public:
    Mesh();
    Mesh(std::vector<Ogre::Vector3> points);

    // Carries out the Catmull-Clark subdivision.
	void Subdivide();

    // Stores the Mesh in a vector such that every 3 points make up a triangle.
    std::vector<Ogre::Vector3> MeshPoints;
};
