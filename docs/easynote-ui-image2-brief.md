# EasyNote gpt-image-2 UI Mockup Brief

This brief is derived from the current WPF code. It is for generating UI mockups only; it must not introduce features that do not exist in the app.

## Scope

Generate the current interactive surfaces:

- Pending empty state
- Pending populated list
- New todo draft editor
- Existing todo edit editor
- Settings panel
- Complete action reveal
- Long-press delete confirmation
- Completed populated list
- Completed empty state
- Night theme variants for the primary list and settings surfaces
- Native tray context menu

Do not generate separate screens for hidden-window state, global hotkey behavior, resize grip, or persistence because those states do not have independent visible UI.

## Existing Functionality Only

Allowed UI elements:

- Header tabs: `Todo`, `Done`
- Header icon buttons: add, settings, hide/minus
- Todo cards with text and secondary timestamp text
- Pinned todo card styling and pin/up icon in Pending view
- Completed-card red corner marker in Done view
- Draft text input with submit labels `记下` and `保存`
- Settings title `设置`
- Opacity slider with percent display
- Theme toggle button: `切换到夜间护眼` or `切换到日间护眼`
- Delete confirmation overlay: `删除这条待办？` / `删除这条已办？`, buttons `删除` and `撤销`
- Empty states: `还没有待办`, `还没有已办记录`
- Tray menu: `显示便签`, `固定到右上角`, `开机自动启动`, `退出`

Forbidden invented UI:

- Calendar, reminders, due dates, alarms
- Search, tags, projects, filters, priorities
- Cloud sync, account/login, sharing
- Markdown/rich text toolbar
- Attachments, images, voice, AI suggestions
- Statistics, charts, onboarding, help panels
- Bulk actions or extra tray menu items

## Typography

- Primary font: `Segoe UI Variable Text`
- Chinese fallback: `Microsoft YaHei UI`
- Accent/display fallback: `Segoe UI Variable Display`
- Tone: compact Windows desktop utility, quiet and readable, not marketing-oriented.

## Day Palette

- Chrome background: `#F0EADC`
- Chrome border: `#55D4C9B2`
- Surface: `#E1D8C4`
- Selected card: `#D8CFB9`
- Strong surface: `#F8F1DE`
- Hover: `#E9E0CB`
- Pressed: `#D5CBB4`
- Primary text: `#302A1F`
- Muted text: `#6F6657`
- Subtle text: `#8A806F`
- Accent: `#63845F`
- Pinned surface: `#E9D89D`
- Pinned border: `#B59B3E`
- Danger: `#B45C50`
- Done corner: `#C9342E`
- Delete overlay: `#EAF4ECD9`

## Night Palette

- Chrome background: `#242A22`
- Chrome border: `#33465240`
- Surface: `#30382D`
- Selected card: `#283022`
- Strong surface: `#3A4435`
- Hover: `#46523F`
- Pressed: `#20261F`
- Primary text: `#EEF1E5`
- Muted text: `#BBC5AD`
- Subtle text: `#8D9980`
- Accent: `#D8C46C`
- Pinned surface: `#4A4325`
- Pinned border: `#A9923E`
- Danger: `#E28B7F`
- Done corner: `#D7473F`
- Delete overlay: `#EE20261F`

## Generation Command

After `OPENAI_API_KEY` is available, run:

```powershell
python C:\Users\edu19\.codex\skills\.system\imagegen\scripts\image_gen.py generate-batch `
  --input D:\easynote\output\imagegen\easynote-ui-image2-prompts.jsonl `
  --out-dir D:\easynote\output\imagegen `
  --model gpt-image-2 `
  --quality high `
  --concurrency 3
```

The prompts already specify semantic output filenames under `output/imagegen/`.
