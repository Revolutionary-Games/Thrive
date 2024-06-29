﻿namespace AutoEvo;

using System.Collections.Generic;
using System.Linq;

public class Miche
{
    public Miche? Parent;
    public List<Miche> Children = new();

    // This should always be null if this Miche has children
    public MicrobeSpecies? Occupant;
    public SelectionPressure Pressure;

    public Miche(SelectionPressure pressure)
    {
        Pressure = pressure;
    }

    public bool IsLeafNode()
    {
        return Children.Count == 0;
    }

    public Miche Root()
    {
        if (Parent == null)
        {
            return this;
        }

        return Parent.Root();
    }

    public IEnumerable<Miche> GetLeafNodes()
    {
        if (IsLeafNode())
        {
            return new List<Miche> { this };
        }

        var nodes = new List<Miche>();

        foreach (var child in Children)
        {
            nodes.AddRange(child.GetLeafNodes());
        }

        return nodes;
    }

    public IEnumerable<MicrobeSpecies> GetOccupants()
    {
        var occupants = new List<MicrobeSpecies>();

        if (Occupant != null)
        {
            occupants.Add(Occupant);
        }

        foreach (var child in Children)
        {
            occupants.AddRange(child.GetOccupants());
        }

        return occupants;
    }

    public void AddChild(Miche newChild)
    {
        Children.Add(newChild);
        newChild.Parent = this;
    }

    public void AddChildren(IEnumerable<Miche> newChildren)
    {
        foreach (var child in newChildren)
        {
            AddChild(child);
        }
    }

    public bool InsertSpecies(MicrobeSpecies species, SimulationCache cache, bool dry = false)
    {
        var scoresDictionary = GetOccupants().Distinct().ToDictionary(x => x, _ => 0.0f);

        scoresDictionary[species] = 0.0f;

        return InsertSpecies(species, scoresDictionary, cache, dry);
    }

    /// <summary>
    ///   Inserts a species into any spots on the tree where the species is a better fit than any current occupants
    /// </summary>
    /// <returns>
    ///   Returns a bool based on if the species was inserted into a leaf node
    /// </returns>
    public bool InsertSpecies(MicrobeSpecies species, Dictionary<MicrobeSpecies, float> scoresSoFar,
        SimulationCache cache, bool dry)
    {
        var myScore = Pressure.Score(species, cache);

        // Prune branch if species fails any pressures
        if (myScore <= 0)
            return false;

        if (IsLeafNode() && Occupant == null)
        {
            if (!dry)
                Occupant = species;

            return true;
        }

        var newScores = new Dictionary<MicrobeSpecies, float>();

        foreach (var currentSpecies in GetOccupants())
        {
            newScores[currentSpecies] = scoresSoFar[currentSpecies] +
                Pressure.WeightedComparedScores(myScore, Pressure.Score(currentSpecies, cache));
        }

        // We check here to see if scores more than 0, beacuse
        // scores is relative to the inserted species
        if (IsLeafNode() && newScores[Occupant!] > 0)
        {
            if (!dry)
                Occupant = species;

            return true;
        }

        var inserted = false;
        foreach (var child in Children)
        {
            if (child.InsertSpecies(species, newScores, cache, dry))
            {
                inserted = true;

                if (dry)
                    return true;
            }
        }

        return inserted;
    }

    public List<List<Miche>> AllTraversals()
    {
        if (IsLeafNode())
        {
            return new List<List<Miche>> { new() { this } };
        }

        var traversals = Children.SelectMany(x => x.AllTraversals());
        var retval = new List<List<Miche>>();

        foreach (var list in traversals)
        {
            list.Insert(0, this);
            retval.Add(list);
        }

        return retval;
    }

    public IEnumerable<Miche> BackTraversal()
    {
        return BackTraversal(new List<Miche>());
    }

    public IEnumerable<Miche> BackTraversal(List<Miche> currentTraversal)
    {
        currentTraversal.Insert(0, this);

        if (Parent == null)
            return currentTraversal;

        return Parent.BackTraversal(currentTraversal);
    }

    public Miche DeepCopy()
    {
        // This doesn't copy pressures, but it shouldn't need to
        // Pressures should not have state outside init

        if (IsLeafNode())
        {
            return new Miche(Pressure)
            {
                Occupant = Occupant,
            };
        }

        var newChildren = new List<Miche>(Children).Select(child => child.DeepCopy()).ToList();

        var newMiche = new Miche(Pressure)
        {
            Children = newChildren,
        };

        return newMiche;
    }

    public override int GetHashCode()
    {
        var parentHash = Parent != null ? Parent.GetHashCode() : 53;

        return Pressure.GetHashCode() * 131 ^ parentHash * 587 ^
            (Occupant == null ? 17 : Occupant.GetHashCode()) * 5171;
    }
}
