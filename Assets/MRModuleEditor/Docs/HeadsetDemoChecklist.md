# Headset Demo Checklist

Use this checklist before every headset demo.

## Before build

[ ] Open the project and confirm there are no compile errors.
[ ] Run Edit Mode tests if time allows.
[ ] Run Play Mode tests if time allows.
[ ] Confirm sample media files exist:
    Assets/MRModuleEditor/Samples/SampleModules/ForwardKinematicsMini/assets/images/intro.png
    Assets/MRModuleEditor/Samples/SampleModules/ForwardKinematicsMini/assets/audio/intro.mp3
[ ] Create placeholder intro image if intro.png is missing:
    MR Module Editor > Temporary > Create Placeholder Intro Image
[ ] Export the module to StreamingAssets:
    MR Module Editor > Export > Export Current Module To StreamingAssets
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

## Export sanity check

[ ] Export completed without missing-file warnings, or you intentionally accepted them.
[ ] External export folder contains module.json.
[ ] External export folder contains assets/images and assets/audio if referenced.
[ ] External export folder does not contain copied source .meta files.
[ ] StreamingAssets path shown by the export dialog matches RuntimeModuleLoader.

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

- If module does not load, check the StreamingAssets export path shown by the export dialog.
- If image/audio fails, check that the referenced files exist in the source module folder before export.
- If robot does not appear, check BindableObject bindingKey RobotPreview.
- If panels are too far, check image/MCQ z offsets and AnchorResolver.headDistance.
- If MCQ text overlaps, check panel size, choice height, wrap characters, and sorting order.
- If world starts behind/sideways, press Recenter instead of restarting first.