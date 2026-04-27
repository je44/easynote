# EasyNote UI/Icon Review

> Review date: 2026-04-26
> Method: Huashu-Design expert review from current WPF source, existing preview image, and Release self-test.
> Focus: icon semantics, icon consistency, affordance, state clarity, and fit for a compact Windows desktop utility.

## Executive Verdict

EasyNote already has a coherent compact utility direction: quiet sticky-note palette, simple task states, and a small always-on-desktop footprint. The weakest layer is the icon system. The current icons are hand-authored XAML path geometries with inconsistent metaphors and weak state language. They work technically, but they do not yet feel like a mature desktop product icon set.

Overall UI score: 7.2 / 10
Icon system score: 5.8 / 10

## Evidence

- App icon and tray icon use `Assets/icon.ico`, with preview at `EasyNote/Assets/icon-preview.png`.
- Header/action icons are inline `PathGeometry` resources in `EasyNote/MainWindow.xaml`.
- Tray menu uses the same app ico through `TaskbarIcon IconSource="/Assets/icon.ico"` in `EasyNote/App.xaml`.
- Release build passed: `dotnet build .\easy-note-wpf.sln -c Release`.
- Self-test passed all 20 checks, including startup, tray menu, add/edit/pin/complete/delete, hotkey, docking, opacity, and autostart persistence.

## Screen Coverage

### Pending Empty State

The empty state is calm and legible. The header icons carry most of the interaction burden, so their ambiguity matters more here than in populated states. The plus and minus are recognizable, but the settings icon reads as "equalizer/sliders" rather than "settings" and may be mistaken for filtering or display controls.

Priority fix: replace the settings sliders icon with a clearer gear or tune icon, then keep the tooltip.

### Pending Populated List

Cards are visually warm and readable. The pinned state uses both a different surface color and an up-arrow/pin-like icon. The problem is semantic: the geometry looks like "move to top" more than "pinned." That may be acceptable if the feature is intentionally "top this item", but the tooltip says `置顶`, while the visual language reads closer to "upload/up."

Priority fix: decide whether this is "pin" or "move to top"; use a pin icon for persistent pinned state, or rename the tooltip to match the up icon.

### Draft And Edit Panels

The draft/edit panel mostly depends on text labels, not icons. The submit button labels are clear. There is no visual distinction between "new note" and "editing existing note" besides the submit label changing from `记下` to `保存`.

Priority fix: optional small leading icon in the editor title area only if the panel gets a title. Do not add a toolbar.

### Settings Panel

The close `X` is clear. The opacity slider has a custom thumb but no icon context. Since there is only one setting plus theme toggle, this is acceptable. The theme toggle is label-only; for a compact utility this is fine, but the day/night state would read faster with a sun/moon icon if the control is later tightened.

Priority fix: no urgent icon change here beyond unifying `X` stroke sizing with header icons.

### Complete Action Reveal

The check-circle action is the strongest icon in the app. It communicates completion, has motion support, and fits the task domain. However, the same check mark appears only after state/action reveal; the normal card has no persistent affordance that says "click to reveal complete action." This is a discoverability issue, not a rendering issue.

Priority fix: add a subtle hover-only check affordance or keep the current reveal but document the interaction in onboarding/release notes.

### Long-Press Delete Confirmation

Delete uses trash plus text; cancel uses X plus text. This is a good pattern for destructive UI. The overlay is visually understandable, but the `X` icon overlaps semantically with settings close. In destructive overlays, `X` can mean close/cancel, so it is still acceptable because the text says `撤销`.

Priority fix: keep text labels; do not make destructive actions icon-only.

### Completed Populated List

The red folded corner is a clever compact completion marker, but it is not universally understood as "done." It reads like a bookmark, priority flag, or folded-paper flourish. Since completed items are already in the `Done` tab, this marker is decorative rather than necessary.

Priority fix: either remove the red corner for visual calm, or replace it with a tiny check marker if state redundancy is desired.

### Night Theme

The night palette keeps contrast better than the day palette for icons because the accent yellow pops. The risk is that icon color semantics shift: day hover green, night hover yellow, danger salmon, pinned yellow. This is acceptable, but the icon set should use a small semantic color contract: neutral actions, positive completion, pinned, destructive.

Priority fix: define icon semantic brushes separately from text brushes.

