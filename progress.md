## 2026-06-29 - Task: Add mouse-driven hand IK target controller
### What was done
- Added a runtime controller that moves a hand IK target based on the mouse position.
- Supported both physics raycast hits and fixed-plane projection for different camera/gameplay views.
- Added a short setup document for binding the script in Unity.
### Testing
- Passed: `MSBuild.exe .\Assembly-CSharp.csproj /t:Build /p:RestorePackages=false /p:Restore=false /nologo /v:m`.
- Pending: Unity Editor play-mode validation was not run in this environment.
### Notes
- `Assets/Scripts/Core/Runtime/MouseHandIKTargetController.cs`: added the mouse-to-IK-target movement component.
- `Assets/Scripts/Core/Runtime/MouseHandIKTargetController.cs.meta`: added Unity metadata for the new script asset.
- `docs/MouseHandIKTargetController.md`: documented the Inspector binding and hit-mode setup.
- Rollback: delete the three files above and remove this progress entry if a full rollback is required.
