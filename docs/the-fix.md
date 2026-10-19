# The fix: autostruts set up on the original layout

Part of [Auto Strut Fix](../README.md). How this mod makes every autostrut built in flight hold the
vessel's original shape, why it does it that way by default, and the other ways it offers.

## Three modes

The mod has three modes, one or the other, set by `mode` in `PluginData/settings.cfg` and read when KSP
starts:

- **`OriginalPose`, the default.** The mod sets every autostrut up on the vessel's original shape. The
  stock joint is left exactly as it is, limits and springs included, and the `AutoStrutDrift` patch of
  [KSP Community Fixes](https://github.com/KSPModdingLibs/KSPCommunityFixes), which this replaces, is turned
  off.
- **`TwistLimitKSPCF`.** The mod gives the twist of the autostruts `AutoStrutDrift` handles a proper range,
  and leaves `AutoStrutDrift` on: `AutoStrutDrift` then brings the parts back by itself. This changes the
  game: the twist of those autostruts is then held by a spring, as their bending is, where stock holds it
  firmly (see [The culprit](the-culprit.md#what-fixing-the-limit-would-change)).
- **`TwistLimit`.** The same, for every part joint KSP sets up: attach joints, and every autostrut, those
  `AutoStrutDrift` leaves alone included. This changes the game more: every joint of every vessel then
  twists as easily as it bends.

|   | `OriginalPose` | `TwistLimitKSPCF` | `TwistLimit` |
|---|---|---|---|
| What it does | sets each new autostrut up on the original layout | fixes the twist limit of the autostruts `AutoStrutDrift` handles | fixes the twist limit of every part joint |
| `AutoStrutDrift` | turned off: this mode does its job | left on: it brings the parts back | left on: it brings the parts back |
| Stiffness | stock: twist held firmly, as KSP does it | the twist of those autostruts gives like a spring, as their bending does | the twist of every joint gives like a spring, as its bending does |
| Without KSP Community Fixes | works the same | brings nothing back | brings nothing back |
| Autostruts through a robotic part | left as stock builds them | left as stock builds them | twist limit fixed, but nothing brings them back |
| Rollover protocol | no rover turned | no rover turned | no rover turned |
| Upside | the game plays as stock, and needs nothing else | the smallest change that makes `AutoStrutDrift` work | the stock slip fixed everywhere it is made |
| Downside | moves the part for an instant at each new autostrut (no physics step runs meanwhile) | changes how stiff those autostruts are | changes how stiff every vessel is, where every craft so far was built with joints whose twist does not give |

The two twist limit modes are the other ways out set out in
[The culprit](the-culprit.md#three-ways-out): they are there to show that the computation of
`AutoStrutDrift` works once the limit lets it, for the maintainers of KSP Community Fixes to try in game and
judge. Without KSP Community Fixes, they bring nothing back to the original shape. All three are checked the
same way, in [Checking the culprit: a rollover](checking-the-culprit-rollover.md).

## How OriginalPose works

The mod is one Harmony postfix on `Part.SecureAutoStrut`, the stock method that builds an autostrut, in
[`Src/AutoStrutPose.cs`](../Src/AutoStrutPose.cs). For each new autostrut:

1. it works out where the vessel's original layout (`Part.orgPos`, `Part.orgRot`) puts the part relative to
   the strut's anchor, the part the strut goes to;
2. it puts the part there, for that instant only: its transform, its rigidbody, and
   `Physics.SyncTransforms` so that the physics engine knows at once;
3. it sets the joint up again (its connected body, then its axes), so that the joint takes that pose as
   the one it brings the parts back to, with its anchors on the original layout and its target left
   neutral;
4. it puts the part back exactly where it was.

Nothing moves between steps 2 and 4: no physics step runs in between. The part then comes back to its
original pose over the next steps, pulled by its joints, which now agree.

## How TwistLimitKSPCF works

The same postfix, in the same place, only sets the lower twist limit of the new autostrut to the negative
of its upper limit: [−177°, +177°] instead of the [+177°, +177°] `PartJoint.SetJointLimits` leaves. It runs
after the postfix of `AutoStrutDrift`, which has already moved the joint's anchors to the original layout
and turned its target by the difference; with a proper twist range, that target now acts on all three axes.
Like `AutoStrutDrift`, it leaves alone an autostrut that goes through a robotic part.

## How TwistLimit works

A second postfix, on `PartJoint.SetJointLimits` itself, in
[`Src/JointTwistLimit.cs`](../Src/JointTwistLimit.cs), does the same for every part joint KSP sets up, as
it sets it up: attach joints, autostruts, robotic parts' autostruts included. A rigid attachment, whose
motions are all locked, is left as it is. The joints that robotic parts and docking ports set up for their
own motion have limits of their own, and are not touched.

## Why OriginalPose is the default

`AutoStrutDrift` leaves the joint set up on the bent pose and turns its target, `targetRotation`, by the
difference, and its computation is the right one: the twist limit modes show it. What stops it in stock is
the twist limit of the joints, which holds the twist where the joint was set up; fixing that limit makes
every joint it is fixed for twist as easily as it bends. Setting the joint up on the right pose needs no
target at all, and keeps the stock joint exactly as it is: every craft built so far was built with joints
whose twist does not give.

## What it leaves alone

- Outside flight, nothing: the editor builds its own autostruts on the original layout already.
- In `OriginalPose` and `TwistLimitKSPCF` modes, an autostrut whose joint does not join the two parts' own
  rigidbodies, as when a robotic part stands in between, is left as stock builds it, as `AutoStrutDrift`
  does.
- Nothing is saved: joints are built again at every load. Removing the mod gives the stock behaviour back
  at the next load, `AutoStrutDrift` included.

## With KSP Community Fixes

In `OriginalPose` mode, `AutoStrutDrift` and this mod would do the same job, on the same autostruts, and
skip the same ones; `AutoStrutDrift` would run first and this mod would then rewrite everything it set up:
its anchors, its axes, its target. It has nothing left to do, so the mod takes it off. KSP Community Fixes
applies its patches once ModuleManager is done; when the main menu shows, before any flight, the mod
removes the postfix of `AutoStrutDrift` from `Part.SecureAutoStrut` (it is the only patch KSP Community
Fixes puts there), and says so in `KSP.log`:

```
[KSPCommunityFixes] Patch AutoStrutDrift applied.
[AutoStrutFix] Mode OriginalPose: AutoStrutDrift of KSP Community Fixes turned off
```

In the two twist limit modes, it leaves it on, and says so too:

```
[AutoStrutFix] Mode TwistLimitKSPCF: AutoStrutDrift of KSP Community Fixes left on
[AutoStrutFix] Mode TwistLimit: AutoStrutDrift of KSP Community Fixes left on
```

Without KSP Community Fixes, `OriginalPose` works the same: it sets the joint's anchors and target itself.
The twist limit modes then have nothing to make work, and the mod warns about it in `KSP.log`. The mod is
built and measured with KSP Community Fixes installed, the base most players run.
