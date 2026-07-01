# Mouse Hand IK Target Controller

`MouseHandIKTargetController` is used to move a hand IK target with the mouse.

## Setup

1. Add the script to the character or a nearby controller object.
2. Bind `Target Camera`.
3. Bind `Reach Origin` to the shoulder or upper arm root.
4. Bind `Hand Target` to the IK target object used by the hand constraint.
5. Choose the mouse hit mode:
   - `PhysicsRaycast`: mouse ray must hit a Collider in `Hit Mask`.
   - `Plane`: mouse ray is projected onto a plane through `Reach Origin`.

## Suggested Values

- `Max Reach`: the longest distance the hand target can move away from the shoulder.
- `Follow Speed`: higher values make the target follow the mouse faster.
- `Plane Normal`: use `(0, 0, 1)` for XY side-view movement, or `(0, 1, 0)` for ground-plane movement.
