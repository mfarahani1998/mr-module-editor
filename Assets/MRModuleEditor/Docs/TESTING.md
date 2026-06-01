# Testing Guide

The project has EditMode and PlayMode tests under:

```text
Assets/MRModuleEditor/Tests/EditMode
Assets/MRModuleEditor/Tests/PlayMode
```

Domain packs may also keep their own tests next to the domain code. For example, Phase 6 adds:

```text
Assets/MRModuleEditor/Domains/ProcedureTraining/Tests/EditMode
Assets/MRModuleEditor/Domains/ProcedureTraining/Tests/PlayMode
```

## Run tests in Unity

1. Open Unity.
2. Open the Test Runner:

```text
Window > General > Test Runner
```

3. Run EditMode tests first.
4. Run PlayMode tests second.

EditMode tests are faster and protect serialization, validation, templates, and editor-safe utilities.

PlayMode tests protect runtime behavior, scene-bound systems, step execution, interaction, audio, variables, anchors, layouts, RoboticsLite handlers, and ProcedureTraining handlers.

## Current EditMode test areas

```text
ModuleExportUtilityTests
ModuleJsonSerializerTests
ModuleLayoutValidationTests
ModuleTemplateFactoryTests
ModuleValidatorTests
RoboticsLiteValidationTests
SpatialRenderUtilityTests
ProcedureTrainingValidationTests   # under Domains/ProcedureTraining/Tests/EditMode
```

## Current PlayMode test areas

```text
AnchorResolverPlayModeTests
InteractionContextPlayModeTests
MCQStepHandlerPlayModeTests
ModuleRunnerFlowPlayModeTests
ModuleRunnerPlayModeTests
RoboticsLiteStepHandlerPlayModeTests
ProcedureTrainingStepHandlerPlayModeTests   # under Domains/ProcedureTraining/Tests/PlayMode
RuntimeAudioServicePlayModeTests
RuntimeModuleLoaderPlayModeTests
RuntimeVariableStorePlayModeTests
SceneBindingRegistryPlayModeTests
SpatialLayoutResolverPlayModeTests
StepFlowResolverPlayModeTests
```

## Optional command-line test commands

These paths vary by machine. Adjust the Unity executable path.

### macOS example

```bash
UNITY="/Applications/Unity/Hub/Editor/2022.3.62f1c1/Unity.app/Contents/MacOS/Unity"
mkdir -p artifacts/test-results
"$UNITY" -batchmode -projectPath "$PWD" -runTests -testPlatform editmode -testResults artifacts/test-results/editmode.xml -quit
"$UNITY" -batchmode -projectPath "$PWD" -runTests -testPlatform playmode -testResults artifacts/test-results/playmode.xml -quit
```

### Windows PowerShell example

```powershell
$Unity = "C:\Program Files\Unity\Hub\Editor\2022.3.62f1c1\Editor\Unity.exe"
New-Item -ItemType Directory -Force artifacts\test-results
& $Unity -batchmode -projectPath (Get-Location) -runTests -testPlatform editmode -testResults artifacts\test-results\editmode.xml -quit
& $Unity -batchmode -projectPath (Get-Location) -runTests -testPlatform playmode -testResults artifacts\test-results\playmode.xml -quit
```

## Phase 1 expected result

At Phase 1 exit:

- all previously passing EditMode tests still pass,
- all previously passing PlayMode tests still pass,
- the current demo still previews,
- the demo runbook matches what actually happens.

