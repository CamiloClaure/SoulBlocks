# SoulBlocks

A minimal Unity starting project extracted from Lighting Blocks.

The single scene contains the original main island, one wisdom tree, and the original player with its idle and locomotion animations. Camera and lighting provide the playable view. No other islands, HUD, enemies, collectibles, bridge building, progression, audio systems, or save systems are included.

## Open and play

1. Add this folder in Unity Hub and open it with **Unity 6000.6.0f1**.
2. Open `Assets/Scenes/SoulBlocks.unity` and press Play.
3. Move with **WASD** or **arrow keys**. Hold the **right mouse button** to orbit the camera; use the **mouse wheel** to zoom.

The player retains the source project's locomotion clip (originally named Run), now used as the Walk state. Movement stops at the island edge.

Runtime scripts are in `Assets/SoulBlocks/Runtime`. Retained art remains under its original asset paths to preserve Unity GUIDs. The editor validation checks scene contents, movement, animation transitions, foot motion, and island boundaries. Its latest results and preview are in `Validation/`.

Source: CamiloClaure/Lighting-Blocks. This repository has its own Git history and project identity. Source assets retain their original ownership and usage terms.
