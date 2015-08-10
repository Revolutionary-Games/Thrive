#include "Mesh.h"

#include <map>

using namespace std;

Mesh::Mesh()
{
}

Mesh::Mesh(vector<Ogre::Vector3> points)
{
	VectorToHE(points);
}

void Mesh::VectorToHE(vector<Ogre::Vector3> points)
{
	map<pair<Ogre::Vector3, Ogre::Vector3>, HE_edge*> Edges;

	faces.clear();

	for(size_t i=0; i<points.size(); i+=3)
	{
		faces.emplace_back(new HE_face);

		if(Edges.find(make_pair(points[i], points[i+1])) == Edges.end())
		{
			Edges[make_pair(points[i], points[i+1])] = new HE_edge;
			Edges[make_pair(points[i], points[i+1])]->face = faces[i/3];
			faces[i/3]->edge = Edges[make_pair(points[i], points[i+1])];
			Edges[make_pair(points[i], points[i+1])]->vert = new HE_vert(points[i]);
			Edges[make_pair(points[i], points[i+1])]->vert->edge = Edges[make_pair(points[i], points[i+1])];
		}
		if(Edges.find(make_pair(points[i+1], points[i+2])) == Edges.end())
		{
			Edges[make_pair(points[i+1], points[i+2])] = new HE_edge;
			Edges[make_pair(points[i+1], points[i+2])]->face = faces[i/3];
			Edges[make_pair(points[i+1], points[i+2])]->vert = new HE_vert(points[i+1]);
			Edges[make_pair(points[i+1], points[i+2])]->vert->edge = Edges[make_pair(points[i+1], points[i+2])];
		}
		if(Edges.find(make_pair(points[i+2], points[i])) == Edges.end())
		{
			Edges[make_pair(points[i+2], points[i])] = new HE_edge;
			Edges[make_pair(points[i+2], points[i])]->face = faces[i/3];
			Edges[make_pair(points[i+2], points[i])]->vert = new HE_vert(points[i+2]);
			Edges[make_pair(points[i+2], points[i])]->vert->edge = Edges[make_pair(points[i+2], points[i])];
		}

		Edges[make_pair(points[i], points[i+1])]->nextEdge = Edges[make_pair(points[i+1], points[i+2])];
		Edges[make_pair(points[i+1], points[i+2])]->nextEdge = Edges[make_pair(points[i+2], points[i])];
		Edges[make_pair(points[i+2], points[i])]->nextEdge = Edges[make_pair(points[i], points[i+1])];

		if(Edges.find(make_pair(points[i+1], points[i])) != Edges.end())
		{
			Edges[make_pair(points[i], points[i+1])]->pair = Edges[make_pair(points[i+1], points[i])];
			Edges[make_pair(points[i+1], points[i])]->pair = Edges[make_pair(points[i], points[i+1])];
		}
		if(Edges.find(make_pair(points[i+2], points[i+1])) != Edges.end())
		{
			Edges[make_pair(points[i+1], points[i+2])]->pair = Edges[make_pair(points[i+2], points[i+1])];
			Edges[make_pair(points[i+2], points[i+1])]->pair = Edges[make_pair(points[i+1], points[i+2])];
		}
		if(Edges.find(make_pair(points[i], points[i+2])) != Edges.end())
		{
			Edges[make_pair(points[i+2], points[i])]->pair = Edges[make_pair(points[i], points[i+2])];
			Edges[make_pair(points[i], points[i+2])]->pair = Edges[make_pair(points[i+2], points[i])];
		}
	}
}

