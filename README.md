# RedOnion and Kerbalua

A joint effort of Evan Dickinson and Lukáš Fireš to create
unrestricted scripted environment inside Kerbal Space Program
for all players and even modders wishing to control the game,
vessels, anything, with own script.

**Warning: Do not use scripts from untrusted sources!**
This is not a sandbox, any script has the power to do almost anything.
We plan to eventually implement a switch to limit the power
(disable what is marked `[Unsafe]` in the code),
but our goal now is to:

- Allow users to do whatever they wish to do to the game environment to have fun.
- Help modders develop and debug their mods using this mod.
- Help anybody to explore KSP API, their own or other's mods
  (read the license of each mod, we expose only `public` members directly).


## Scripting Languages
We have two scripting languages, [**Kerbalua**](Kerbalua/README.md), and [**ROS** (Red Onion Script)](RedOnion.ROS/README.md). **Kerbalua** is a Lua implementation, while **ROS** is a custom language. **ROS** is designed to make programming easier by requiring very little syntax (examples: python-like indentation to mark program structure, and function calls/definitions without parenthesis).

Scripts are currently stored in GameData/RedOnion/Scripts,
our own scripts are packed inside GameData/RedOnion/Scripts.zip.
You can override our scripts simply by opening them in REPL
and saving the modified version (which will become a file outside of the zip).

## Documentation

[Scripting](ScriptingReadme.md) - Documentation related to writing scripts.

[Troubleshooting](TroubleShooting.md) - Some possible issues and solutions

[Development](DevelopmentReadme.md) - Main page for development documentation (project structure, implementation explanations, etc).

## Videos

Demonstration videos for this mod are on [this channel](https://www.youtube.com/channel/UChduoYTVOtAH0NA-Lj8EiKA).

## Releases

Releases are hosted at [spacedock](https://spacedock.info/mod/2116/Red%20Onion) and [curseforge](https://kerbal.curseforge.com/projects/redonion).

## Upcoming Features

[Next Release](ChangeLog.md#next-release)

[Planned](ChangeLog.md#planned-features)

## Feedback

Feedback can be left on our forum [thread](https://forum.kerbalspaceprogram.com/index.php?/topic/183050-wip-redonion-020-unrestricted-in-game-scripting-has-repl-editor-and-intellisense-lua-and-a-custom-jsruby-like-language-implemented-tested-on-ksp-161/), on this repository as a new issue, or as a comment on any of our videos.

## Contributing

We welcome people of all skill levels to contribute or give feedback, ask questions, etc.

Here is more information about [contributing](Contributing.md).
