# Galactic Nexus: The Last Junkyard

## Project Overview
This is a High-Performance Mobile Tycoon game built with Unity ECS (DOTS) and URP.

## Technical Stack
- **Unity 6 / 2022.3+**
- **Entities 1.0.16**
- **URP (Universal Render Pipeline)**
- **Burst Compiler & Job System**

## Folder Structure
- `Assets/Scripts/Components`: ECS Data structures (IComponentData).
- `Assets/Scripts/Systems`: ECS Logic (ISystem).
- `Assets/Shaders`: High-end mobile shaders for "Pas ve Neon" look.
- `Packages/manifest.json`: Dependency configuration.

## How to Start
1. Open this folder with Unity Hub.
2. Ensure you have the 'Android Build Support' module installed.
3. Unity will automatically download the packages defined in `Packages/manifest.json`.
