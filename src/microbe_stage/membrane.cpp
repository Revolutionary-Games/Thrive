#include "membrane.h"

using namespace std;

Membrane::Membrane(vector<Ogre::Vector3> organellePositions): organellePos(organellePositions)
{
    cellDimensions = 50;
    membraneResolution = 5;

    organellePos.emplace_back(0,0,0);
	organellePos.emplace_back(0,-20,0);
	organellePos.emplace_back(0,20,0);
	organellePos.emplace_back(0,-40,0);
	organellePos.emplace_back(17,-10,0);
	organellePos.emplace_back(-17,-10,0);
	organellePos.emplace_back(-34,-20,0);
	organellePos.emplace_back(34,-20,0);
	organellePos.emplace_back(-34,-40,0);
	organellePos.emplace_back(34,-40,0);
	organellePos.emplace_back(-17,10,0);
	organellePos.emplace_back(17,10,0);
	organellePos.emplace_back(-17,-30,0);
	organellePos.emplace_back(17,-30,0);
	organellePos.emplace_back(0,40,0);

	for(int i=0; i<membraneResolution; i++)
	{
		Vertices2D.emplace_back(-cellDimensions + 2*cellDimensions/membraneResolution*i, -cellDimensions, 0);
	}
	for(int i=0; i<membraneResolution; i++)
	{
		Vertices2D.emplace_back(cellDimensions, -cellDimensions + 2*cellDimensions/membraneResolution*i, 0);
	}
	for(int i=0; i<membraneResolution; i++)
	{
		Vertices2D.emplace_back(cellDimensions - 2*cellDimensions/membraneResolution*i, cellDimensions, 0);
	}
	for(int i=0; i<membraneResolution; i++)
	{
		Vertices2D.emplace_back(-cellDimensions, cellDimensions - 2*cellDimensions/membraneResolution*i, 0);
	}

	for(int i=0; i<200; i++)
	{
		DrawMembrane();
	}
}

bool Membrane::Update()
{
//	for(size_t i=0, end=organellePos.size(); i<end; i++)
//	{
//		organellePos[i].y-=.1;
//	{
	DrawMembrane();
	WriteData(Vertices2D);

	return true;
}

void Membrane::DrawMembrane()
{
    // Stores the temporary positions of the membrane.
	vector<Ogre::Vector3> newPositions = Vertices2D;

    // Loops through all the points in the membrane and relocates them as necessary.
	for(size_t i=0, end=newPositions.size(); i<end; i++)
	{
		Ogre::Vector3 closestOrganelle = FindClosestOrganelles(Vertices2D[i]);
		if(closestOrganelle == Ogre::Vector3(0,0,-1))
		{
			newPositions[i] = (Vertices2D[(end+i-1)%end] + Vertices2D[(i+1)%end] + Vertices2D[(end+i-2)%end] + Vertices2D[(i+2)%end])/4;
		}
		else
		{
			Ogre::Vector3 movementDirection = GetMovement(Vertices2D[i], closestOrganelle);
			newPositions[i].x -= movementDirection.x;
			newPositions[i].y -= movementDirection.y;
		}
	}

	// Allows for the addition and deletion of points in the membrane.
	for(size_t i=0; i<newPositions.size(); i++)
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

	Vertices2D = newPositions;
}

Ogre::Vector3 Membrane::FindClosestOrganelles(Ogre::Vector3 target)
{
	double closestSoFar = 400;
	int closestIndex = -1;

	for (size_t i=0, end=organellePos.size(); i<end; i++)
	{
		double lenToObject =  target.squaredDistance(organellePos[i]);

		if(lenToObject < 400 && lenToObject < closestSoFar)
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
	double power = pow(2.7, (-target.distance(closestOrganelle))/100)/5;

	return (Ogre::Vector3(closestOrganelle)-Ogre::Vector3(target))*power;
}


void Membrane::WriteData(vector<Ogre::Vector3> vertices)
{
	ofstream outputFile("membrane.txt");

	vector<Ogre::Vector3> membranePrism;
	double height = 30;

	for(size_t i=0, end=vertices.size(); i<end; i++)
	{
		membranePrism.emplace_back(vertices[i%end].x, vertices[i%end].y, vertices[i%end].z+height/2);
		membranePrism.emplace_back(vertices[(i+1)%end].x, vertices[(i+1)%end].y, vertices[(i+1)%end].z-height/2);
		membranePrism.emplace_back(vertices[i%end].x, vertices[i%end].y, vertices[i%end].z-height/2);
		membranePrism.emplace_back(vertices[i%end].x, vertices[i%end].y, vertices[i%end].z+height/2);
		membranePrism.emplace_back(vertices[(i+1)%end].x, vertices[(i+1)%end].y, vertices[(i+1)%end].z+height/2);
		membranePrism.emplace_back(vertices[(i+1)%end].x, vertices[(i+1)%end].y, vertices[(i+1)%end].z-height/2);
	}

	for(size_t i=0, end=vertices.size(); i<end; i++)
	{
		membranePrism.emplace_back(vertices[i%end].x, vertices[i%end].y, vertices[i%end].z+height/2);
		membranePrism.emplace_back(0,0,height/2);
		membranePrism.emplace_back(vertices[(i+1)%end].x, vertices[(i+1)%end].y, vertices[(i+1)%end].z+height/2);

		membranePrism.emplace_back(vertices[i%end].x, vertices[i%end].y, vertices[i%end].z-height/2);
		membranePrism.emplace_back(vertices[(i+1)%end].x, vertices[(i+1)%end].y, vertices[(i+1)%end].z-height/2);
		membranePrism.emplace_back(0,0,-height/2);
	}

	if (outputFile.is_open())
	{
		outputFile << "Vertex Count: " << membranePrism.size() << endl;
		outputFile << endl << "Data:" << endl << endl;

		for(size_t i=0; i<membranePrism.size(); i++)
		{
			outputFile << membranePrism[i].x/40 << " ";
			outputFile << membranePrism[i].y/40 << " ";
			outputFile << membranePrism[i].z/40 << " ";
			outputFile << ".5" << " ";
			outputFile << ".5" << " ";
			outputFile << "0" << " ";
			outputFile << "0" << " ";
			outputFile << "-1" << " " << endl;
		}

		outputFile.close();
	}
	else
	{
	    MessageBox(NULL, "Could not create file: membrane.txt", "Error", 0);
	}
}
