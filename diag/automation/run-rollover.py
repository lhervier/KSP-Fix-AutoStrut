"""Plays "The rollover protocol": rolls a rover over until it lands back on its wheels with a part broken off,
then shows whether it turns on the spot.

It drives KSP through KSP-MCPServer, a mod that answers HTTP requests on 127.0.0.1, and needs nothing but
Python 3: no AI, no package to install. Start KSP with KSP-MCPServer installed, copy rollover-kerbin.sfs into
a sandbox game, wait for the main menu, then run:

    python run-rollover.py --folder <your sandbox game> --runs 3 --out out

Each try loads the save, drives the rover straight ahead at full throttle up to 15 m/s, steers left for two
seconds, then lets go of the controls with the brakes on. A try counts when the rover ends on its wheels with
fewer parts than it started with; any other try (on its roof, on its side, nothing broken off) is dropped and
played again. For each run that counts, it waits ten seconds, takes a screenshot, waits thirty seconds more
and takes another: the heading on the navball tells whether the rover turned in between. It prints what it
did and writes it to runs.json. KSP is left running at the end.
"""
import argparse
import json
import os
import time
import urllib.request

URL = None


def call(tool, **args):
    """Calls one tool of KSP-MCPServer and returns its answer, decoded from JSON when it is JSON."""
    body = json.dumps({"jsonrpc": "2.0", "id": 1, "method": "tools/call",
                       "params": {"name": tool, "arguments": args}}).encode()
    request = urllib.request.Request(URL, body, {"Content-Type": "application/json"})
    with urllib.request.urlopen(request, timeout=900) as response:
        result = json.loads(response.read())["result"]
    text = result["content"][0].get("text", "") if result["content"] else ""
    if result.get("isError"):
        raise RuntimeError(tool + ": " + text)
    try:
        return json.loads(text)
    except ValueError:
        return text


def member(path):
    """A member of the active vessel, as KSP-MCPServer reads it; used only to sort the tries."""
    return call("get_member", type="FlightGlobals", member="ActiveVessel." + path)["value"]


def log(*parts):
    print(time.strftime("%H:%M:%S"), *parts, flush=True)


def roll(options):
    """One rollover. Returns the number of parts before and after, and whether every wheel touches the
    ground."""
    call("load_save", folder=options.folder, save=options.save)
    before = len(member("parts"))
    call("set_controls", wheel_throttle=1.0, wheel_steer=0.0, brakes=False)
    start = time.time()
    while time.time() - start < 60 and call("get_state")["vessel"]["surfaceSpeed"] < options.speed:
        time.sleep(0.1)
    call("set_controls", wheel_throttle=1.0, wheel_steer=options.steer)
    time.sleep(options.steer_seconds)
    # What a player does after a crash: hands off the controls, brakes on.
    call("set_controls", wheel_throttle=0.0, wheel_steer=0.0, brakes=True)
    time.sleep(options.settle)
    parts = member("parts")
    wheels = [i for i, name in enumerate(parts) if name.startswith("wheelMed")]
    grounded = all(member("parts.%d.[ModuleWheels.ModuleWheelSteering].0.wheel.currentState.grounded" % i)
                   for i in wheels)
    return before, len(parts), grounded


def main():
    global URL
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("--folder", required=True, help="the sandbox game under saves/ the save was copied into")
    parser.add_argument("--save", default="rollover-kerbin", help="the save, without .sfs")
    parser.add_argument("--runs", type=int, default=3, help="how many runs that count")
    parser.add_argument("--tries", type=int, default=20, help="how many tries at most")
    parser.add_argument("--speed", type=float, default=15.0, help="the speed to steer at, in m/s")
    parser.add_argument("--steer", type=float, default=0.4, help="the steering, 0 to 1, to the left")
    parser.add_argument("--steer-seconds", type=float, default=2.0, help="how long to steer")
    parser.add_argument("--settle", type=float, default=10.0, help="seconds to wait before the first screenshot")
    parser.add_argument("--watch", type=float, default=30.0, help="seconds between the two screenshots")
    parser.add_argument("--out", default="out", help="where runs.json and the screenshots go")
    parser.add_argument("--port", type=int, default=8770, help="the port of KSP-MCPServer")
    options = parser.parse_args()
    URL = "http://127.0.0.1:%d/mcp/" % options.port
    os.makedirs(options.out, exist_ok=True)

    if call("get_state")["scene"] == "MAINMENU":
        call("open_game", folder=options.folder)
    runs = []
    tries = 0
    while len(runs) < options.runs and tries < options.tries:
        tries += 1
        before, after, grounded = roll(options)
        if not grounded or after >= before:
            log("try %d dropped: %d parts -> %d, on its wheels: %s" % (tries, before, after, grounded))
            continue
        name = "run-%d" % (len(runs) + 1)
        first = call("get_state")["vessel"]["heading"]
        call("screenshot", path=os.path.join(os.path.abspath(options.out), name + "-a.png"), return_image=False)
        time.sleep(options.watch)
        second = call("get_state")["vessel"]["heading"]
        call("screenshot", path=os.path.join(os.path.abspath(options.out), name + "-b.png"), return_image=False)
        turned = (second - first + 540.0) % 360.0 - 180.0
        log("%s (try %d): %d parts -> %d, heading %.1f then %.1f, %.1f degrees in %.0f s"
            % (name, tries, before, after, first, second, turned, options.watch))
        runs.append({"run": name, "try": tries, "partsBefore": before, "partsAfter": after,
                     "headingFirst": first, "headingSecond": second, "seconds": options.watch})
    with open(os.path.join(options.out, "runs.json"), "w", newline="\n") as f:
        json.dump({"tries": tries, "runs": runs}, f, indent=2)
    log("%d runs out of %d tries" % (len(runs), tries))


main()
