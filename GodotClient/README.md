# Godot Client Shell

This folder is reserved for the presentation client.

Rules:

- Godot is presentation only.
- Godot may read snapshots.
- Godot must not mutate `GameState`.
- Godot must not contain gameplay authority.
- Godot physics and delta time must not affect simulation.
- Headless simulation, replay, lockstep, and chaos tests remain authoritative.
