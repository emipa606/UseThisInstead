# GitHub Copilot Instructions for RimWorld Modding Project

## Mod Overview and Purpose
This mod, titled "Use This Instead," aims to enhance the user experience by providing an intuitive method of managing and replacing mods within RimWorld. It gives players streamlined options to replace outdated or unsupported mods with more stable or updated alternatives, all while integrating seamlessly with the game's existing mod management interface.

## Key Features and Systems
- **Mod Replacement Interface**: The mod introduces a user interface that simplifies the replacement of mods.
- **Steam Workshop Integration**: The ability to fetch and link replacement mods from the Steam Workshop using SteamURIs.
- **Version Support Verification**: A check system to ensure that replacement mods are compatible with the current game version.

## Coding Patterns and Conventions
- **Namespace Use**: Each class should reside in its own namespace for better organization and clarity.
- **Method Accessibility**: Wherever possible, methods are public unless specific encapsulation is needed.
- **Consistency in Naming**: Use PascalCase for class and method names, aligning with C# conventions.
- **Use of Static Classes**: For utility functions or methods that do not require instantiation, use static classes to encapsulate static methods.

## XML Integration
While the project summary did not specifically mention XML files, they are typically used in RimWorld modding for defining game data and settings. If XML files are included, ensure:
- **Proper Schema**: Adhere to RimWorld's XML schema to avoid runtime errors.
- **Encapsulation of Configuration**: Use XML to store settings that the user can change, such as preferences or toggles.
- **Deserializing XML Data**: Utilize C# libraries (like `System.Xml`) to load and parse XML data at runtime.

## Harmony Patching
- **Harmony Library Use**: The project should employ the Harmony library for patching methods instead of directly modifying the game's code.
- **Patch Targets**: Identify target methods in the base game to apply patches. An example could be adjusting the behavior of the mods list.
- **Prefix and Postfix Methods**: Use [HarmonyPrefix] and [HarmonyPostfix] attributes to modify the game behavior either before or after the original method execution.
- **Error Handling**: Implement robust error handling within Harmony patches to prevent game crashes due to unhandled exceptions.

## Suggestions for Copilot
- **Intelligent Suggestions**: Enable Copilot to suggest code snippets for common operations like file I/O, XML parsing, and HTTPS requests (useful for Steam Workshop integration).
- **Pattern Recognition**: Allow Copilot to recognize recurring patterns and suggest optimizations or alternative approaches.
- **Auto-Documentation**: Encourage Copilot to generate XML comments for methods to maintain a well-documented codebase.
- **Identify Best Practices**: Utilize Copilot to identify and suggest adherence to best practices in C# development and Harmony patching.

This mod should be maintained with attention to detail, ensuring high compatibility with RimWorld's updates and a smooth user experience. By following these instructions, developers can leverage GitHub Copilot to assist in writing efficient, maintainable, and high-quality code for the mod project.
