# Third-Party Notices

## Art assets — Kenney.nl (CC0 1.0 / Public Domain)

`Assets/Art/Kenney/` contains sprites derived from Kenney.nl asset packs:
- Tiny Town (https://kenney.nl/assets/tiny-town)
- Roguelike/RPG Pack (https://kenney.nl/assets/roguelike-rpg-pack)
- Roguelike Characters (https://kenney.nl/assets/roguelike-characters)

CC0: free to use in personal, educational, and commercial projects. Crediting
Kenney is appreciated but not required. See
`Assets/Art/Kenney/LICENSE_ATTRIBUTION.txt` for the original notice.

## GossipNet (`com.gossipnet.ai-npc`)

This project consumes the GossipNet middleware as a Unity Package Manager
dependency (see `Packages/manifest.json`).
- License: MIT
- Source: https://github.com/projectpuppeteerai-cpu/gossipnet-ai-npc

## Newtonsoft.Json (via GossipNet's `com.unity.nuget.newtonsoft-json` dependency)

- License: MIT
- Distributed by Unity Technologies as a UPM wrapper around
  [Newtonsoft.Json](https://github.com/JamesNK/Newtonsoft.Json)
- License text: https://github.com/JamesNK/Newtonsoft.Json/blob/master/LICENSE.md

This project also calls out to third-party LLM APIs (via GossipNet) at runtime,
under credentials and terms of service that are the responsibility of whoever
runs the project — no API client SDKs or credentials are bundled here.
