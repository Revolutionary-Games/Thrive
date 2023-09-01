using System;
using System.Collections.Generic;
using Godot;

/// <summary>
///   Helps container types that dynamically create child <see cref="Control"/>s to add the children to the navigation
///   flow
/// </summary>
public class FocusFlowDynamicChildrenHelper
{
    private readonly Control owner;

    private NodePath? focusPreviousControl;
    private NodePath? focusNextControl;

    private NodePath? focusLeftControl;
    private NodePath? focusRightControl;
    private NodePath? focusUpControl;
    private NodePath? focusDownControl;

    private bool nextNavigatesToChildren;

    private NavigationInChildrenDirection inChildrenDirection;

    private NavigationToChildrenDirection toChildrenDirection;

    public FocusFlowDynamicChildrenHelper(Control owner, NavigationToChildrenDirection toChildren,
        NavigationInChildrenDirection inChildren, bool nextGoesToChildren = true)
    {
        this.owner = owner;
        toChildrenDirection = toChildren;
        inChildrenDirection = inChildren;
        nextNavigatesToChildren = nextGoesToChildren;

        ReReadOwnerNeighbours();
    }

    public enum NavigationInChildrenDirection
    {
        Horizontal,
        Vertical,
        Both,
    }

    public enum NavigationToChildrenDirection
    {
        Horizontal,
        Vertical,
        Both,

        /// <summary>
        ///   Parent doesn't navigate to children, children navigate directly past the parent when exiting the children
        ///   list
        /// </summary>
        None,

        /// <summary>
        ///   Only move vertically into the children, otherwise acts like <see cref="None"/>
        /// </summary>
        VerticalToChildrenOnly,
    }

