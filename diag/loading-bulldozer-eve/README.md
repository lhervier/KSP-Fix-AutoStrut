# Loading a save: a heavy rover on Eve (work in progress)

A save where the autostruts of a rover come out bent as soon as it is loaded, with no crash and no part lost.
It is a work in progress: one rover, one place, measured by hand, not yet a checked campaign.

## The save

`bulldozer-eve-amort005.sfs.gz`: Lionel's Bulldozer (about 14 t, six TR-2L wheels), parked on a gentle slope on
Eve, brakes on. Its wheels have the friction and damper set by hand. It needs the blade of
[KSP-Mod-Bulldozer](https://github.com/lhervier/KSP-Mod-Bulldozer) to load; nothing else beyond stock.

## What you see

Without this mod, right after the load, the parked rover slides down the slope with its brakes on, at a steady
2 cm/s or so, and never stops. With its brakes released, it turns on itself from the start. With this mod, in
either of its modes, it stays where it is, brakes on.

## How to check it

1. Unzip the save into a game folder of yours and load it.
2. Leave it untouched, brakes on, and watch its latitude and longitude for half a minute.
3. Do it again with this mod installed.

The rover's wheels read a constant sideways speed that does not match any motion of the rover: the part
holding each wheel reports a velocity it does not have, as described in [the culprit](../../docs/the-culprit.md),
and the tyre pushes against it.
