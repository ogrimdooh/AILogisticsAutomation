# AI Logistics Automation

This mod adds blocks that allow you to configure Inventory Management and Production Queues without the need for scripts.

# Complete information

I set up a discord to keep discussions, roadmap information and bug reporting.
https://discord.gg/rTd2P6pbEu

# Local Development Setup

When working with the project locally, you must import all of the assemblies provided by Space Engineers to be able to compile the project.

By default, the project will attempt to load these files from `F:\SteamLibrary\steamapps\common\SpaceEngineers`, it's possible your installation
is not located on this path.

This can be resolved by doing the following:
- Copy the file `user.props.template` and name it `user.props`
- Open the file and modify the `SERootDirectory` to your Space Engineers installation directory
- The warning symbol on the assemblies should now go away