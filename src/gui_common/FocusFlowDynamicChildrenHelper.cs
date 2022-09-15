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

    // Currently there's no case where the previous control is changed
    // private readonly NodePath? focusPreviousControl;
    private readonly NodePath? focusNextControl;

    private readonly NodePath? focusLeftControl;
    private readonly NodePath? focusRightControl;
    private readonly NodePath? focusUpControl;
    private readonly NodePath? focusDownControl;

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

        // focusPreviousControl = owner.FocusPrevious;
        focusNextControl = owner.ResolveToAbsolutePath(owner.FocusNext);

        focusLeftControl = owner.ResolveToAbsolutePath(owner.FocusNeighbourLeft);
        focusRightControl = owner.ResolveToAbsolutePath(owner.FocusNeighbourRight);
        focusUpControl = owner.ResolveToAbsolutePath(owner.FocusNeighbourTop);
        focusDownControl = owner.ResolveToAbsolutePath(owner.FocusNeighbourBottom);
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
    }

    /// <summary>
    ///   Applies the navigation neighbours to owner and between children based on the configuration
    /// </summary>
    /// <param name="children">The child nodes. When empty the default navigation is restored</param>
    public void ApplyNavigationFlow(IEnumerable<Control> children)
    {
        var ownerPath = owner.GetPath();

        NodePath? firstChildPath = null;
        Control? previousChild = null;
        NodePath? previousChildPath = null;

        foreach (var currentChild in children)
        {
            var currentPath = currentChild.GetPath();

            if (firstChildPath == null)
            {
                firstChildPath = currentPath;

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

                    default:
                        throw new ArgumentOutOfRangeException();
                }

                if (nextNavigatesToChildren)
                {
                    owner.FocusNext = firstChildPath;
                    currentChild.FocusPrevious = ownerPath;
                }
            }

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
                currentChild.FocusPrevious = previousChildPath;
            }

            previousChild = currentChild;
            previousChildPath = currentPath;
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
            if (previousChild == null)
                throw new Exception("logic error in previous child update");

            switch (toChildrenDirection)
            {
                case NavigationToChildrenDirection.Horizontal:
                {
                    previousChild.FocusNeighbourRight = focusRightControl;

                    // TODO: should left also navigate to the children (also update Both case)
                    // owner.FocusNeighbourLeft = previousChildPath;
                    break;
                }

                case NavigationToChildrenDirection.Vertical:
                {
                    previousChild.FocusNeighbourBottom = focusDownControl;
                    break;
                }

                case NavigationToChildrenDirection.Both:
                {
                    previousChild.FocusNeighbourRight = focusRightControl;

                    previousChild.FocusNeighbourBottom = focusDownControl;
                    break;
                }

                default:
                    throw new ArgumentOutOfRangeException();
            }

            previousChild.FocusNext = focusNextControl;
        }
    }
}
