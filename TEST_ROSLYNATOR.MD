# How to Test if Roslynator is Working

## Reload VS Code

Press Ctrl+Shift+P → Search "Reload Window" → Enter

This reinitializes the C# extension with the new settings

## Check OmniSharp output

Open the Output panel: Ctrl+J
Select "OmniSharp Log" from the dropdown
Look for messages confirming EditorConfig is loaded

## Trigger Roslynator warnings

Open InitiateCallFunction.cs
Look for squiggly lines (warnings/suggestions) in the editor
Hover over them to see their diagnostic codes (e.g., RCS1077, RCS1048)

## Check specific rules

Try writing `var x = new List<string>();` - should suggest using collection expressions (if enabled)
Try writing redundant parentheses like `if ((x == 5))` - should suggest removing them
Look at private field names - should warn if not prefixed with _ (from .editorconfig.personal)

## Verify in Problems panel

Press Ctrl+Shift+M to open Problems
Should see diagnostics from Roslynator with RCS codes

## If you see zero warnings/suggestions after reload, check

- OmniSharp log for errors
- Confirm .editorconfig.personal exists at .editorconfig.personal
- Try manually running: Ctrl+Shift+P → "Omnisharp: Restart OmniSharp"
