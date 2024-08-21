namespace AutoEvo;

using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
///   A node for the Miche Tree
/// </summary>
/// <remarks>
///   <para>
///     The Miche class forms a tree by storing a list of child instances of Miche Nodes. If a Miche has no children
///     it is considered a leaf node and can have a species Occupant instead (otherwise Occupant should be null).
///     This class handles insertion into the tree through scores from the selection pressure it contains.
///     For a fuller explanation see docs/auto_evo.md
///   </para>
/// </remarks>
public class Miche
{
    public readonly SelectionPressure Pressure;
    public readonly List<Miche> Children = new();
    public Miche? Parent;

    /// <summary>
    ///   The species that currently occupies this Miche
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     Occupant should always be null if this Miche is not a leaf node (children is not empty).
    ///   </para>
    /// </remarks>
    public Species? Occupant;

    public Miche(SelectionPressure pressure)
    {
        Pressure = pressure;
    }

    public bool IsLeafNode()
    {
        return Children.Count == 0;
    }

    public void GetLeafNodes(List<Miche> nodes)
    {
        if (IsLeafNode())
        {
            nodes.Add(this);
            return;
        }

        foreach (var child in Children)
        {
            child.GetLeafNodes(nodes);
        }
    }

    public void GetLeafNodes(List<Miche> nodes, Func<Miche, bool> criteria)
    {
        if (IsLeafNode() && criteria(this))
        {
            nodes.Add(this);
            return;
        }

        foreach (var child in Children)
        {
            child.GetLeafNodes(nodes, criteria);
        }
    }

    public void GetLeafNodes(List<Miche> passingNodes, List<Miche> failingNodes, Func<Miche, bool> criteria)
    {
        if (IsLeafNode())
        {
            if (criteria(this))
            {
                passingNodes.Add(this);
            }
            else
            {
                failingNodes.Add(this);
            }

            return;
        }

        foreach (var child in Children)
        {
            child.GetLeafNodes(passingNodes, failingNodes, criteria);
        }
    }

    /// <summary>
    ///   Adds occupants of this and all child miches to the set. Does not clear the set before adding.
    /// </summary>
    /// <param name="occupantsSet">Where to *append* the results</param>
    public void GetOccupants(HashSet<Species> occupantsSet)
    {
        if (Occupant != null)
        {
            occupantsSet.Add(Occupant);
            return;
        }

        foreach (var child in Children)
        {
            child.GetOccupants(occupantsSet);
        }
    }

    public IEnumerable<Miche> BackTraversal(List<Miche> currentTraversal)
    {
        currentTraversal.Insert(0, this);

        if (Parent == null)
            return currentTraversal;

        return Parent.BackTraversal(currentTraversal);
    }

    public void AddChild(Miche newChild)
    {
        Children.Add(newChild);
        newChild.Parent = this;
    }

    /// <summary>
    ///   Inserts a species into any spots on the tree where the species is a better fit than any current occupants
    /// </summary>
    /// <param name="species">Species to try to insert</param>
    /// <param name="patch">Patch this miche is in for calculating scores</param>
    /// <param name="scoresSoFar">
    ///   Scores generated so far. If not called recursively just pass in null. Not modified by this method.
    /// </param>
    /// <param name="cache">Data cache for faster calculation</param>
    /// <param name="dry">If true the species is not inserted but only checked if it could be inserted</param>
    /// <param name="workingMemory">Temporary working memory to use by this method, automatically cleared</param>
    /// <returns>
    ///   Returns a bool based on if the species was inserted into a leaf node
    /// </returns>
    public bool InsertSpecies(Species species, Patch patch, Dictionary<Species, float>? scoresSoFar,
        SimulationCache cache, bool dry, HashSet<Species> workingMemory)
    {
        var myScore = Pressure.Score(species, patch, cache);

        // Prune branch if species fails any pressures
        if (myScore <= 0)
            return false;

        if (IsLeafNode() && Occupant == null)
        {
            if (!dry)
                Occupant = species;

            return true;
        }

        // TODO: somehow avoid this recursive method call memory allocations
        var newScores = new Dictionary<Species, float>();

        workingMemory.Clear();
        GetOccupants(workingMemory);

        // Build new scores on top of previous values
        if (scoresSoFar == null)
        {
            // Initial call, not recursive

            foreach (var currentSpecies in workingMemory)
            {
                newScores[currentSpecies] =
                    Pressure.WeightedComparedScores(myScore, Pressure.Score(currentSpecies, patch, cache));
            }
        }
        else
        {
            foreach (var currentSpecies in workingMemory)
            {
                var addedScoreAmount =
                    Pressure.WeightedComparedScores(myScore, Pressure.Score(currentSpecies, patch, cache));

                // If some species doesn't have a score yet, the score it starts off with is 0
                if (scoresSoFar.TryGetValue(currentSpecies, out var score))
                {
                    newScores[currentSpecies] = score + addedScoreAmount;
                }
                else
                {
                    newScores[currentSpecies] = addedScoreAmount;
                }
            }
        }

        // We check here to see if scores more than 0, because
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
            if (child.InsertSpecies(species, patch, newScores, cache, dry, workingMemory))
            {
                inserted = true;

                if (dry)
                    return true;
            }
        }

        return inserted;
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

        var newMiche = new Miche(Pressure);
        newMiche.Children.AddRange(newChildren);

        return newMiche;
    }

    public override int GetHashCode()
    {
        var parentHash = Parent != null ? Parent.GetHashCode() : 53;

        // TODO: as Occupant can change it should not be used as part of the hash code
        return Pressure.GetHashCode() * 131 ^ parentHash * 587 ^
            (Occupant == null ? 17 : Occupant.GetHashCode()) * 5171;
    }
}
