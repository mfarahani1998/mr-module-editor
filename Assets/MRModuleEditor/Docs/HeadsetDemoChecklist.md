# Headset Demo Checklist

Use this checklist before every headset demo.

## Before build

[ ] Open the project and confirm there are no compile errors.
[ ] Run Edit Mode tests if time allows.
[ ] Run Play Mode tests if time allows.
[ ] Create placeholder intro image if missing:
    MR Module Editor > Phase F > Create Placeholder Intro Image
[ ] Copy FK sample to StreamingAssets:
    MR Module Editor > Phase F > Copy FK Sample To StreamingAssets
[ ] Confirm this folder exists:
    Assets/StreamingAssets/MRModuleEditor/SampleModules/ForwardKinematicsMini
[ ] Confirm module JSON exists in that folder.
[ ] Open RuntimePreview_Headset scene.
[ ] Confirm RuntimeModuleLoader Load Mode is StreamingAssetsRelative.
[ ] Confirm StreamingAssets path is:
    MRModuleEditor/SampleModules/ForwardKinematicsMini/module.json
[ ] Confirm RuntimeBuildAutoStarter is enabled.
[ ] Confirm RuntimeBuildAutoStarter Play After Load is enabled.
[ ] Confirm RuntimeBuildAutoStarter Recenter World Before Play is enabled.
[ ] Confirm RuntimeServices has SimulatorRecenterController.
[ ] Confirm RuntimeServices has AnchorResolver.
[ ] Confirm AnchorResolver Head Distance is intentionally chosen.
[ ] Confirm SpatialImagePanel Default Local Offset z is 0.
[ ] Confirm SpatialMCQPanel Default Local Offset z is 0.
[ ] Confirm RobotLiteArm has BindableObject with bindingKey RobotPreview.

## On headset

[ ] Launch app.
[ ] Wait for headset tracking to stabilize.
[ ] Trigger manual Recenter.
[ ] Verify robot/world anchor appears in a comfortable forward area.
[ ] Verify text step is readable.
[ ] Verify image step is readable.
[ ] Verify MCQ panel is readable.
[ ] Verify choices do not overlap.
[ ] Verify keyboard/gaze answer selection works as expected.
[ ] Run the module from start to MCQ.

## If it fails

- If module does not load, check StreamingAssets copy.
- If image is missing, create placeholder intro image and copy again.
- If robot does not appear, check BindableObject bindingKey RobotPreview.
- If panels are too far, check image/MCQ z offsets and AnchorResolver.headDistance.
- If MCQ text overlaps, check panel size, choice height, wrap characters, and sorting order.
- If world starts behind/sideways, press Recenter instead of restarting first.