    /// <summary>
    ///   Applies the navigation neighbours to owner and between children based on the configuration
    /// </summary>
    /// <param name="children">The child nodes. When empty the default navigation is restored</param>
    /// <param name="childNavigationTargets">
    ///   If not null needs to be a sequence of equal length to <see cref="children"/> that specifies the navigation
    ///   targets that should be used when navigating to the child rather than just using the child as the
    ///   navigation target
    /// </param>
    /// <param name="backwardsChildNavigation">
    ///   When set overrides the backwards navigation targets to be these. Very similar to
    ///   <see cref="childNavigationTargets"/> but this applies when navigating backwards
    /// </param>
    public void ApplyNavigationFlow(IEnumerable<Control> children, IEnumerable<Control>? childNavigationTargets = null,
        IEnumerable<Control>? backwardsChildNavigation = null)
    {
        var ownerPath = owner.GetPath();

        NodePath? firstChildPath = null;
        Control? previousChild = null;
        NodePath? previousChildPath = null;

        using var realPathsEnumerator = childNavigationTargets?.GetEnumerator();
        using var reversePathsEnumerator = backwardsChildNavigation?.GetEnumerator();

        foreach (var currentChild in children)
        {
            NodePath currentPath;

            if (realPathsEnumerator != null)
            {
                if (!realPathsEnumerator.MoveNext())
                    throw new ArgumentException("Child and navigation target list sizes don't match");

                if (realPathsEnumerator.Current == null)
                    throw new ArgumentException("Navigation targets has a null in it", nameof(childNavigationTargets));

                currentPath = realPathsEnumerator.Current.GetPath();
            }
            else
            {
                currentPath = currentChild.GetPath();
            }

            var reversePath = currentPath;

            if (reversePathsEnumerator != null)
            {
                if (!reversePathsEnumerator.MoveNext() || reversePathsEnumerator.Current == null)
                    throw new ArgumentException("Invalid back navigation items", nameof(childNavigationTargets));

                reversePath = reversePathsEnumerator.Current.GetPath();
            }

            // These serve as base directions that get overwritten with more specific values
            currentChild.FocusNeighbourBottom = focusDownControl;
            currentChild.FocusNeighbourRight = focusRightControl;

            if (toChildrenDirection is NavigationToChildrenDirection.None
                or NavigationToChildrenDirection.VerticalToChildrenOnly)
            {
                currentChild.FocusNeighbourTop = focusUpControl;
                currentChild.FocusNeighbourLeft = focusLeftControl;
            }
            else
            {
                currentChild.FocusNeighbourTop = ownerPath;
                currentChild.FocusNeighbourLeft = ownerPath;
            }

            // First child-owner focuses
            if (firstChildPath == null)
            {
                firstChildPath = currentPath;

                if (nextNavigatesToChildren)
                {
                    owner.FocusNext = firstChildPath;
                    currentChild.FocusPrevious = ownerPath;
                }

                switch (toChildrenDirection)
                {
                    case NavigationToChildrenDirection.Horizontal:
                    {
                        currentChild.FocusNeighbourLeft = ownerPath;

                        owner.FocusNeighbourRight = firstChildPath;
                        break;
                    }

                    case NavigationToChildrenDirection.Vertical:
                    {
                        currentChild.FocusNeighbourTop = ownerPath;

                        owner.FocusNeighbourBottom = firstChildPath;
                        break;
                    }

                    case NavigationToChildrenDirection.Both:
                    {
                        currentChild.FocusNeighbourLeft = ownerPath;
                        currentChild.FocusNeighbourTop = ownerPath;

                        owner.FocusNeighbourRight = firstChildPath;
                        owner.FocusNeighbourBottom = firstChildPath;
                        break;
                    }

                    case NavigationToChildrenDirection.None or NavigationToChildrenDirection.VerticalToChildrenOnly:
                    {
                        if (nextNavigatesToChildren)
                            currentChild.FocusPrevious = focusPreviousControl;

                        currentChild.FocusNeighbourLeft = focusLeftControl;
                        currentChild.FocusNeighbourTop = focusUpControl;
                        break;
                    }

                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            // Subsequent children focuses between children
            if (previousChild != null)
            {
                switch (inChildrenDirection)
                {
                    case NavigationInChildrenDirection.Horizontal:
                    {
                        previousChild.FocusNeighbourRight = currentPath;
                        currentChild.FocusNeighbourLeft = previousChildPath;
                        break;
                    }

                    case NavigationInChildrenDirection.Vertical:
                    {
                        previousChild.FocusNeighbourBottom = currentPath;
                        currentChild.FocusNeighbourTop = previousChildPath;
                        break;
                    }

                    case NavigationInChildrenDirection.Both:
                    {
                        previousChild.FocusNeighbourRight = currentPath;
                        currentChild.FocusNeighbourLeft = previousChildPath;

                        previousChild.FocusNeighbourBottom = currentPath;
                        currentChild.FocusNeighbourTop = previousChildPath;
                        break;
                    }

                    default:
                        throw new ArgumentOutOfRangeException();
                }

                previousChild.FocusNext = currentPath;
            }

            previousChild = currentChild;
            previousChildPath = reversePath;
        }

        // TODO: a circular navigation from first to last (previousChild)? When toChildrenDirection wouldn't conflict

        if (firstChildPath == null)
        {
            // No children, restore defaults
            if (nextNavigatesToChildren)
            {
                // owner.FocusPrevious = focusPreviousControl;
                owner.FocusNext = focusNextControl;
            }

            owner.FocusNeighbourLeft = focusLeftControl;
            owner.FocusNeighbourRight = focusRightControl;
            owner.FocusNeighbourTop = focusUpControl;
            owner.FocusNeighbourBottom = focusDownControl;
        }
        else
        {
            // Navigation between last child-owner
            if (previousChild == null)
                throw new Exception("logic error in previous child update");

            switch (toChildrenDirection)
            {
                case NavigationToChildrenDirection.Horizontal:
                {
                    owner.FocusNeighbourRight = firstChildPath;

                    previousChild.FocusNeighbourRight = focusRightControl;

                    // TODO: should left also navigate to the children (also update Both case)
                    // owner.FocusNeighbourLeft = previousChildPath;
                    break;
                }

                case NavigationToChildrenDirection.Vertical:
                {
                    owner.FocusNeighbourBottom = firstChildPath;

                    previousChild.FocusNeighbourBottom = focusDownControl;
                    break;
                }

                case NavigationToChildrenDirection.Both:
                {
                    owner.FocusNeighbourRight = firstChildPath;
                    owner.FocusNeighbourBottom = firstChildPath;

                    previousChild.FocusNeighbourRight = focusRightControl;

                    previousChild.FocusNeighbourBottom = focusDownControl;
                    break;
                }

                case NavigationToChildrenDirection.None:
                {
                    if (inChildrenDirection is NavigationInChildrenDirection.Both
                        or NavigationInChildrenDirection.Horizontal)
                    {
                        previousChild.FocusNeighbourRight = focusRightControl;
                    }

                    if (inChildrenDirection is NavigationInChildrenDirection.Both
                        or NavigationInChildrenDirection.Vertical)
                    {
                        previousChild.FocusNeighbourBottom = focusDownControl;
                    }

                    break;
                }

                case NavigationToChildrenDirection.VerticalToChildrenOnly:
                {
                    owner.FocusNeighbourBottom = firstChildPath;

                    goto case NavigationToChildrenDirection.None;
                }

                default:
                    throw new ArgumentOutOfRangeException();
            }

            previousChild.FocusNext = focusNextControl;
        }
    }

    /// <summary>
    ///   Makes the first and last child set the focus neighbours to themselves based on
    ///   <see cref="NavigationInChildrenDirection"/>
    /// </summary>
    public void MakeFirstAndLastChildDeadEnds(IEnumerable<Control> children, bool trapNextAndPrevious = false)
    {
        Control? previousChild = null;

        var selfPath = new NodePath(".");

        foreach (var control in children)
        {
            if (previousChild == null)
            {
                // First control
                switch (inChildrenDirection)
                {
                    case NavigationInChildrenDirection.Horizontal:
                    {
                        control.FocusNeighbourLeft = selfPath;
                        break;
                    }

                    case NavigationInChildrenDirection.Vertical:
                    {
                        control.FocusNeighbourTop = selfPath;
                        break;
                    }

                    case NavigationInChildrenDirection.Both:
                    {
                        control.FocusNeighbourLeft = selfPath;
                        control.FocusNeighbourTop = selfPath;
                        break;
                    }

                    default:
                        throw new ArgumentOutOfRangeException();
                }

                if (trapNextAndPrevious)
                    control.FocusPrevious = selfPath;
            }

            previousChild = control;
        }

        if (previousChild != null)
        {
            // Apply last
            switch (inChildrenDirection)
            {
                case NavigationInChildrenDirection.Horizontal:
                {
                    previousChild.FocusNeighbourRight = selfPath;
                    break;
                }

                case NavigationInChildrenDirection.Vertical:
                {
                    previousChild.FocusNeighbourBottom = selfPath;
                    break;
                }

                case NavigationInChildrenDirection.Both:
                {
                    previousChild.FocusNeighbourRight = selfPath;
                    previousChild.FocusNeighbourBottom = selfPath;
                    break;
                }

                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (trapNextAndPrevious)
                previousChild.FocusNext = selfPath;
        }
    }

    /// <summary>
    ///   Refreshes the navigation directions of the owner control. Note that if <see cref="ApplyNavigationFlow"/>
    ///   has been called already, you need to be really sure before calling this that it makes sense.
    /// </summary>
    public void ReReadOwnerNeighbours()
    {
        focusPreviousControl = owner.ResolveToAbsolutePath(owner.FocusPrevious);
        focusNextControl = owner.ResolveToAbsolutePath(owner.FocusNext);

        focusLeftControl = owner.ResolveToAbsolutePath(owner.FocusNeighbourLeft);
        focusRightControl = owner.ResolveToAbsolutePath(owner.FocusNeighbourRight);
        focusUpControl = owner.ResolveToAbsolutePath(owner.FocusNeighbourTop);
        focusDownControl = owner.ResolveToAbsolutePath(owner.FocusNeighbourBottom);
    }
}
