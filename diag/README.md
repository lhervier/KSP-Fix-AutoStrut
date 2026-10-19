# Measurement files

What the protocols of [Auto Strut Fix](../README.md) use and produce: their saves, the script that plays
them, and what each series wrote.

## The rollover protocol

Described in [Checking the culprit: a rollover](../docs/checking-the-culprit-rollover.md).

- [checking-the-culprit-rollover/rollover-kerbin.sfs](checking-the-culprit-rollover/rollover-kerbin.sfs):
  the save. Copy it into the folder of a sandbox game, and load it.
- [automation/run-rollover.py](automation/run-rollover.py): the script that plays it through
  [KSP-MCPServer](https://github.com/lhervier/KSP-MCPServer). Python 3 only, nothing to install.
- `checking-the-culprit-rollover/stock/`, `fix/`, `twist-limit-kspcf/` and `twist-limit/`: for each series,
  on stock, with this mod in its default `OriginalPose` mode, in its `TwistLimitKSPCF` mode and in its
  `TwistLimit` mode, `runs.json` (what the script printed) and `KSP.log`. The screenshots are in
  [../imgs/checking-the-culprit-rollover/](../imgs/checking-the-culprit-rollover/).