### Tray Menu

The tray icon is attractive at preview size, but it contains many small interior details: yellow dot, check circle, lines, highlight fold, rounded card. At 16x16 or 20x20 tray scale, those details will collapse. The icon silhouette is a rounded blue square, which is recognizable but generic among Windows tray icons.

Priority fix: create tray-specific 16/20/24px variants with simplified geometry: rounded square + bold check/note mark, remove secondary line details at small sizes.

## Icon Findings

### P0

None. No icon issue blocks use or causes an obvious destructive-action hazard.

### P1

1. Settings icon metaphor is weak for this app.
   Evidence: `IconSettingsGeometry` is a sliders/equalizer shape, while settings opens opacity/theme controls. Users may read it as filter/display tuning.
   Fix: use a gear icon or keep sliders only if the panel is positioned as "appearance controls."

2. Tray icon is over-detailed for system tray scale.
   Evidence: `IconSource="/Assets/icon.ico"` uses the same app icon. The preview works at 256px, but tray contexts need a reduced symbol.
   Fix: add simplified small-size ico frames, especially 16x16 and 20x20.

3. Pinned icon does not match pinned semantics.
   Evidence: `IconTopGeometry` is an upward arrow with baseline, while tooltip is `置顶`.
   Fix: use a pushpin if the item remains pinned; keep arrow only for a one-shot "move to top" command.

### P2

4. Header icons have thin stroke and low visual authority compared with active tab text.
   Fix: increase header icon viewbox from 15.5 to 16 and stroke from 1.75 to 2.0, or reduce active tab jump from 19px to 18px.

5. `X` is reused for close settings and cancel delete.
   Fix: acceptable with text labels; if cancel ever becomes icon-only, use undo/return instead of X.

6. Completed red corner is not semantically crisp.
   Fix: replace with check marker or remove. The `Done` tab already supplies the state.

7. Icon style is manually maintained in multiple styles.
   Fix: centralize icon size/stroke/cap rules into one icon token style before adding new icons.

### P3

8. The app icon palette is disconnected from the in-app palette.
   Fix: either accept the blue icon as brand contrast, or introduce a warm/paper-themed icon variant so the product feels more unified.

9. Tooltip coverage is good but not accessibility-complete.
   Fix: add `AutomationProperties.Name` for icon-only buttons.

## Recommended Icon Direction

Use a small Fluent-like line icon set with consistent geometry:

- Add: plus
- Settings: gear or tune, not ambiguous equalizer unless panel becomes appearance-only
- Hide: minus
- Pin: pushpin
- Complete: circle-check
- Delete: trash
- Cancel: undo for delete overlay, X for panel close
- Theme: moon/sun only if the theme button becomes compact

Stroke contract:

- Header icons: 16px optical size, 2px stroke, round caps.
- Inline card icons: 14px optical size, 1.8px stroke.
- Tray icon: separate raster/ico design per small size, not just scaled from 256px.

Color contract:

- Neutral action: muted text brush
- Hover action: accent brush
- Complete: accent/positive brush
- Pinned: pinned icon brush
- Destructive: danger brush
- Panel close/cancel: muted until hover

## Interface-Level Recommendations

1. Keep the compact layout. The current surface density fits a desktop sticky-note utility.
2. Avoid adding instructional copy inside the UI. The app should stay quiet.
3. Treat icon-only actions as a designed system, not one-off paths.
4. Keep destructive confirmation text labels; do not rely on trash/X alone.
5. Create a small icon QA checklist: semantic match, 16px legibility, day/night contrast, hover/pressed state, tooltip, automation name.

## Suggested Implementation Order

1. Replace settings and pin icons.
2. Add `AutomationProperties.Name` to all icon-only buttons.
3. Normalize header/inline icon stroke and viewbox rules.
4. Create tray-specific ico small-size frames.
5. Reconsider the completed red corner after the core icon set is fixed.

## Verification Notes

Verified:

- Release build succeeds with zero warnings/errors.
- Built-in self-test report succeeds for startup, tray, add/edit/pin/complete/delete, hotkey, dock, opacity, autostart.

Not verified:

- Fresh live screenshots for every visual state were not captured in this review pass.
- Tray icon legibility at true 16x16/20x20 was assessed from the icon preview and design reasoning, not pixel-inspected in the Windows tray.
