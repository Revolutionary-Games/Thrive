Updating Godot Version
======================

When updating to a newer version of Godot the version number needs
to be changed in the following files:

- `doc/setup_instructions.md`
- `Scripts/GodotVersion.cs`
- `podman/ci/Dockerfile`

The CI system needs to also be updated (`CIConfiguration.yml`). That
can only be done by Thrive team members. Instructions for that can be 
found here: https://wiki.revolutionarygamesstudio.com/wiki/CI_Images

