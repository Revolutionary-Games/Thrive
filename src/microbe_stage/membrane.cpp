#include "membrane.h"

using namespace std;

Membrane::Membrane(): isInitialized(false)
{
    // Half the side length of the original square that is compressed to make the membrane.
    cellDimensions = 10;

    // Amount of segments on one side of the above described square.
	membraneResolution = 10;
}

void Membrane::Update(vector<Ogre::Vector3> organellePositions)
{
    organellePos = organellePositions;

    MeshPoints.clear();
	faces.clear();

    DrawMembrane();
	MakePrism();
	//Subdivide();
    CalcUVCircle();
}

void Membrane::Initialize(vector<Ogre::Vector3> organellePositions)
{
    organellePos = organellePositions;

    for (Ogre::Vector3 pos : organellePos) {
        if (abs(pos.x) + 1 > cellDimensions) {
            cellDimensions = abs(pos.x) + 1;
        }
        if (abs(pos.y) + 1 > cellDimensions) {
            cellDimensions = abs(pos.y) + 1;
        }
    }

	for(int i=0; i<membraneResolution; i++)
	{
		vertices2D.emplace_back(-cellDimensions + 2*cellDimensions/membraneResolution*i, -cellDimensions, 0);
	}
	for(int i=0; i<membraneResolution; i++)
	{
		vertices2D.emplace_back(cellDimensions, -cellDimensions + 2*cellDimensions/membraneResolution*i, 0);
	}
	for(int i=0; i<membraneResolution; i++)
	{
		vertices2D.emplace_back(cellDimensions - 2*cellDimensions/membraneResolution*i, cellDimensions, 0);
	}
	for(int i=0; i<membraneResolution; i++)
	{
		vertices2D.emplace_back(-cellDimensions, cellDimensions - 2*cellDimensions/membraneResolution*i, 0);
	}

	for(int i=0; i<50*cellDimensions; i++)
    {
        DrawMembrane();
    }
	MakePrism();
	//Subdivide();
	CalcUVCircle();

	isInitialized = true;
}

void Membrane::DrawMembrane()
{
    // Stores the temporary positions of the membrane.
	vector<Ogre::Vector3> newPositions = vertices2D;

    // Loops through all the points in the membrane and relocates them as necessary.
	for(size_t i=0, end=newPositions.size(); i<end; i++)
	{
		Ogre::Vector3 closestOrganelle = FindClosestOrganelles(vertices2D[i]);
		if(closestOrganelle == Ogre::Vector3(0,0,-1))
		{
			newPositions[i] = (vertices2D[(end+i-1)%end] + vertices2D[(i+1)%end])/2;
		}
		else
		{
			Ogre::Vector3 movementDirection = GetMovement(vertices2D[i], closestOrganelle);
			newPositions[i].x -= movementDirection.x;
			newPositions[i].y -= movementDirection.y;
		}
	}

	// Allows for the addition and deletion of points in the membrane.
	for(size_t i=0; i<newPositions.size()-1; i++)
	{
		// Check to see if the gap between two points in the membrane is too big.
		if(newPositions[i].distance(newPositions[(i+1)%newPositions.size()]) > cellDimensions/membraneResolution)
		{
			// Add an element after the ith term that is the average of the i and i+1 term.
			auto it = newPositions.begin();
			Ogre::Vector3 tempPoint = (newPositions[(i+1)%newPositions.size()] + newPositions[i])/2;
			newPositions.insert(it+i+1, tempPoint);

			i++;
		}

		// Check to see if the gap between two points in the membrane is too small.
		if(newPositions[(i+1)%newPositions.size()].distance(newPositions[(i-1)%newPositions.size()]) < cellDimensions/membraneResolution)
		{
			// Delete the ith term.
			auto it = newPositions.begin();
			newPositions.erase(it+i);
		}
	}

	vertices2D = newPositions;
}

Ogre::Vector3 Membrane::FindClosestOrganelles(Ogre::Vector3 target)
{
	double closestSoFar = 9;
	int closestIndex = -1;

	for (size_t i=0, end=organellePos.size(); i<end; i++)
	{
		double lenToObject =  target.squaredDistance(organellePos[i]);

		if(lenToObject < 9 && lenToObject < closestSoFar)
		{
			closestSoFar = lenToObject;

			closestIndex = i;
		}
	}

	if(closestIndex != -1)
		return (organellePos[closestIndex]);
	else
		return Ogre::Vector3(0,0,-1);
}

Ogre::Vector3 Membrane::GetMovement(Ogre::Vector3 target, Ogre::Vector3 closestOrganelle)
{
	double power = pow(2.7, (-target.distance(closestOrganelle))/10)/50;

	return (Ogre::Vector3(closestOrganelle)-Ogre::Vector3(target))*power;
}


void Membrane::MakePrism()
{
	double height = .1;

	for(size_t i=0, end=vertices2D.size(); i<end; i++)
	{
		MeshPoints.emplace_back(vertices2D[i%end].x, vertices2D[i%end].y, vertices2D[i%end].z+height/2);
		MeshPoints.emplace_back(vertices2D[(i+1)%end].x, vertices2D[(i+1)%end].y, vertices2D[(i+1)%end].z-height/2);
		MeshPoints.emplace_back(vertices2D[i%end].x, vertices2D[i%end].y, vertices2D[i%end].z-height/2);
		MeshPoints.emplace_back(vertices2D[i%end].x, vertices2D[i%end].y, vertices2D[i%end].z+height/2);
		MeshPoints.emplace_back(vertices2D[(i+1)%end].x, vertices2D[(i+1)%end].y, vertices2D[(i+1)%end].z+height/2);
		MeshPoints.emplace_back(vertices2D[(i+1)%end].x, vertices2D[(i+1)%end].y, vertices2D[(i+1)%end].z-height/2);
	}

	for(size_t i=0, end=vertices2D.size(); i<end; i++)
	{
		MeshPoints.emplace_back(vertices2D[i%end].x, vertices2D[i%end].y, vertices2D[i%end].z+height/2);
		MeshPoints.emplace_back(0,0,height/2);
		MeshPoints.emplace_back(vertices2D[(i+1)%end].x, vertices2D[(i+1)%end].y, vertices2D[(i+1)%end].z+height/2);

		MeshPoints.emplace_back(vertices2D[i%end].x, vertices2D[i%end].y, vertices2D[i%end].z-height/2);
		MeshPoints.emplace_back(vertices2D[(i+1)%end].x, vertices2D[(i+1)%end].y, vertices2D[(i+1)%end].z-height/2);
		MeshPoints.emplace_back(0,0,-height/2);
	}
}

Ogre::Vector3 Membrane::GetExternalOrganelle(double x, double y)
{

    float organelleAngle = Ogre::Math::ATan2(y,x).valueRadians();

    Ogre::Vector3 closestSoFar(0, 0, 0);
    float angleToClosest = Ogre::Math::TWO_PI;

    for(Ogre::Vector3 vertex : vertices2D) {
        if(Ogre::Math::Abs(Ogre::Math::ATan2(vertex.y, vertex.x).valueRadians() - organelleAngle) < angleToClosest) {
            closestSoFar = Ogre::Vector3(vertex.x, vertex.y, 0);
            angleToClosest = Ogre::Math::Abs(Ogre::Math::ATan2(vertex.y, vertex.x).valueRadians() - organelleAngle);
        }
    }

    return closestSoFar;
}
