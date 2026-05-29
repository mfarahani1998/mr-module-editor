# MR Module Editor Documentation

MR Module Editor is a Unity-based prototype for authoring and running data-driven mixed-reality learning modules.

The current baseline supports:

- JSON-backed module documents.
- Unity Editor authoring through `MR Module Editor > Module Editor`.
- Runtime preview scenes.
- Head, world, and object anchors.
- Text, image, audio, wait, confirm, variable, object visibility/highlight/callout/movement, MCQ, and RoboticsLite demo steps.
- Validation, export, and a sample Forward Kinematics mini module.

## Start here

1. [Quickstart](QUICKSTART.md)
2. [Demo Runbook](DEMO_RUNBOOK.md)
3. [Architecture](ARCHITECTURE.md)
4. [Module Schema](MODULE_SCHEMA.md)
5. [Authoring Guide](AUTHORING_GUIDE.md)
6. [Runtime Scene Setup](RUNTIME_SCENE_SETUP.md)
7. [Testing](TESTING.md)
8. [Troubleshooting](TROUBLESHOOTING.md)
9. [Known Limitations](KNOWN_LIMITATIONS.md)
10. [Roadmap](ROADMAP.md)

## Current baseline sample

- Module JSON: `Assets/MRModuleEditor/Samples/SampleModules/ForwardKinematicsMini/module.json`
- Desktop/simulator preview scene: `Assets/MRModuleEditor/Samples/Scenes/RuntimePreview.unity`
- Headset preview scene: `Assets/MRModuleEditor/Samples/Scenes/RuntimePreview_Headset.unity`

## Important constraint

The next expansion should not make the platform robotics-centered. `Domains/RoboticsLite` is a contained sample/domain, not the long-term center of the platform.

## Current extension warning

Generic step metadata is centralized in the Step Catalog so the editor add menu, inspector defaults, and generic validation stay aligned with runtime handlers.