void Mesh::Subdivide()
{
	// Every three elements will make a triangle.
	vector<Ogre::Vector3> MeshPoints;

	for(size_t i=0, end=faces.size(); i<end; i++)
	{
		FaceAvg(faces[i]);
	}
	for(size_t i=0, end=faces.size(); i<end; i++)
	{
		EdgeAvg(faces[i]->edge);
		EdgeAvg(faces[i]->edge->nextEdge);
		EdgeAvg(faces[i]->edge->nextEdge->nextEdge);
	}
	for(size_t i=0, end=faces.size(); i<end; i++)
	{
		VertAvg(faces[i]->edge->vert);
		VertAvg(faces[i]->edge->nextEdge->vert);
		VertAvg(faces[i]->edge->nextEdge->nextEdge->vert);
	}

	for(size_t i=0, end=faces.size(); i<end; i++)
	{
		MeshPoints.push_back(faces[i]->edge->vert->avg);
		MeshPoints.push_back(faces[i]->edge->avg);
		MeshPoints.push_back(faces[i]->avg);

		MeshPoints.push_back(faces[i]->edge->avg);
		MeshPoints.push_back(faces[i]->edge->nextEdge->vert->avg);
		MeshPoints.push_back(faces[i]->avg);

		MeshPoints.push_back(faces[i]->edge->nextEdge->vert->avg);
		MeshPoints.push_back(faces[i]->edge->nextEdge->avg);
		MeshPoints.push_back(faces[i]->avg);

		MeshPoints.push_back(faces[i]->edge->nextEdge->avg);
		MeshPoints.push_back(faces[i]->edge->nextEdge->nextEdge->vert->avg);
		MeshPoints.push_back(faces[i]->avg);

		MeshPoints.push_back(faces[i]->edge->nextEdge->nextEdge->vert->avg);
		MeshPoints.push_back(faces[i]->edge->nextEdge->nextEdge->avg);
		MeshPoints.push_back(faces[i]->avg);

		MeshPoints.push_back(faces[i]->edge->nextEdge->nextEdge->avg);
		MeshPoints.push_back(faces[i]->edge->nextEdge->nextEdge->nextEdge->vert->avg);
		MeshPoints.push_back(faces[i]->avg);
	}

	VectorToHE(MeshPoints);
}

void Mesh::FaceAvg(HE_face* face)
{
	double x_avg = (face->edge->vert->x + face->edge->nextEdge->vert->x + face->edge->nextEdge->nextEdge->vert->x)/3;
	double y_avg = (face->edge->vert->y + face->edge->nextEdge->vert->y + face->edge->nextEdge->nextEdge->vert->y)/3;
	double z_avg = (face->edge->vert->z + face->edge->nextEdge->vert->z + face->edge->nextEdge->nextEdge->vert->z)/3;

	Ogre::Vector3 avg(x_avg, y_avg, z_avg);

	face->avg = avg;
}

void Mesh::EdgeAvg(HE_edge* edge)
{
	double x_avg = (edge->face->avg.x + edge->pair->face->avg.x + edge->vert->x + edge->pair->vert->x)/4;
	double y_avg = (edge->face->avg.y + edge->pair->face->avg.y + edge->vert->y + edge->pair->vert->y)/4;
	double z_avg = (edge->face->avg.z + edge->pair->face->avg.z + edge->vert->z + edge->pair->vert->z)/4;

	Ogre::Vector3 avg(x_avg, y_avg, z_avg);

	edge->avg = avg;
}

void Mesh::VertAvg(HE_vert* vert)
{
	int edgeCount = 0;
	Ogre::Vector3 avgFacePoints(0,0,0);
	Ogre::Vector3 avgMidEdges(0,0,0);

	HE_edge* currEdge = vert->edge;
	do
	{
		avgFacePoints += currEdge->face->avg;
		avgMidEdges += currEdge->avg;

		edgeCount++;
		currEdge = currEdge->pair->nextEdge;
	} while(currEdge != vert->edge);

	vert->edgeCount = edgeCount;

	avgFacePoints = avgFacePoints/edgeCount;
	avgMidEdges = avgMidEdges/edgeCount;

	double m1 = (edgeCount-3.0)/edgeCount;
	double m2 = 1.0/edgeCount;
	double m3 = 2.0/edgeCount;

	Ogre::Vector3 avg = Ogre::Vector3(vert->x, vert->y, vert->z)*m1 + avgFacePoints*m2 + avgMidEdges*m3;

	vert->avg = avg;
}
