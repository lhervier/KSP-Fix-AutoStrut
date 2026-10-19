# The culprit: autostruts rebuilt on a bent vessel

Part of [Auto Strut Fix](../README.md). What happens in the stock code, in KSP 1.12.5, when a vessel's
autostruts are rebuilt while the vessel is bent, and what stops the patch of
[KSP Community Fixes](https://github.com/KSPModdingLibs/KSPCommunityFixes) for this kind of trouble from
covering this case.

## When KSP rebuilds the autostruts

An autostrut is a joint KSP adds between a part and another part further away on the same vessel: the
heaviest part, the root, or the grandparent, as set in the part's menu. Wheels and landing legs have one by
default, to the heaviest part.

KSP throws these joints away and builds them again every time the vessel changes: a part breaking off, a
stage decoupling, a docking or an undocking, the vessel going off rails, a part added or removed in EVA
construction. Each part then calls `Part.CycleAutoStrut`, which removes its autostruts at once, and starts
`Part.SecureAutoStruts`, which builds the new ones on the next physics step:

```csharp
// Part.SecureAutoStruts, KSP 1.12.5
yield return new WaitForFixedUpdate();
...
autoStrutJoints.Add(SecureAutoStrut(autoStrutAnchor, nodeToParent, nodeFromParent, srfAttached));
```

## The pose an autostrut holds

`Part.SecureAutoStrut` builds the joint through `PartJoint.Create`, which adds a Unity `ConfigurableJoint`
between the two parts as they are at that instant. A `ConfigurableJoint` takes the relative pose its two
bodies have when it is set up as the pose it brings them back to. Nothing on that path looks at the
vessel's original layout, the positions and rotations the parts were given in the editor (`Part.orgPos`,
`Part.orgRot`): the new autostrut holds the parts where the physics happens to have them.

On a vessel at rest, that is the original layout, give or take how the vessel sags under its own weight.
On a vessel in the middle of a rollover, it is not: the joints between the parts bend under the shocks,
and a part breaking off at that moment has every autostrut of the vessel rebuilt on that bent shape.

## Two joints that cannot agree

A wheel is then held by two joints: its attach joint, made when the vessel was loaded, which brings it back
to the original layout, and its new autostrut, which brings it back to the bent one. Once the rover is back
on its wheels and settles, no pose satisfies both. The physics engine keeps correcting the wheel at every
step, and the wheel never settles.

The wheels of KSP compute how they slide on the ground from the speed of the wheel's rigidbody at the
contact point, and push against that sliding:

```csharp
// VehiclePhysics.VehicleBase.CalculateLocalFrame, KSP 1.12.5
Vector2 impulseForce = (0f - tireImpulseRatio) * supportedMass * wheel.localWheelVelocity / Time.deltaTime;
```

A wheel that is corrected at every step without going anywhere is, for this computation, a wheel that
slides. The tyres push, the joints pass the pushes on to the rest of the rover, and what is left of them
turns the rover on the spot: brakes on, hands off the controls, for as long as the save is not reloaded.
Reloading builds every joint again on the same shape, and the rover stops.

## KSP Community Fixes and its AutoStrutDrift

### What AutoStrutDrift does

KSP Community Fixes has a patch for autostruts rebuilt after a change to the vessel, `AutoStrutDrift`: a
postfix of `Part.SecureAutoStrut`, like this mod. It moves the anchors of the new joint to the vessel's
original layout, then turns the joint's target, `targetRotation`, by the difference between the rotation the
part has at that instant and the one the original layout gives it. The joint was set up on the bent pose;
its target says how far to turn from there to get back to the original one.

That is a sound way to do it. Nothing is wrong in its computation: the target it works out is the one that
brings the part back to the original layout, on all three axes, for a joint that is free to turn on all
three axes towards its target. The two twist limit modes of this mod show it in game (see
[Three ways out](#three-ways-out)).

### What stops it: the twist limit of stock joints

The stock joints are not free to. `PartJoint.SetJointLimits` gives the twist of a joint (its rotation about
its main axis) the same value as its upper and its lower limit:

```csharp
// PartJoint.SetJointLimits, KSP 1.12.5, for a joint that is not a rigid attachment
angularLimit.limit = (rigidAttach ? 0f : 180f);
...
newJoint.highAngularXLimit = angularLimit;
newJoint.lowAngularXLimit = angularLimit;
```

Unity expects a negative lower limit: with +180 for both (clamped to 177 by Unity), the twist range is
[+177°, +177°] instead of [−177°, +177°]. The twist then stays where it was when the joint was set up,
whatever `targetRotation` asks. Stock never notices, since its joints always aim at the pose they were set
up in. `AutoStrutDrift` does: it brings the part back on the two other axes, but the twist it asks for is
never applied, the autostrut keeps part of the bent pose, and the rover still turns.

It looks like a slip rather than a choice: the same method locks every axis (`Locked`) when it means to,
for a rigid attachment, and sets a twist spring of its own (`angularXDrive`), which the limit leaves with
nothing to do.

### What fixing the limit would change

A lower limit of −180 is what would make `AutoStrutDrift` work as written. But it is not a small change.
Today the limit holds the twist of every joint of a craft, firmly. With a proper range, only the twist spring
would, and `PartJoint.SetJointLimits` gives it the same stiffness as the bending springs (`angXDrive` and
`angYZDrive` both take `stiffness`): the joints would twist as easily as they bend. Every craft built so far
was built, tuned and flown with joints whose twist is held by the limit.

### Three ways out

- **Fix the lower limit of every joint**, in `PartJoint.SetJointLimits`: the stock code then does what it
  seems to mean, `AutoStrutDrift` works, and every joint of every craft twists as much as it bends. This mod
  can play it, in its `TwistLimit` mode (see
  [Checking the culprit: a rollover](checking-the-culprit-rollover.md#with-this-mod)).
- **Fix it for the autostruts `AutoStrutDrift` rebuilds only**: `AutoStrutDrift` works, the other joints stay
  as they are, and those autostruts twist as much as they bend. This mod can play it, in its
  `TwistLimitKSPCF` mode (see
  [Checking the culprit: a rollover](checking-the-culprit-rollover.md#with-this-mod)).
- **Set the rebuilt autostrut up on the original pose**, so that its target needs no turning at all: the
  joint stays exactly as stock makes it, limit included. That is what this mod does by default, in its
  `OriginalPose` mode (see [The fix](the-fix.md)).

In both twist limit modes, with `AutoStrutDrift` on, the rovers of the rollover protocol no longer turn.
Which of these ways belongs in KSP Community Fixes is for its maintainers to judge, and the three modes of
this mod are there to let them try each in game. By default it takes the third, and, since it then rewrites
everything `AutoStrutDrift` sets up, turns `AutoStrutDrift` off (see
[The fix](the-fix.md#with-ksp-community-fixes)).
