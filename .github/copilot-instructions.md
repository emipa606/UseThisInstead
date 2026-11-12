# GitHub Copilot Instructions for Use This Instead Mod

## Mod Overview and Purpose

The Use This Instead mod for RimWorld is designed to scan your installed mods and suggest working or more updated replacements for those mods. Continuations of mods have become a significant part of the RimWorld modding community, making it challenging to keep track of updates. This mod aims to help users stay informed about the availability of new versions or replacements for their installed mods on the Steam Workshop.

## Key Features and Systems

### Replacement Suggestion
- The mod checks for available replacements for your installed mods.
- It uses data from a collection of continued mods, including the author's own collection, Zaljerem's collection, velcroboy333's collection, and mods requested on the author's Discord server.
- The mod also considers mods published on the workshop using common suffixes for continued mods.

### Mod Options
- Always show replacement button, even if there are no replacements.
- Show all possible replacements or only for active mods.
- Show all replacement versions or only for the currently installed RimWorld version.
- Verbose logging for debugging purposes.
- Replacement button available in mod options for those using a custom GUI theme.

### Regular Updates
- The replacement rules are updated regularly to include new releases.
- The mod automatically downloads the latest version of the rules as soon as they are published.

## Coding Patterns and Conventions

### C# Files
- **Dialog_ModReplacements.cs**: Contains the `Dialog_ModReplacements` class, which handles the dialog for displaying replacement suggestions.
- **UseThisInsteadSettings.cs**: Contains the `UseThisInsteadSettings` class, which manages the mod settings.
- **Json.cs**: Contains classes and methods for handling JSON parsing and serialization.
- **Widgets_ButtonText.cs**: Contains the `Widgets_ButtonText` class, which provides a postfix for button text widgets.
- **Page_ModsConfig_RecacheSelectedModInfo.cs**: Contains the `Page_ModsConfig_RecacheSelectedModInfo` class, which handles the prefix for caching selected mod information.

### XML Files
- **UpdateInfo.xml**: Contains the `TabulaRasa.UpdateDef` definition.
- **List.xml**: Contains personal favorites list data.
- **template.xml**: A template file used for various purposes within the mod.

## XML Integration

The mod uses XML files to store and manage various definitions and data. Key XML files include:

- **UpdateInfo.xml**: Contains information about mod updates and replacements.
- **List.xml**: Contains personal favorites list data.
- **template.xml**: A template file used for various purposes within the mod.

When working with XML files, ensure that the structure and definitions are consistent with the mod's requirements.

## Harmony Patching

The mod utilizes Harmony to patch existing methods in the RimWorld game. Key patches include:

- **Widgets_ButtonText.cs**: Provides a postfix for button text widgets.
- **Page_ModsConfig_RecacheSelectedModInfo.cs**: Handles the prefix for caching selected mod information.

When using Harmony for patching, ensure that the patches are correctly applied and do not interfere with the original game logic.

## Suggestions for Copilot

### General Recommendations
- Use consistent naming conventions for classes, methods, and variables.
- Follow the mod's coding patterns and conventions when writing new code.
- Ensure that XML files are well-structured and consistent with the mod's requirements.
- Use Harmony patches judiciously and test them thoroughly to avoid conflicts with the original game logic.

### Specific Recommendations
- When adding new features, consider how they will integrate with existing systems and options.
- When updating replacement rules, ensure that the new rules are accurate and comprehensive.
- When debugging, use verbose logging to trace the flow of the mod and identify potential issues.

By following these guidelines, you can help ensure the quality and maintainability of the Use This Instead mod.

## Project Solution Guidelines
- Relevant mod XML files are included as Solution Items under the solution folder named XML, these can be read and modified from within the solution.
- Use these in-solution XML files as the primary files for reference and modification.
- The `.github/copilot-instructions.md` file is included in the solution under the `.github` solution folder, so it should be read/modified from within the solution instead of using paths outside the solution. Update this file once only, as it and the parent-path solution reference point to the same file in this workspace.
- When making functional changes in this mod, ensure the documented features stay in sync with implementation; use the in-solution `.github` copy as the primary file.
- In the solution is also a project called Assembly-CSharp, containing a read-only version of the decompiled game source, for reference and debugging purposes.
- For any new documentation, update this copilot-instructions.md file rather than creating separate documentation files.


## Hard rules (must follow)
- Do NOT run commands that modify the repo (no git commit, git apply, dotnet format) unless explicitly asked.
- Prefer minimal reads: read only the smallest code region needed (around the suspicious lines).
- When mentioning SonarQube issues, automatically use the SonarQube MCP service to fetch and address issues instead of making inferred fixes without querying SonarQube first.
- When mentioning the rimworld log, automatically use the Rimworld MCP service to fetch the log.

