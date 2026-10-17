# Low Poly Sign

Minecraft-style oak sign with up to 5 lines of text.

(The matching door lives next to this folder, in `1 - Environment/Low Poly Door`.)

## Setup
1. Import the package. The prefabs are built automatically into `Low Poly Sign/Prefabs`:
   - `Sign_Standing` - board on a post, pivot on the ground
   - `Sign_Wall` - board only, pivot at the back so it sits flush on a wall
2. If Unity asks to import **TMP Essentials**, accept it.
3. To regenerate the prefabs: **Tools > Low Poly Sign > Rebuild Prefabs**.

The readable face points toward -Z. Scale is 1 unit = 1 block (board is 1 m x 0.6 m).

## Editing text
Select a sign and fill in the 5 `Lines` on the **LowPolySign** component. Each line is cut at
`Max Chars Per Line` (default 16). Long lines shrink to fit instead of wrapping.

From code:

```csharp
var sign = GetComponent<LowPolySign>();
sign.SetLine(0, "Welcome!");          // lines 0-4
sign.SetText("Line 1\nLine 2\nLine 3"); // extra lines past 5 are dropped
sign.TextColor = Color.white;
sign.ShowOnBack = true;               // standing sign only
```

## Colours
Materials live in `Low Poly Sign/Generated`. Edit them freely; rebuilding keeps your changes.
Delete a material and rebuild to get the default colour back. Works with Built-in, URP and HDRP.
