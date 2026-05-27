# MVP Baseline Report

Generated UTC: 2026-05-27 07:38:14Z
Unity version: 2022.3.62f1c1

## Asset checks
- Sample module: `Assets/MRModuleEditor/Samples/SampleModules/ForwardKinematicsMini/module.json` — found
- Runtime preview scene: `Assets/MRModuleEditor/Samples/Scenes/RuntimePreview.unity` — found
- Headset preview scene: `Assets/MRModuleEditor/Samples/Scenes/RuntimePreview_Headset.unity` — found

## Sample module summary
- Module ID: `module.forward_kinematics_mini`
- Title: Forward Kinematics Mini Demo
- Schema version: `0.1`
- Assets: 2
- Objects: 1
- Anchors: 3
- Layouts: 9
- Steps: 14
- Validation: 0 error(s), 0 warning(s)

## Step inventory

| # | ID | Type | Title |
|---:|---|---|---|
| 0 | `step.001` | `text` | Welcome |
| 1 | `step.001.audio` | `audio` | Intro Narration |
| 2 | `step.002` | `image` | Module Overview |
| 3 | `step.003` | `wait` | Pause |
| 4 | `step.004` | `showObject` | Show Robot |
| 5 | `step.005` | `moveObject` | Move Robot Preview |
| 6 | `step.006` | `text` | Robot Callout |
| 7 | `step.007` | `showFrame` | Show End-Effector Frame |
| 8 | `step.008` | `rotateJoint` | Rotate Joint 3 |
| 9 | `step.009` | `text` | Forward Kinematics Idea |
| 10 | `step.010` | `mcq` | Quick Check |
| 11 | `step.011` | `text` | Short Review |
| 12 | `step.012` | `text` | Summary |
| 13 | `step.013` | `resetRobot` | Reset Robot to Home |

